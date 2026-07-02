using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Windows;
using Everlong.Nester.Presentation;
using Everlong.Nester.Diagnostics;
using Everlong.Nester.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NesterApp.Pages.Login;
using NesterApp.Properties;
using Everlong.Nester.Activation;
using Everlong.Nester.Auth;
using Everlong.Nester.Messaging;

namespace NesterApp;


/// <summary>
///   WPF bootstrap, and the app's Model→View translation table.  The order
///   below is the order to read it in: process container → single instance
///   negotiation → app lifetime → the first shell → i18n.  Only process-wide
///   wiring belongs here; each shell builds its own container
///   (<c>UiShell.cs</c>), and leaf item views are declared in <c>App.xaml</c>.
/// </summary>
/// <remarks>
///   Your seam: the services in <c>BuildAppServices</c>, the activation
///   agent, and the startup shell with its first navigation.
/// </remarks>
public partial class App : IErrorHandler
{
  /// <summary>
  ///   The activation agent — your specialization point.  WPF runs Windows
  ///   only; return your own agent for custom negotiation / protocol.
  /// </summary>
  private static IActivationAgent CreateActivationAgent() => new WindowsActivationAgent(enableNegotiation: true);

  public bool HandleError(Exception exception)
  {
    AppLifetime.MainShell?.Services.GetService<ILogger<App>>()
      ?.LogCritical(exception, "Unhandled host error");
    Debug.WriteLine(exception.StackTrace);
    ExceptionDispatchInfo.Capture(exception).Throw();
    return false;
  }

  protected override void OnStartup(StartupEventArgs e)
  {
    Console.OutputEncoding = Encoding.UTF8;

    base.OnStartup(e);

    // ── Model→View translation table ────────────────────────────────────────
    // Nester does not invent its own mapping mechanism: the App's resource
    // dictionaries ARE the translator.  Every view — framework or user — is
    // resolved by falling through the locators merged here (each is a
    // ResourceDictionary + IViewLocator; the last match wins):
    //
    //   NesterExtendedViewLocator → the extension packages' framework
    //                       views: notices (toast/snackbar/notification),
    //                       dialog sessions + dimmer chrome (DefaultDimmerModel
    //                       → DimmerLayout) (Everlong.Nester.Extensions.Wpf)
    //   ViewLocator       → source-generated user mappings, declared via
    //                       [ViewFor<T>] on views or [Mapping<T,V>] on
    //                       ViewLocators.cs
    //   DynamicViewLocator→ hand-written custom resolution (example:
    //                       PostDetailPage, shape subtypes)
    //
    // A leaf item view with nothing to dispatch skips all three: EmojiIcon is
    // an implicit DataTemplate declared in App.xaml, which the ContentPresenter
    // inside RouteLink/RouteTreeItem resolves by type directly — a locator
    // entry would wrap every icon in a ContentControl.
    //
    // A merged resource dictionary is searched last entry first and the WPF
    // locator walks the list the same way, so the order below means one thing:
    // the hook overrides, the extension views are the fallback.  Avalonia
    // resolves first entry first, so its template declares the same three in
    // the mirrored order (Template.Avalonia/App.axaml.cs).
    //
    // Order matters only for overlapping registrations; unmatched types
    // simply fall through to the next locator.
    Resources.MergedDictionaries.Add(new NesterExtendedViewLocator());
    Resources.MergedDictionaries.Add(new ViewLocator());
    Resources.MergedDictionaries.Add(new DynamicViewLocator());


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
        Shutdown(-1);
        return;
      }
    }

    AppLifetimeBuilder.Build(new AppLifetimeOptions
    {
      ErrorHandler = this,
      DisposeOnExit = true,
      // The user-built process container — cross-window shared services and
      // the activation agent live here; the shells bridge them in their
      // InitializeServices.
      Services = process,
      UseExplicitShutdown = false
    });



    // ── Shell assembly — the whole path, written out ────────────────────────
    // The user's own Shell class: ONE class for every window kind, the kind
    // declared per instance through the DirectorType thread (set before
    // Start — the framework resolves the Director; the host window comes
    // from PrepareHost — direct creation here, or the view locator by
    // default).  Start() presents the window and runs the Director's
    // startup coroutine (first navigation).  The main window is created by
    // LoginPageModel after a successful login (same path, written out
    // there).
    var shell = new UiShell { DirectorType = typeof(LoginWindowModel) };
    shell.Start();   // synchronous presentation (survival anchor); the startup flow settles via shell.Lifetime.Startup
    AppLifetime.SetMainShell(shell);   // the main-shell declaration is explicit: activation intents route here (Start never promotes)

    // i18n — runs synchronously; the login window is already up and its
    // bindings resolve Lang after this line.
    Lang.Initialize(AppSettings.Default.Language);
  }


  private static IServiceProvider BuildAppServices()
  {
    var services = new ServiceCollection();

    // Cross-window message hub — process-level (the shells bridge it into
    // their window containers).
    services.AddSingleton<IMessageHub>(new MessageHub());

    // The activation agent — registered as a process-level service (the
    // shells bridge it into their window containers).  Factory-registered on
    // purpose: the container disposes what its factory creates, so the
    // process container (released after every shell) reclaims the agent's
    // resources — an instance registration would never be disposed.
    if (CreateActivationAgent() is { } agent)
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

}

