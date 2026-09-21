using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Transformation;
using Avalonia.Styling;
using Everlong.DI;
using Everlong.Nester.Auth;
using Everlong.Nester.DI;
using Everlong.Nester.Dialog;
using Everlong.Nester.Hosting;
using Everlong.Nester.Messaging;
using Everlong.Nester.Routing;
using Everlong.Nester.Shell;
using Microsoft.Extensions.DependencyInjection;
using NesterApp.Pages.Login;
using NesterApp.Pages.Shell;
using NesterApp.Properties;
using NesterApp.Services;
using Everlong.Nester.Activation;

namespace NesterApp;

/// <summary>
///   The app's one shell class — the same user-owned Shell for every shell
///   kind, the kind declared per instance through <see cref="DirectorType" />
///   (set in the object initializer before <see cref="IShell.Start" />).
///   Single-view (Android / WASM): no host — the framework direct-mounts the
///   stage as the MainView.  Desktop: the host window is created directly
///   per Director kind in <see cref="ShellBase.PrepareHost" /> (wired with
///   this shell) — the host is the concrete shell's to declare.
///   Platform-specific: this copy derives from the
///   Avalonia shell (the WPF project carries its own WpfShell-derived copy
///   — the class lives per platform, no conditional compilation).
/// </summary>
public sealed partial class UiShell(IActivationIntent? startupIntent = null) : AvaloniaShell(startupIntent)
{
  protected override IServiceProvider InitializeServices()
  {
    // ① Registrations: the Director (resolved via DirectorType — the public
    //    per-instance thread, set before Start), the shell's identity +
    //    framework domains, then the shell's registrations (they win —
    //    last-registration-wins).  User code knows its own directors —
    //    per-shell registrations may branch on DirectorType.
    var services = new ServiceCollection();
    ArgumentNullException.ThrowIfNull(DirectorType, nameof(DirectorType));
    services.AddScoped(DirectorType); // the Director — the framework resolves it via DirectorType
    if (DirectorType == typeof(MainViewModel))
    {
      services
        .AddHttpClient<
          JsonPlaceholderService>(); // main-window/single-view HTTP surface (the login window never fetches)
    }

    services.AddNesterShell(this); // the shell's identity: IShell / ILayerBroker / the concrete type
    services.AddNesterCore(); // core infrastructure (logging / injector / options / flying layer)

    // Route-level authorization — the demo's own wiring (see AuthorizedRouter):
    // the generated registry (AuthRegistry.cs) and the router subclass that
    // evaluates every Route request against it replace the default router.
    services.AddSingleton<IAuthRegistry, AppAuthRegistry>();
    services.AddScoped<IRouter, AuthorizedRouter>();

    services.AddNesterDialog(); // the dialog domain
    services.AddNesterNotice();
    services.BridgeSingleton<IAuthService>(); // cross-window shared login state (bridged from the process container — Auth lives process-level now)
    services.BridgeSingleton<IMessageHub>(); // cross-window shared broadcast hub (bridged from the process container)
    BridgeActivationAgent(services); // optional — an agent-less app has no activation surface
    services.AddServices(new AppServices());
    services.AddScoped<HostProperty>(); // the window (host) resolves its own status snapshot from the window scope
    // Nester-side (activation agent) resolves by base type
    services.AddScoped<HostPropertyBase>(sp => sp.GetRequiredService<HostProperty>());

    // ② Every shell builds and owns its provider.
    return services.BuildServiceProvider(new ServiceProviderOptions
    {
      ValidateScopes = true,
      ValidateOnBuild = true
    });
  }

  // ── The host — the user's own surface, created per shell kind and wired with this shell ──

  /// <summary>
  ///   The shell's host surface.  Single-view: no host — the framework's
  ///   default direct-mounts the stage as the MainView.  Desktop: the host
  ///   window, created directly per Director kind (the window's Shell is
  ///   wired here; the framework never touches it).
  /// </summary>
  protected override void PrepareHost()
  {
    if (AppLifetime.IsSingleView)
    {
      return; // direct mount — the stage itself becomes the MainView
    }

    ShellHost = Director switch
    {
      MainViewModel => new MainWindow(),
      LoginWindowModel => new LoginWindow { Shell = this },
      _ => throw new InvalidOperationException($"No host window for Director {Director?.GetType().Name}"),
    };
  }

  protected override void OnTopLevelConnected(TopLevel topLevel)
  {
    if (topLevel is MainWindow mainWindow)
    {
      mainWindow.AddHandler(InputElement.PointerReleasedEvent, MouseSideButtonToRoutingIntent, RoutingStrategies.Tunnel,
                            handledEventsToo: true);
    }
    else if (AppLifetime.IsAndroid)
    {
      // Platform seam — the framework gives the line, the template connects
      // it: the Android system back key becomes a BackIntent through the
      // intent chain (the router consumes back; a page vetoes with true).
      // The browser's back surface is handled the same way when the
      // platform raises it.
      topLevel.BackRequested += BackRequestToBackIntent;
    }
  }

