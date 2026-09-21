using System.Diagnostics;
using System.Runtime.ExceptionServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Everlong.Nester.Activation;
using Everlong.Nester.Auth;
using Everlong.Nester.Presentation;
using Everlong.Nester.Diagnostics;
using Everlong.Nester.Hosting;
using Everlong.Nester.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NesterApp.Pages.Login;
using NesterApp.Pages.Shell;
using NesterApp.Properties;

namespace NesterApp;



/// <summary>
///   Avalonia bootstrap, and the app's Model→View translation table.  The
///   order below is the order to read it in: process container → single
///   instance negotiation → app lifetime → the first shell → i18n.  Only
///   process-wide wiring belongs here; each shell builds its own container
///   (<c>UiShell.cs</c>).
/// </summary>
/// <remarks>
///   Your seam: the services in <c>BuildAppServices</c>, the activation
///   agent, and the startup shell with its first navigation.
/// </remarks>
public partial class App : Application, IErrorHandler
{
  public override void Initialize()
  {
    AvaloniaXamlLoader.Load(this);

    // ── Model→View translation table ────────────────────────────────────────
    // Nester does not invent its own mapping mechanism: the App's
    // DataTemplates collection IS the translator.  Every view — framework or
    // user — is resolved by falling through this list, and there is no other
    // registration path.  The shell's own host surface is NOT resolved here:
    // a shell declares its window by overriding PrepareHost.  Each entry is a
    // DataTemplate:
    //
    //   NesterExtendedViewLocator → the extension packages' framework
    //                       views: notices (toast/snackbar/notification),
    //                       dialog sessions + dimmer chrome (DefaultDimmerModel
    //                       → DimmerLayout) (Everlong.Nester.Extensions.Avalonia)
    //   ViewLocator       → source-generated user mappings, declared via
    //                       [ViewFor<T>] on views or [Mapping<T,V>] on
    //                       ViewLocators.cs
    //   ViewLocatorHook   → hand-written custom resolution (example:
    //                       PostDetailPage, shape subtypes)
    //
    // This order is priority order, and it is declared highest first: the
    // framework mounts every route, dialog and notice view through the tree's
    // own lookup, and Avalonia resolves a template the same way — the first entry
    // that matches wins (Controls/DataTemplatePrecedenceTests pins both).  The
    // extension locator therefore sits last: framework views are what is left
    // when nothing more specific matched, and a locator declared ahead of it
    // overrides a shipped view.  WPF mirrors this — a merged resource
    // dictionary resolves last entry first, so App.xaml.cs declares the same
    // three in the opposite order.
    //
    // Order matters only for overlapping Match()es; unmatched types simply
    // fall through to the next entry.
    DataTemplates.Add(new ViewLocatorHook());
    DataTemplates.Add(new ViewLocator());
    DataTemplates.Add(new NesterExtendedViewLocator());
  }

  /// <summary>
  ///   The activation agent — your specialization point: return your own
  ///   agent here (custom negotiation, custom deep-link protocol...).
  ///   Desktop: the framework's per-OS agent.  Single-view platforms
  ///   (Android/iOS/Browser) may return <c>new SingleViewActivationAgent(messageHub)</c>
  ///   for OS activation events (deep links); <see langword="null" /> =
  ///   no activation surface (no conversion, no deep links).
  /// </summary>
  private static IActivationAgent? CreateActivationAgent(IMessageHub messageHub)
  {
    if (OperatingSystem.IsWindows())
      return new WindowsActivationAgent(enableNegotiation: true);
    if (OperatingSystem.IsLinux())
      return new LinuxActivationAgent(enableNegotiation: true);
    if (OperatingSystem.IsMacOS())
      return new MacActivationAgent(enableNegotiation: true, messageHub: messageHub);
    return null;
  }

  public override void OnFrameworkInitializationCompleted()
  {
    // ── ① The process-level container first — the user builds it (the
    //    activation agent registers here); the host receives it. ──
    var process = BuildAppServices();

    // ── ② Activation negotiation at the very front ─────────────────────────
    // A follower probes the leader synchronously; yielding suicides in
    // place — the container (agent included) is disposed here, nothing
    // else was built.
    if (process.GetService<IActivationAgent>() is IDesktopActivationAgent desktop)
    {
      var outcome = desktop.Negotiate();
      if (outcome == ActivationOutcome.Yield)
      {
        (process as IDisposable)?.Dispose();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
        {
          desktopLifetime.Shutdown(-1);
        }

        return;
      }
    }

    AppLifetimeBuilder.Build(new AppLifetimeOptions
    {
      ErrorHandler = this,
      // The user-built process container — cross-window shared services and
      // the activation agent live here; the shells bridge them in their
      // InitializeServices.
      Services = process,
      Desktop = new DesktopOptions()
      {
        DisposeOnExit = true,
        UseExplicitShutdown = false
      }
    });

    // ── Shell assembly — the whole path, written out ────────────────────────
    // The user's own Shell class: `new` the window, the constructor runs the
    // assembly ceremony (registrations → the window's own container →
    // settings injection), Start presents it and runs the startup dispatch
    // (agent drain → first navigation).
    if (AppLifetime.IsSingleView)
    {
      var shell = new UiShell { DirectorType = typeof(MainViewModel) };
      shell.Start();   // single-view: direct mount — the stage itself becomes the MainView (the startup flow settles via shell.Lifetime.Startup)
      AppLifetime.SetMainShell(shell);   // the main-shell declaration is explicit (Start never promotes)

      // i18n — runs synchronously; the shell is already up and its bindings
      // resolve Lang after this line.
      Lang.Initialize(AppSettings.Default.Language);
    }
    else
    {
      // Two-window example: login window first; the main window is created by
      // LoginPageModel after a successful login (same path, written out there).
      var shell = new UiShell { DirectorType = typeof(LoginWindowModel) };
      shell.Start();   // desktop: the window is synchronously presented (survival anchor); the first navigation settles via shell.Lifetime.Startup
      AppLifetime.SetMainShell(shell);   // the main-shell declaration is explicit (Start never promotes)

      // i18n — runs synchronously; the login window is already up and its
      // bindings resolve Lang after this line.
      Lang.Initialize(AppSettings.Default.Language);
    }

    base.OnFrameworkInitializationCompleted();
  }

  private static IServiceProvider BuildAppServices()
  {
    var services = new ServiceCollection();

    // Cross-window message hub — process-level (the shells bridge it into
    // their window containers); the lifecycle-capable agents publish the
    // AppResumed / AppBackground / AppReopen facts through it.
    var messageHub = new MessageHub();
    services.AddSingleton<IMessageHub>(messageHub);

    // The activation agent — registered as a process-level service (the
    // shells bridge it into their window containers).  Factory-registered on
    // purpose: the container disposes what its factory creates, so the
    // process container (released after every shell) reclaims the agent's
    // resources — an instance registration would never be disposed.
    if (CreateActivationAgent(messageHub) is { } agent)
    {
      services.AddSingleton<IActivationAgent>(_ => agent);
    }

    services.AddNesterAuth(options =>
    {
      options.AddPolicy("Admin", p => p.RequireRole("Admin"));
    });

    return services.BuildServiceProvider(new ServiceProviderOptions
    {
      ValidateScopes = true,
      ValidateOnBuild = true
    });
  }


  public bool HandleError(Exception exception)
  {
    AppLifetime.MainShell?.Services.GetService<ILogger<App>>()
      ?.LogCritical(exception, "Unhandled host error");
    Debug.WriteLine(exception.StackTrace);
    ExceptionDispatchInfo.Capture(exception).Throw();
    return false;
  }
}
