using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Everlong.Nester.Activation;
using Everlong.Nester.Controls;
using Everlong.Nester.Presentation;
using Everlong.Nester.Hosting;
using Everlong.Nester.Intent;
using Everlong.Nester.Layer;
using Everlong.Nester.Primitives;
using Everlong.Nester.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Everlong.Nester.Shell;

/// <summary>
///   The Avalonia platform face of <see cref="ShellBase" /> — the window
///   (or single-view host) that owns the stage.
/// </summary>
public abstract partial class AvaloniaShell : ShellBase
{
  /// <summary>Creates the shell with the optional startup intent this shell's dispatch starts from.</summary>
  protected AvaloniaShell(IActivationIntent? startupIntent = null)
    : base(startupIntent)
  {
  }

  /// <remarks> may or may not be a window, depends on ApplicationLifetime </remarks>
  private PControl? TopView => (PControl?)ShellHost ?? StagePanel;

  /// <summary>The virtual visual tree for this shell.</summary>
  internal StagePanel? StagePanel;

  /// <summary>
  ///   The shell's host surface — the desktop window or the single-view
  ///   host view; <see langword="null"/> = direct mount (the stage itself
  ///   becomes the MainView).  Assigned by <see cref="PrepareHost"/>.
  /// </summary>
  protected PContentControl? ShellHost { get; set; }

  /// <summary>
  ///   The shell's platform top-level — the host window on desktop; in
  ///   single-view, the platform top-level the surface is attached to
  ///   (available once the surface is in the visual tree).
  /// </summary>
  internal TopLevel? TopLevel
    => ShellHost as TopLevel ?? TopLevel.GetTopLevel(TopView);

  /// <summary>
  ///   Platform services: Avalonia native services pass through as-is
  ///   (the probe type IS the contract); the view locator is a framework-built
  ///   capability — the shell scans the application's global template
  ///   collection (the platform singleton's knowledge) and wraps it;
  ///   anything unknown is null.
  /// </summary>
  public override T? GetPlatformService<T>() where T : class
  {
    if (typeof(T) == typeof(IViewLocator<PControl>))
      return (T?)GetOrCreateViewLocator();
    if (typeof(T) == typeof(IClipboard))
      return (T?)TopLevel?.Clipboard;
    if (typeof(T) == typeof(IStorageProvider))
      return (T?)TopLevel?.StorageProvider;
    if (typeof(T) == typeof(ILauncher))
      return (T?)TopLevel?.Launcher;

    return null;
  }

  private IViewLocator<PControl>? _viewLocator;

  private PApp? _locatorAppSnapshot;

  /// <summary>
  ///   The shell's view locator — a thin translator over the application's
  ///   global template collection, rebuilt when the application instance
  ///   changes (tests rebuild it per test; a stale snapshot would pin a dead
  ///   template collection).  Views themselves are always <c>new</c>'d by
  ///   the locator — the container never instantiates views.
  /// </summary>
  private IViewLocator<PControl>? GetOrCreateViewLocator()
  {
    PApp? app = PApp.Current;
    if (app is null)
      return null;
    if (!ReferenceEquals(_locatorAppSnapshot, app))
    {
      _locatorAppSnapshot = app;
      _viewLocator = new CompositeViewLocator(app.DataTemplates);
    }

    return _viewLocator;
  }

  // ── ILayerBroker (the shell is the broker of its own stage) ──

  /// <inheritdoc />
  protected override ILayerLease CreateLease(ILayerLedger ledger, int z)
    => new ContentLayerLease(new ContentLayer(), ledger, z);

  /// <summary>Creates the stage panel, hands it its owner and connects the broker ledger to it.</summary>
  protected override void PrepareStage()
  {
    StagePanel ??= new StagePanel();
    StagePanel.Shell = this;
    ConnectLedger(StagePanel);
  }

  /// <summary>Connects the ledger to a specific stage panel.</summary>
  internal void ConnectStage(StagePanel panel)
  {
    StagePanel = panel;
    PrepareStage();
  }

  /// <summary>
  ///   Resolves the host surface from the Director via the view locator
  ///   (the <c>[ViewFor&lt;T&gt;]</c> contract).  Single-view with no mapping
  ///   = direct mount (the stage itself becomes the MainView); desktop
  ///   without a host fails fast.
  /// </summary>
  protected override void PrepareHost()
  {
    ShellHost = GetPlatformService<IViewLocator<PControl>>()?.Build(Director!) as PContentControl;
    if (ShellHost is IAvaloniaShellHost)
      return;

    // No usable host: single-view direct-mounts (the framework connects the
    // stage as the MainView); desktop has no fallback — fail fast.
    ShellHost = null;
    EnsureDesktopHost();
  }

