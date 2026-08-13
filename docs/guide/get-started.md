# Get started — adding Nester to an existing app

> The template already did all of this: `dotnet new nester-avalonia` (or `-wpf`) produces a working
> app. Read this page when you are wiring Nester into an application you already have.
> Mechanism: `docs/design/shell.md`, `docs/design/hosting.md`.

## 1. Install

| Your app | Package |
|---|---|
| Avalonia | `Everlong.Nester.Avalonia` |
| WPF | `Everlong.Nester.Wpf` |

```xml
<PackageReference Include="Everlong.Nester.Avalonia" Version="..." />
<!-- LangVersion must be preview: [Inject] is generated over partial properties. -->
<LangVersion>preview</LangVersion>
```

Add the Extensions packages when you want the built-in dialog and notice chrome:

```xml
<PackageReference Include="Everlong.Nester.Extensions.Avalonia" Version="..." />
```

Skip them and the dialog sessions, the dimmer and the notice entries have no views — you then map
those models to your own views with `[ViewFor<T>]` / `[Mapping<TModel,TView>]`. The auth *engine* is
the same story: referencing only the platform package gives you no `AddNesterAuth`.

`Everlong.Nester` (core + analyzers), `Everlong.Nester.Abstractions` and the standalone
`Everlong.DI` / `Everlong.Globalization` come in transitively — never reference them by hand.
`Everlong.Settings` does not: nothing in Nester declares a setting, so add it yourself when the app
declares `[Settings]` / `[Section]` types, and its generator writes the `[Setting]` members into your
assembly.

## 2. Declare the view locator

The generator turns the locator into the view-resolution table. Avalonia and WPF differ only in the
trigger attribute and the base type.

```csharp
// Avalonia
[ViewLocator]
[Mapping<VideoPlayerPageModel, VideoPlayerPage>]   // cross-assembly / central registration
public partial class ViewLocator;

// WPF
[WpfViewLocator]
public partial class ViewLocator;
```

A view declares itself with `[ViewFor<TViewModel>]`; `[Mapping<TModel, TView>]` on the locator is the
only override and the only way to bind a view model that carries no `[ViewFor]`. Nothing is inferred
from names, namespaces or base types.

## 3. Write the shell class

A shell is one window (or one single-view top-level) with its own DI container. You own the class —
`UiShell` in the template — and derive it from the platform base.

```csharp
public sealed partial class UiShell(IActivationIntent? startupIntent = null)
    : AvaloniaShell(startupIntent)          // WPF: WpfShell
{
  protected override IServiceProvider InitializeServices()
  {
    var services = new ServiceCollection();
    services.AddScoped(DirectorType);        // ① the Director — resolved through DirectorType
    services.AddNesterShell(this);           //     the shell's identity (IShell / ILayerBroker / …)
    services.AddNesterCore();                //     core infrastructure
    services.AddNesterDialog();              //     the dialog domain (Extensions)
    services.AddNesterNotice();              //     the notice domain (Extensions)
    services.AddServices(new AppServices()); //     your [Singleton/Transient/Scoped] services
    return services.BuildServiceProvider(new ServiceProviderOptions   // ② the shell owns its provider
    {
      ValidateScopes = true, ValidateOnBuild = true
    });
  }

  protected override void PrepareHost()      // the window (default: resolved by the view locator)
  {
    ShellHost = Director switch
    {
      MainViewModel => new MainWindow(),
      LoginWindowModel => new LoginWindow { Shell = this },
      _ => throw new InvalidOperationException($"No host for {Director?.GetType().Name}")
    };
  }

  protected override void OnAssembled()      // ③ the ready anchor — services live, host connected
  {
    AppSettings.Default.Inject(Services);    // the settings store is a shell singleton: no scope needed
  }
}
```

`DirectorType` is the per-instance public thread: it declares which `IShellDirector` this shell runs,
and the framework resolves it from the container. `Start()` presents the shell synchronously and runs
the startup dispatch; `shell.Lifetime.Startup` is the completion signal (the first navigation
settled).

## 4. Bootstrap the process

Build the process container first, negotiate, then build the app lifetime and start the first shell.

```csharp
// Avalonia — App.axaml.cs
public override void OnFrameworkInitializationCompleted()
{
  var process = BuildAppServices();                     // the user-built process container

  if (process.GetService<IActivationAgent>() is IDesktopActivationAgent desktop
      && desktop.Negotiate() == ActivationOutcome.Yield)
  {
    (process as IDisposable)?.Dispose();                // a follower exits before anything is built
    return;
  }

  AppLifetimeBuilder.Build(new AppLifetimeOptions
  {
    ErrorHandler = this,
    Services = process,
    Desktop = new DesktopOptions { DisposeOnExit = true, UseExplicitShutdown = false }
  });

  var shell = new UiShell { DirectorType = typeof(LoginWindowModel) };
  shell.Start();                                        // the window is up at this line
  AppLifetime.SetMainShell(shell);                      // Start() never promotes — say it explicitly
  Lang.Initialize(AppSettings.Default.Language);        // runs synchronously, shell already up

  base.OnFrameworkInitializationCompleted();
}
```

