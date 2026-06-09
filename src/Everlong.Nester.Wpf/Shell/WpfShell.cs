using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Everlong.Nester.Activation;
using Everlong.Nester.Controls;
using Everlong.Nester.Presentation;
using Everlong.Nester.Intent;
using Everlong.Nester.Layer;
using Everlong.Nester.Primitives;
using Everlong.Nester.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Everlong.Nester.Shell;

/// <summary>
///   The WPF platform face of <see cref="ShellBase" /> — the window that owns
///   the stage.
/// </summary>
public abstract partial class WpfShell : ShellBase
{
  /// <summary>Creates the shell with the optional startup intent this shell's dispatch starts from.</summary>
  protected WpfShell(IActivationIntent? startupIntent = null)
    : base(startupIntent)
  {
  }

  /// <summary>
  ///   Platform services: the WPF clipboard contract (synchronous text
  ///   surface); the view locator is a framework-built capability — the
  ///   shell scans the application's resources and wraps them; anything
  ///   unknown is null.
  /// </summary>
  public override T? GetPlatformService<T>() where T : class
  {
    if (typeof(T) == typeof(IViewLocator<PControl>))
      return (T?)(object?)GetOrCreateViewLocator();
    if (typeof(T) == typeof(IClipboardService))
      return (T)(object)_clipboard;

    return null;
  }

  private IViewLocator<PControl>? _viewLocator;
  private PApp? _locatorAppSnapshot;

  /// <summary>
  ///   The shell's view locator — a thin translator over the application's
  ///   resources, rebuilt when the application instance changes (tests
  ///   rebuild it per test; a stale snapshot would pin a dead resource
  ///   collection).  Views themselves are always <c>new</c>'d by the
  ///   locator — the container never instantiates views.
  /// </summary>
  private IViewLocator<PControl>? GetOrCreateViewLocator()
  {
    PApp? app = PApp.Current;
    if (app is null)
      return null;
    if (!ReferenceEquals(_locatorAppSnapshot, app))
    {
      _locatorAppSnapshot = app;
      _viewLocator = new CompositeViewLocator();
    }

    return _viewLocator;
  }

  private readonly IClipboardService _clipboard = new WpfClipboardService();

  /// <summary>The virtual visual tree for this shell.</summary>
  internal StagePanel? _stagePanel;

  /// <summary>The shell's host window (desktop — WPF has no single-view).  Assigned by <see cref="PrepareHost"/>; exposed typed via <see cref="Window"/>.</summary>
  protected PWindow? HostWindow { get; set; }

  // ── ILayerBroker (the shell is the broker of its own stage) ──

  /// <inheritdoc />
  protected override ILayerLease CreateLease(ILayerLedger ledger, int z)
    => new ContentLayerLease(new ContentLayer(), ledger, z);

  /// <summary>Creates the stage panel, hands it its owner and connects the broker ledger to it.</summary>
  protected override void PrepareStage()
  {
    _stagePanel ??= new StagePanel();
    _stagePanel.Shell = this;
    ConnectLedger(_stagePanel);
  }

  /// <summary>Connects the ledger to a specific stage panel.</summary>
  internal void ConnectStage(StagePanel panel)
  {
    _stagePanel = panel;
    PrepareStage();
  }

  /// <summary>
  ///   Resolves the host window from the Director via the view locator
  ///   (the <c>[ViewFor&lt;T&gt;]</c> contract).  WPF has no single-view — a
  ///   host-less shell fails fast.
  /// </summary>
  protected override void PrepareHost()
  {
    HostWindow = GetPlatformService<IViewLocator<PControl>>()?.Build(Director!) as PWindow;
    if (HostWindow is not IWpfShellHost)
    {
      throw new InvalidOperationException(
        "WPF shell requires a host window: override PrepareHost (e.g. HostWindow = new MainWindow { Shell = this };)" +
        " or provide a [ViewFor<DirectorType>] mapping the view locator resolves — the window must " +
        "implement IWpfShellHost.  The window presents itself via OnAssembled — " +
        "the framework never falls back for uncooperative views.");
    }
  }

  /// <summary>
  ///   Attaches the shell to the host window (the Director DataContext),
  ///   mounts the stage into the host, then declares the window as the
  ///   application MainWindow (assignment only).
  /// </summary>
  protected override void ConnectHost()
  {
    // The shell wiring: the Director is bound to the host window at mount
    // — a DataContext the user already set wins (never overwritten).  The
    // stage already carries its owner (PrepareStage) — tree code resolves
    // the shell by crawling to the stage.
    if (HostWindow!.DataContext is null)
      HostWindow.DataContext = Director;

    // The framework hands the stage to the host — the host wires it into
    // its own surface (e.g. Window.Content).
    ((IWpfShellHost)HostWindow).HostShell(this, _stagePanel!);

    // The shell's window becomes the application MainWindow (WPF:
    // assignment only; the window presents itself via OnAssembled).
    if (PApp.Current?.MainWindow != HostWindow)
      PApp.Current!.MainWindow = HostWindow;
  }