  /// <summary>
  ///   Fails fast when no host surface exists on desktop — a host-less
  ///   desktop shell must never touch the visual root.
  /// </summary>
  protected void EnsureDesktopHost()
  {
    if (ShellHost is null && !SingleViewLifetime.IsSingleView(PApp.Current?.ApplicationLifetime))
    {
      throw new InvalidOperationException(
        "Desktop shell requires a host: override PrepareHost (e.g. HostSurface = new MainWindow { Shell = this };)" +
        " or provide a [ViewFor<DirectorType>] mapping the view locator resolves — the host must " +
        "implement IAvaloniaShellHost (a Window).  The window presents itself via OnAssembled — " +
        "the framework never falls back for uncooperative views.");
    }
  }

  /// <summary>
  ///   Attaches the shell to the visual root (the Director DataContext),
  ///   mounts the stage into the host (or direct-mounts it), presents
  ///   (single-view MainView) and promotes (desktop MainWindow).
  /// </summary>
  protected override void ConnectHost()
  {
    // The shell wiring: the Director is bound to the host at mount — a
    // DataContext the user already set wins (never overwritten).  The
    // stage already carries its owner (PrepareStage) — tree code resolves
    // the shell by crawling to the stage.
    if (ShellHost is { DataContext: null } root)
      root.DataContext = Director;

    // The framework hands the stage to the host — the host wires it into
    // its own surface (hosting is the host's presentation job).  The
    // framework never hooks input itself: navigation input translation
    // (mouse side buttons, keyboard shortcuts) is the app's choice —
    // wire it via OnTopLevelConnected (the ready-made translators are
    // MouseSideButtonToRoutingIntent / KeyDownToRoutingIntent).
    if (ShellHost is { } host)
    {
      ((IAvaloniaShellHost)host).HostShell(this, StagePanel!);
    }

    // The surface connects its platform TopLevel (single-view: the attach
    // wait — a surface's TopLevel exists only after the platform host
    // exists) or is itself the TopLevel (desktop: a Window is its own
    // TopLevel — the hook connects immediately, no wait).
    var lifetime = PApp.Current?.ApplicationLifetime;
    if (SingleViewLifetime.IsSingleView(lifetime))
    {
      if (ShellHost is { } surface)
      {
        NotifyTopLevelConnected(surface);
        SingleViewLifetime.SetMainView(lifetime, surface);
      }
      else
      {
        NotifyTopLevelConnected(StagePanel!);
        SingleViewLifetime.SetMainView(lifetime, StagePanel!);
      }
    }
    // Desktop: the shell's window — itself a TopLevel — connects the hook
    // immediately and becomes the application MainWindow (Avalonia: the
    // setter also shows — idempotent).
    else if (ShellHost is PWindow window)
    {
      OnTopLevelConnected(window);

      if (lifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
        desktopLifetime.MainWindow = window;
    }
  }

  /// <inheritdoc />
  protected override IIntentHandler? HostHandler => ShellHost as IIntentHandler;

  // ── Fallback intent handling ──

  // Shell is the LAST link of the intent chain (layers → Director → host
  // → fallback).  The sealed dispatcher fixes the environment dispatch —
  // android → iOS → browser → single-view → desktop-window; each branch
  // walks the tunnel first, then handles what fell through.  Subclasses
  // extend by overriding the branch their platform crosses.
  /// <inheritdoc />
  protected sealed override ValueTask HandleFallbackIntentAsync(IntentContext context, IntentDelegate next)
  {
    if (AppLifetime.IsAndroid)
    {
      return HandleAndroidIntent(context, next);
    }

    if (AppLifetime.IsIOS)
    {
      return HandleIOSIntent(context, next);
    }

    if (AppLifetime.IsBrowser)
    {
      return HandleWasmIntent(context, next);
    }

    if (SingleViewLifetime.IsSingleView(PApp.Current?.ApplicationLifetime))
    {
      return HandleSingleViewIntentAsync(context, next);
    }

    if (Window is not { } window)
      return default;

    return HandleDesktopIntent(context, next);
  }

  /// <summary>
  ///   The desktop branch — walks the tunnel, then handles the window intents
  ///   and the shell-lifecycle pair.
  /// </summary>
  protected virtual async ValueTask HandleDesktopIntent(IntentContext context, IntentDelegate next)
  {
    await next(context);
    if (context.IsTerminated)
      return;

    if (context.Intent is not IWindowIntent and not IShellIntent)
      return;

    if (Window is not { } window)
      return;

    switch (context.Intent)
    {
      case CloseIntent:
      case TryCloseIntent:
        // The intent reached the last link = nobody vetoed: destroy the
        // shell, then close the window (the re-entrant OnClosing falls
        // through — the shell is already disposed).  Awaiting makes the
        // close deterministic for the caller (e.g. the window-close
        // translation returns with the shell destroyed).
        //
        // NO LOCK — by design: DisposeAsync's Interlocked guard is set
        // synchronously BEFORE its first await, so any second entrant
        // (re-entrant OnClosing or a concurrent dispatch) no-ops at the
        // entry and the teardown sequence runs exactly once; a repeated
        // window.Close() is either a platform exception (caught below) or
        // a harmless OnClosing re-entry gated by Lifecycle.  An async lock
        // here would DEADLOCK (the first await yields the UI thread, a
        // re-entrant UI-thread waiter blocks the continuation).
        await DisposeAsync();
        try
        {
          window.Close();
        }
        catch
        {
          // best-effort: the window may already be closed
        }
        context.Handle(this);
        break;
      case HideIntent:
        window.Hide();
        context.Handle(this);
        break;
      case ShowIntent:
        window.Show();
        context.Handle(this);
        break;
      case MutateShellStateIntent stateIntent:
        window.WindowState = FromShellState(stateIntent.TargetState);
        context.Handle(this);
        break;
      case TopmostIntent top:
        window.Topmost = top.IsTopmost;
        context.Handle(this);
        break;
      case RestoreShellStateIntent:
        // The shell mirrors the host's state — restore from its snapshot.
        if (Status is { } status)
          window.WindowState = status.LastHostState.AsWindowState();
        context.Handle(this);
        break;
      case CenterOnScreenIntent:
        window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        context.Handle(this);
        break;
    }
  }

  /// <summary>The single-view branch — walks the tunnel, then handles the single-view close family.</summary>
  protected virtual async ValueTask HandleSingleViewIntentAsync(IntentContext context, IntentDelegate next)
    => await HandleSingleViewFamilyAsync(context, next);

  /// <summary>The Android branch — walks the tunnel, then handles the single-view close family.</summary>
  protected virtual async ValueTask HandleAndroidIntent(IntentContext context, IntentDelegate next)
    => await HandleSingleViewFamilyAsync(context, next);

  /// <summary>The iOS branch — walks the tunnel, then handles the single-view close family.</summary>
  protected virtual async ValueTask HandleIOSIntent(IntentContext context, IntentDelegate next)
    => await HandleSingleViewFamilyAsync(context, next);

  /// <summary>The browser branch — walks the tunnel, then handles the single-view close family.</summary>
  protected virtual async ValueTask HandleWasmIntent(IntentContext context, IntentDelegate next)
    => await HandleSingleViewFamilyAsync(context, next);

  private async ValueTask HandleSingleViewFamilyAsync(IntentContext context, IntentDelegate next)
  {
    await next(context);
    if (context.IsTerminated)
      return;

    if (context.Intent is CloseIntent or TryCloseIntent)
    {
      // Single-view surface detach lives inside DisposeAsync (MainView
      // ownership check) — no window to close here.
      await DisposeAsync();
      context.Handle(this);
    }
  }

  // ── Window close plumbing ──

  /// <summary>
  ///   Translates a window-closing notification into a <see cref="TryCloseIntent" />
  ///   for unified arbitration through the shell dispatch chain.  The host
  ///   window's <c>OnClosing</c> override calls this; a destroyed shell lets
  ///   the close fall through (no arbitration — the window is already going).
  /// </summary>
  public async void WindowClosingToTryCloseIntent(WindowClosingEventArgs e)
  {
    if (e.Cancel || Lifetime.Lifecycle != ShellLifecycle.Started)
      return;

    e.Cancel = true;
    await this.DispatchIntent(StagePanel, new TryCloseIntent());
  }

  /// <summary>
  ///   Destroys the shell and detaches the single-view surface: layer leases,
  ///   container, then <c>MainView</c> — only when the current MainView still
  ///   belongs to this shell (a replaced shell must never clear the new one).
  /// </summary>
  public override async ValueTask DisposeAsync()
  {
    await base.DisposeAsync();

    // Single-view surface detach — only when the current view still belongs
    // to this shell (a replaced shell must never clear the new one).
    var lifetime = PApp.Current?.ApplicationLifetime;
    SingleViewLifetime.ClearIfOwned(lifetime, StagePanel);
    SingleViewLifetime.ClearIfOwned(lifetime, ShellHost);
  }

  /// <summary>
  ///   Translates mouse side buttons (XButton1 / XButton2) into
  ///   <see cref="BackIntent" /> / <see cref="ForwardIntent" />.
  /// </summary>
  public async void MouseSideButtonToRoutingIntent(object? sender, PointerReleasedEventArgs e)
  {
    if (e.Handled)
      return;
    var point = e.GetCurrentPoint(TopView);
    if (!TopView!.Bounds.Contains(point.Position))
      return;

    switch (point.Properties.PointerUpdateKind)
    {
      case PointerUpdateKind.XButton1Released:
        e.Handled = true;
        await this.DispatchIntent(null, new BackIntent());
        break;
      case PointerUpdateKind.XButton2Released:
        e.Handled = true;
        await this.DispatchIntent(null, new ForwardIntent());
        break;
    }
  }

  /// <summary>
  ///   Translates keyboard shortcuts into navigation intents
  ///   (F5 → Refresh, Alt+Left → Back, Alt+Right → Forward).
  /// </summary>
  public async void KeyDownToRoutingIntent(object? sender, KeyEventArgs e)
  {
    IIntent? intent = e.Key switch
    {
      Key.F5 => new RefreshIntent(),
      Key.Left when e is { KeyModifiers: KeyModifiers.Alt } => new BackIntent(),
      Key.Right when e is { KeyModifiers: KeyModifiers.Alt } => new ForwardIntent(),
      _ => null
    };

    if (intent != null)
    {
      e.Handled = true;
      await this.DispatchIntent(StagePanel, intent);
    }
  }

  /// <summary>
  ///   The shell's surface connected to its platform <see cref="TopLevel" />:
  ///   the single-view surface gains the platform host after attach; the
  ///   desktop host window is itself the TopLevel.  Fires once per shell.
  /// </summary>
  /// <remarks>
  ///   The framework wires nothing — hook <c>TopLevel.BackRequested</c> for
  ///   the system back (<see cref="BackRequestToBackIntent" />); hook
  ///   <c>PointerReleasedEvent</c> / <c>KeyDownEvent</c> for mouse side
  ///   buttons / keyboard shortcuts (<see cref="MouseSideButtonToRoutingIntent" />
  ///   / <see cref="KeyDownToRoutingIntent" />).  Wire only what the app
  ///   wants.
  /// </remarks>
  protected virtual void OnTopLevelConnected(TopLevel topLevel) { }

  /// <summary>Notifies <see cref="OnTopLevelConnected" /> once the surface gains its TopLevel (the platform host exists only after attach).</summary>
  private void NotifyTopLevelConnected(Control view)
  {
    view.AttachedToVisualTree += OnSingleViewAttached;
  }

  private void OnSingleViewAttached(object? sender, VisualTreeAttachmentEventArgs e)
  {
    if (sender is Control { } view && TopLevel.GetTopLevel(view) is { } topLevel)
    {
      OnTopLevelConnected(topLevel);
    }
  }

  /// <summary>Translates the platform system-back request into a <see cref="BackIntent" /> through the intent chain — ready-made for <see cref="OnTopLevelConnected" /> (unconditionally handled: consumption lives in the intent chain, the Director is the last word).</summary>
  public async void BackRequestToBackIntent(object? sender, RoutedEventArgs e)
  {
    e.Handled = true;
    await this.DispatchIntent(StagePanel, new BackIntent());
  }

  private static PWindowState FromShellState(HostState state) =>
    state switch
    {
      HostState.Normal => PWindowState.Normal,
      HostState.Minimized => PWindowState.Minimized,
      HostState.Maximized => PWindowState.Maximized,
      HostState.FullScreen => PWindowState.FullScreen,
      _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
    };

  // ── Host status channel ──

  private HostProperty? Status => field ??= ShellServiceScope?.ServiceProvider.GetService<HostProperty>();

  /// <inheritdoc />
  public void FeedHostPropertyChanged(AvaloniaPropertyChangedEventArgs e)
  {
    if (Lifetime.Lifecycle == ShellLifecycle.Disposed || Status is not { } status)
    {
      return;
    }

    if (e.Property == WindowBase.IsActiveProperty)
      status.IsActive = e.NewValue is true;
    else if (e.Property == WindowBase.TopmostProperty)
      status.TopMost = e.NewValue is true;
    else if (e.Property == Window.TitleProperty)
      status.Title = e.NewValue as string ?? string.Empty;
    else if (e.Property == PVisual.BoundsProperty)
      status.Bounds = e.NewValue is PRect rect
                        ? new ShellBounds(rect.X, rect.Y, rect.Width, rect.Height)
                        : ShellBounds.Empty;
    else if (e.Property == PVisual.IsVisibleProperty)
      status.IsVisible = e.NewValue is true;
    else if (e.Property == Window.WindowStateProperty)
    {
      if (e.OldValue is PWindowState oldState)
        status.LastHostState = oldState.AsShellState();
      if (e.NewValue is PWindowState newState)
        status.HostState = newState.AsShellState();
    }
  }

  /// <inheritdoc />
  public override nint HostHandle => Window?.TryGetPlatformHandle()?.Handle ?? 0;

}