  /// <summary>
  ///   The window appears synchronously — invisible from the first frame:
  ///   the presentation choreography (<see cref="OnStarted" />) fades it in
  ///   once the first navigation settles (no flash, no dead window).
  /// </summary>
  /// <remarks>
  ///   The READY ANCHOR: <see cref="ShellBase.InitializeServices" /> decides
  ///   how the container is assembled; this hook is the shell's own
  ///   post-assembly step.  Per-director assembly (pre-flight resolution,
  ///   background services) belongs in the Director's
  ///   <see cref="IShellDirector.OnAssembled" />, which runs immediately
  ///   after this hook.
  /// </remarks>
  protected override void OnAssembled()
  {
    // ③ The shell's post-assembly init — ROOT-level resolution (the
    //    settings store is a shell Singleton): no scope needed here.
    AppSettings.Default.Inject(Services);

    // The backdrop domain rents its own bands — the shell only names the profile.
    Services.GetRequiredService<BackdropService>().Mount(
      DirectorType == typeof(LoginWindowModel) ? BackdropProfile.Login : BackdropProfile.Main);

    if (Window != null)
    {
      Window.Opacity = 0;
      Window.ShowActivated = false;
      Window.Show(); // the host window presents itself — the framework never shows it
    }
  }

  /// <summary>
  ///   Bridges the process-level activation agent into this window's
  ///   container (optional — no agent registered = no activation surface).
  ///   The framework binds the resolved agent at start.
  /// </summary>
  private static void BridgeActivationAgent(IServiceCollection services)
  {
    if (AppLifetime.Current?.Services?.GetService<IActivationAgent>() is { } agent)
    {
      services.AddSingleton(agent);
    }
  }

  /// <summary>
  ///   The startup presentation choreography: wait for the first navigation
  ///   to settle (bounded), render-sync, then the per-window entrance.
  ///   (Single-view: no host window — nothing to present.)
  /// </summary>
  protected override async Task OnStarted(Task directorStart)
  {
    if (Window is null)
    {
      return; // single-view: no window to present
    }

    try
    {
      // Wait for the first navigation to settle (bounded — a stuck coroutine
      // must never leave the window invisible forever).
      await Task.WhenAny(directorStart, Task.Delay(StartupTimeout));
    }
    catch
    {
      // Startup failed — still present: an error surface beats a window that
      // never appears.
    }

    // Render-sync: the invisible first frame must be on screen before the
    // animation starts — it must begin from a rendered frame.
    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { }, Avalonia.Threading.DispatcherPriority.Loaded);

    switch (Window)
    {
      case MainWindow mainWindow:
        await FadeInAsync(mainWindow);
        break;
      case LoginWindow:
        await PlayEntranceAnimationAsync(Window!);
        break;
    }

    Window?.Activate();
  }

  private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(5);

  private static async Task FadeInAsync(Window window)
  {
    var animation = new Animation
    {
      Duration = TimeSpan.FromMilliseconds(250),
      FillMode = FillMode.Forward,
      Children =
      {
        new KeyFrame { Cue = new Cue(0d), Setters = { new Setter(Visual.OpacityProperty, 0d) } },
        new KeyFrame { Cue = new Cue(1d), Setters = { new Setter(Visual.OpacityProperty, 1d) } },
      },
    };
    await animation.RunAsync(window);
  }

  /// <summary>
  ///   The custom entrance animation — fade + slight scale-up.  This is the
  ///   knob: replace it with any animation to change the window's entrance.
  ///   (A standard-chrome window would ALSO get the platform's native open
  ///   animation; this chrome-less window owns the whole effect.)
  /// </summary>
  private static async Task PlayEntranceAnimationAsync(Window window)
  {
    // Property-change transitions: assigning Opacity/RenderTransform below
    // plays them.  Transitions have no completion event — the duration wait
    // is the choreography's pacing (fine for an entrance).
    window.Transitions =
    [
      new DoubleTransition { Property = Visual.OpacityProperty, Duration = TimeSpan.FromMilliseconds(250) },
      new TransformOperationsTransition
        { Property = Visual.RenderTransformProperty, Duration = TimeSpan.FromMilliseconds(250) },
    ];

    window.RenderTransform = TransformOperations.Parse("scale(0.95)");
    window.Opacity = 1; // trigger the entrance

    // The scale transition needs a second assignment to run: the first one
    // establishes the start value, the second animates it back to identity
    // (a single assignment would leave the window permanently scaled).
    await Task.Delay(TimeSpan.FromMilliseconds(16));
    window.RenderTransform = TransformOperations.Parse("scale(1)");

    await Task.Delay(TimeSpan.FromMilliseconds(250));
  }
}