  /// <inheritdoc />
  protected override IIntentHandler? HostHandler => HostWindow as IIntentHandler;

  // ── Fallback intent handling ──

  /// <inheritdoc />
  protected override async ValueTask HandleFallbackIntentAsync(IntentContext context, IntentDelegate next)
  {
    await next(context);
    if (context.IsTerminated
      || context.Intent is not IShellIntent intent
      || HostWindow is not { } window)
      return;

    switch (intent)
    {
      case TryCloseIntent:
      case CloseIntent:
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
      case MutateShellStateIntent { TargetState: var target }:
        window.WindowState = target.AsWindowState();
        context.Handle(this);
        break;
      case TopmostIntent { IsTopmost: var top }:
        window.Topmost = top;
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

  // ── Window close plumbing ──

  /// <summary>
  ///   Translates a window-closing notification into a <see cref="TryCloseIntent" />
  ///   for unified arbitration through the shell dispatch chain.  The host
  ///   window's <c>OnClosing</c> override calls this; a destroyed shell lets
  ///   the close fall through (no arbitration — the window is already going).
  /// </summary>
  public async void ClosingToTryCloseIntent(CancelEventArgs e)
  {
    if (e.Cancel || Lifetime.Lifecycle != ShellLifecycle.Started)
      return;

    e.Cancel = true;
    await this.DispatchIntent(_stagePanel, new TryCloseIntent());
  }

  /// <summary>
  ///   Translates mouse side buttons (XButton1 / XButton2) into
  ///   <see cref="BackIntent" /> / <see cref="ForwardIntent" />.
  /// </summary>
  public async void MouseSideButtonToRoutingIntent(object? sender, MouseButtonEventArgs e)
  {
    IIntent? intent = e.ChangedButton switch
    {
      MouseButton.XButton1 => new Everlong.Nester.Routing.BackIntent(),
      MouseButton.XButton2 => new Everlong.Nester.Routing.ForwardIntent(),
      _ => null
    };

    if (intent != null)
    {
      e.Handled = true;
      await this.DispatchIntent(sender, intent);
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
      Key.F5 => new Everlong.Nester.Routing.RefreshIntent(),
      Key.Left when (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt =>
        new Everlong.Nester.Routing.BackIntent(),
      Key.Right when (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt =>
        new Everlong.Nester.Routing.ForwardIntent(),
      _ => null
    };

    if (intent != null)
    {
      e.Handled = true;
      await this.DispatchIntent(sender, intent);
    }
  }

  // ── Host status channel ──


  /// <summary>Gets the host's status snapshot, resolved from the shell's service scope.</summary>
  protected HostStatus? Status => field ??= ShellServiceScope?.ServiceProvider.GetService<HostStatus>();

  /// <inheritdoc />
  public void FeedHostPropertyChanged(DependencyPropertyChangedEventArgs e)
  {
    if (Lifetime.Lifecycle == ShellLifecycle.Disposed || Status is not { } status)
    {
      return;
    }

    if (e.Property == Window.WindowStateProperty)
    {
      if (e.OldValue is PWindowState oldState)
        status.LastHostState = oldState.AsShellState();
      if (e.NewValue is PWindowState newState)
        status.HostState = newState.AsShellState();
    }
    else if (e.Property == Window.TopmostProperty)
    {
      status.TopMost = e.NewValue is true;
    }
    else if (e.Property == Window.TitleProperty)
    {
      status.Title = e.NewValue as string ?? string.Empty;
    }
    else if (e.Property == Window.IsActiveProperty)
    {
      status.IsActive = e.NewValue is true;
    }
    else if (e.Property == UIElement.IsVisibleProperty)
    {
      status.IsVisible = e.NewValue is true;
    }
    else if (e.Property == Window.LeftProperty || e.Property == Window.TopProperty
                                               || e.Property == FrameworkElement.WidthProperty ||
                                               e.Property == FrameworkElement.HeightProperty)
    {
      // Bounds components carry a single value with no window instance — the
      // shell reads its own window for the full geometry.
      status.Bounds = new ShellBounds(Window.Left, Window.Top, Window.Width, Window.Height);
    }
  }

  /// <inheritdoc />
  public override nint HostHandle
    => HostWindow is { } window ? new WindowInteropHelper(window).Handle : IntPtr.Zero;

}