```csharp
// WPF — App.xaml.cs: the same flow inside OnStartup(StartupEventArgs e)
var process = BuildAppServices();
// … negotiation …
AppLifetimeBuilder.Build(new AppLifetimeOptions { ErrorHandler = this, Services = process });
var shell = new UiShell { DirectorType = typeof(LoginWindowModel) };
shell.Start();
AppLifetime.SetMainShell(shell);
Lang.Initialize(AppSettings.Default.Language);
```

The process container is where cross-window services live — the message hub, the auth service, the
activation agent. Register the agent through a factory so the container disposes it:

```csharp
var messageHub = new MessageHub();
services.AddSingleton<IMessageHub>(messageHub);
if (CreateActivationAgent(messageHub) is { } agent)
  services.AddSingleton<IActivationAgent>(_ => agent);
services.AddNesterAuth(options => options.AddPolicy("Admin", p => p.RequireRole("Admin")));
```

A window container cannot resolve a process service by itself. The template ships a small helper
for the bridge (`examples/Template.Shared/Hosting/ServiceBridgeExtensions.cs`):

```csharp
services.BridgeSingleton<IAuthService>();   // resolves from AppLifetime.Current.Services per shell
services.BridgeSingleton<IMessageHub>();
```

## 5. Merge the theme resources

```csharp
// Avalonia — App.axaml.cs, Initialize(): the template keeps the table in code so the
// bootstrap flow reads in one place. Each entry is a plain locator and the list is read
// first entry first — the platform's own rule — so a locator declared ahead of the
// extension one overrides a shipped view.
DataTemplates.Add(new ViewLocatorHook());              // optional: hand-written custom resolution
DataTemplates.Add(new ViewLocator());                  // source-generated user mappings
DataTemplates.Add(new NesterExtendedViewLocator());   // extension views — only with the Extensions packages
```

The order above is priority order, highest first — the same reading Avalonia applies to the collection
itself, so the shell's locator and any `ContentPresenter` resolving a view model on its own agree: the
first entry that matches wins, the logical tree is consulted before the collection, and the extension
views are what remains when nothing more specific matched. WPF resolves a merged resource dictionary
last entry first, so its declaration is the mirror of this one and its template merges the same three
in the opposite order.

A locator builds: `Build` returns a new view for the data it is given, and a locator never hands back a
control it built for earlier data — one mount, one view, on both platforms.

```csharp
// WPF — App.xaml.cs, OnStartup(): the same three, mirrored.  A merged resource dictionary
// resolves LAST entry first, so the hook is merged last to win and the extension views are
// the fallback.
Resources.MergedDictionaries.Add(new NesterExtendedViewLocator());
Resources.MergedDictionaries.Add(new ViewLocator());
Resources.MergedDictionaries.Add(new DynamicViewLocator());   // optional: hand-written custom resolution
```

`docs/design/shell.md` §4 states the rule; the shells' composites follow their own platform.

```xml
<!-- Avalonia — App.axaml: the styles, in this order -->
<Application.Styles>
  <StyleInclude Source="avares://Everlong.Nester.Avalonia/Themes/NesterTheme.axaml" />
  <StyleInclude Source="avares://Everlong.Nester.Extensions.Avalonia/Themes/NesterExtendedTheme.axaml" />
  <FluentTheme />
</Application.Styles>
```

The `NesterExtendedTheme` entry belongs there only when the Extensions dialog and notice chrome is
used; an app that maps those models itself skips it.

```xml
<!-- WPF — App.xaml -->
<ResourceDictionary.MergedDictionaries>
  <ResourceDictionary Source="pack://application:,,,/Everlong.Nester.Wpf;component/Themes/Generic.xaml" />
  <ResourceDictionary Source="pack://application:,,,/Everlong.Nester.Extensions.Wpf;component/Themes/NesterExtendedTheme.xaml" />
</ResourceDictionary.MergedDictionaries>
```

The WPF `ThemeMode` on `<Application>` loads Fluent itself — do not merge
`PresentationFramework.Fluent` by hand, or the theme switch is shadowed and the app stays light.

## 6. The host view

The shell's host mounts the framework's stage into its own container at one empty slot:

```xml
<ContentControl x:Name="RootSlot" />   <!-- Avalonia; the shell wires it in PrepareHost / the view -->
```

`RootSlot` is a template convention, not a framework name; the contract is
`IAvaloniaShellHost.HostShell(shell, stage)` / the WPF equivalent. Nothing else in the view is
special.

## 7. Checklist

| Step | Avalonia | WPF |
|---|---|---|
| Locator trigger | `[ViewLocator]` | `[WpfViewLocator]` |
| Shell base | `AvaloniaShell` | `WpfShell` |
| Bootstrap hook | `OnFrameworkInitializationCompleted` | `OnStartup` |
| Entry point | `Program.cs` + `BuildAvaloniaApp` | generated `Main` from `App.xaml` |
| Theme | `NesterTheme.axaml` | `Generic.xaml` |
| Translation table | `Application.DataTemplates` | merged resource dictionaries |

Next: `docs/guide/navigation.md` for routing, `docs/guide/interaction.md` for dialogs, notices and
the rest of the extension surface.
