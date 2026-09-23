using Everlong.DI;
using Everlong.Nester.Auth;
using Everlong.Nester.DI;
using Everlong.Nester.Dialog;
using Everlong.Nester.Hosting;
using Everlong.Nester.Intent;
using Everlong.Nester.Messaging;
using Everlong.Nester.Routing;
using Everlong.Nester.Shell;
using Microsoft.Extensions.DependencyInjection;
using NesterApp.Pages.Login;
using NesterApp.Pages.Shell;
using NesterApp.Properties;
using NesterApp.Services;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Everlong.Nester.Activation;

namespace NesterApp;

/// <summary>
///   The app's one shell class — the same user-owned Shell for every shell
///   kind, the kind declared per instance through <see cref="DirectorType" />
///   (set in the object initializer before <see cref="IShell.Start" />).
///   WPF runs Windows only — every shell is a window: the host window is
///   created directly per Director kind in <see cref="ShellBase.PrepareHost" />
///   (wired with this shell).  Platform-specific: this copy derives from the
///   WPF shell (the Avalonia project carries its own AvaloniaShell-derived
///   copy — the class lives per platform, no conditional compilation).
/// </summary>
public sealed partial class UiShell(IActivationIntent? startupIntent = null) : WpfShell(startupIntent)
{
  protected override IServiceProvider InitializeServices()
  {
    // ① Registrations: the Director (resolved via DirectorType — the public
    //    per-instance thread, set before Start), the shell's identity +
    //    framework domains, then the shell's registrations (they win —
    //    last-registration-wins).  User code knows its own directors —
    //    per-shell registrations may branch on DirectorType.
    var services = new ServiceCollection();
    ArgumentNullException.ThrowIfNull(DirectorType);
    services.AddScoped(DirectorType); // the Director — the framework resolves it via DirectorType
    if (DirectorType == typeof(MainViewModel))
    {
      services
        .AddHttpClient<JsonPlaceholderService>(); // main-window-only HTTP surface (the login window never fetches)
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
    services.AddScoped<HostStatus>(); // the window (host) resolves its own status snapshot from the window scope
    services.AddScoped<HostPropertyBase>(sp => sp
                                           .GetRequiredService<
                                             HostStatus>()); // Nester-side (activation agent) resolves by base type

    // ② Every shell builds and owns its provider.
    return services.BuildServiceProvider(new ServiceProviderOptions
    {
      ValidateScopes = true,
      ValidateOnBuild = true
    });
  }

  // ── The host — the user's own window, created per Director kind and wired with this shell ──

  /// <summary>
  ///   The shell's host window — created directly per Director kind (the
  ///   window's Shell is wired here; the framework never touches it).  The
  ///   platform default resolves the host via the view locator
  ///   ([ViewFor&lt;T&gt;] on the window) when this is not overridden.
  /// </summary>
  protected override void PrepareHost()
  {
    HostWindow = Director switch
    {
      MainViewModel => new MainWindow { Shell = this },
      LoginWindowModel => new LoginWindow { Shell = this },
      _ => throw new InvalidOperationException($"No host window for Director {Director?.GetType().Name}"),
    };
  }

  // ── Presentation (the shell's presentation hooks — see OnAssembled / OnStarted) ──

  /// <summary>
  ///   The window appears synchronously — invisible from the first frame:
  ///   the presentation choreography (<see cref="OnStarted" />) fades it in
  ///   once the first navigation settles (no flash, no dead window).
  /// </summary>
  protected override void OnAssembled()
  {
    // ③ The shell's post-assembly init — ROOT-level resolution (the
    //    settings store is a shell Singleton): no scope needed here.
    AppSettings.Default.Inject(this.Services);

    // The backdrop domain rents its own bands — the shell only names the profile.
    Services.GetRequiredService<BackdropService>().Mount(
      DirectorType == typeof(LoginWindowModel) ? BackdropProfile.Login : BackdropProfile.Main);

    Window.Opacity = 0; // public face — non-null (WPF has no single-view)
    Window.Show(); // the host window presents itself — the framework never shows it
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
  /// </summary>
  protected override async Task OnStarted(Task directorStart)
  {
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
    // fade starts — the animation must begin from a rendered frame.
    await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Loaded);

    switch (Window)
    {
      case MainWindow mainWindow:
        await FadeInAsync(mainWindow);
        break;
      case LoginWindow:
        await PlayEntranceAnimationAsync(Window);
        break;
    }
  }

  private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(5);

  private static Task FadeInAsync(Window window)
  {
    var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var animation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250));
    animation.Completed += (_, _) => tcs.TrySetResult();
    window.BeginAnimation(UIElement.OpacityProperty, animation);
    return tcs.Task;
  }

  /// <summary>
  ///   The custom entrance animation — fade + slight scale-up.  This is the
  ///   knob: replace it with any animation to change the window's entrance.
  ///   (A standard-chrome window would ALSO get DWM's native open animation;
  ///   this chrome-less window owns the whole effect.)
  /// </summary>
  private static Task PlayEntranceAnimationAsync(Window window)
  {
    var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

    var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250))
    {
      EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
    };
    fade.Completed += (_, _) => tcs.TrySetResult();
    window.BeginAnimation(UIElement.OpacityProperty, fade);

    // Scale 0.95 → 1 (the window's own render transform — DWM has no say here).
    //var scale = new ScaleTransform(0.95, 0.95);
    //window.RenderTransform = scale;
    //var scaleAnim = new DoubleAnimation(0.95, 1, TimeSpan.FromMilliseconds(250))
    //{
    //  EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
    //};
    //scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
    //scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);

    return tcs.Task;
  }

  protected override async ValueTask HandleFallbackIntentAsync(IntentContext context, IntentDelegate next)
  {
    await next(context);
    if (context.IsTerminated)
      return;

    if (context.Intent is not IWindowIntent and not IShellIntent)
      return;

    switch (context.Intent)
    {
      case ShowIntent:
        Window.Show();
        context.Handle(this);
        break;
      case MutateShellStateIntent { TargetState: var target }:
        Window.WindowState = target.AsWindowState();
        context.Handle(this);
        break;
      case RestoreShellStateIntent:
        if (Status is { } status)
          Window.WindowState = status.LastHostState.AsWindowState();
        context.Handle(this);
        break;
      case TopmostIntent { IsTopmost: var top }:
        Window.Topmost = top;
        context.Handle(this);
        break;
      case TryCloseIntent:
      case CloseIntent:
        // The intent reached the last link = nobody vetoed: destroy the
        // shell, then close the window (the re-entrant OnClosing falls
        // through — the shell is already disposed).  The close translation
        // is the framework's only window hook; veto lives in the intent
        // chain.
        await DisposeAsync();
        try
        {
          Window.Close();
        }
        catch
        {
          // best-effort: the window may already be closed
        }

        context.Handle(this);
        break;
    }
  }
}
