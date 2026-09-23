using Everlong.Nester.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using Everlong.Nester.Presentation;
using Everlong.Nester.Dialog;
using Everlong.Nester.Intent;
using Everlong.Nester.Routing;
using Everlong.Nester.Shell;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Everlong.Nester.Tests.Shell;

using static Everlong.Nester.Tests.AsyncTestHelpers;

/// <summary>
///   Dialog-overlay behaviors (headless): the countdown
///   confirm flow through the real pipeline, the wait session's dispose
///   close, the overlay's focus trap, the closing exit choreography, and
///   the session's own close under stacking (targeted dismissal).
/// </summary>
[Collection("RealShell")]
public class DialogOverlayBehaviorTests
{
  public sealed class ProbeDialog : DialogSessionBase<object?>
  {
  }

  /// <summary>Dialog view with an exit-animation counter.</summary>
  public sealed class ExitingDialogView : ContentControl, ISceneTransition
  {
    public int EnterCount { get; private set; }
    public int ExitCount { get; private set; }

    public Task AnimateEnterAsync(TransitionContext ctx, CancellationToken token)
    {
      EnterCount++;
      return Task.CompletedTask;
    }

    public Task AnimateExitAsync(TransitionContext ctx, CancellationToken token)
    {
      ExitCount++;
      return Task.CompletedTask;
    }
  }

  public sealed class ExitingDialog : DialogSessionBase<object?>
  {
  }

  /// <summary>A sized dialog view that records its layout state when the director runs.</summary>
  public sealed class LaidOutDialogView : ContentControl, ISceneTransition
  {
    public LaidOutDialogView()
    {
      Width = 200;
      Height = 120;
    }

    /// <summary>Whether <see cref="AnimateEnterAsync" /> has been entered.</summary>
    public bool EnterRan { get; private set; }

    /// <summary>Whether the view was attached when <see cref="AnimateEnterAsync" /> ran.</summary>
    public bool AttachedAtEnter { get; private set; }

    /// <summary>The view's measured width when <see cref="AnimateEnterAsync" /> ran.</summary>
    public double WidthAtEnter { get; private set; }

    /// <summary>The view's measured height when <see cref="AnimateEnterAsync" /> ran.</summary>
    public double HeightAtEnter { get; private set; }

    public Task AnimateEnterAsync(TransitionContext context, CancellationToken token)
    {
      EnterRan = true;
      AttachedAtEnter = this.IsAttachedToVisualTree();
      WidthAtEnter = Bounds.Width;
      HeightAtEnter = Bounds.Height;
      return Task.CompletedTask;
    }

    public Task AnimateExitAsync(TransitionContext context, CancellationToken token) => Task.CompletedTask;
  }

  public sealed class LaidOutDialog : DialogSessionBase<object?>
  {
  }

  /// <summary>A dialog view that parks its enter animation until released.</summary>
  public sealed class BlockingDialogView : ContentControl, ISceneTransition
  {
    private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Whether <see cref="AnimateEnterAsync" /> has been entered.</summary>
    public bool Entered { get; private set; }

    public Task AnimateEnterAsync(TransitionContext context, CancellationToken token)
    {
      Entered = true;
      return _release.Task;
    }

    public Task AnimateExitAsync(TransitionContext context, CancellationToken token) => Task.CompletedTask;

    /// <summary>Lets the parked enter animation complete.</summary>
    public void Release() => _release.TrySetResult();
  }

  public sealed class BlockingDialog : DialogSessionBase<object?>
  {
  }

  private sealed class ViewLocator : IDataTemplate
  {
    public bool Match(object? data)
      => data is ProbeDialog or ExitingDialog or LaidOutDialog or BlockingDialog or ConfirmDialogSession;

    public Control? Build(object? data) => data switch
    {
      ExitingDialog => new ExitingDialogView(),
      LaidOutDialog => new LaidOutDialogView(),
      BlockingDialog => new BlockingDialogView(),
      _ => new ContentControl()
    };
  }

  private static (RealShell Stage, StagePanel Panel, AvaloniaShell Shell, IServiceProvider Sp) CreateShell()
  {
    var shell = RealShell.Create<RealShell.RealTestContext>(
      register: s => { },
      template: new ViewLocator());
    return (shell, shell.Panel, shell.Shell, shell.Services);
  }

  /// <summary>A real host window — the stage joins a visual root, so the
  /// arriving view's layout is observable at all.</summary>
  private sealed class HostWindow : Window, IAvaloniaShellHost
  {
    public void HostShell(IAvaloniaShell shell, Control stage) => Content = stage;
  }

  private static (RealShell Stage, StagePanel Panel, AvaloniaShell Shell, IServiceProvider Sp) CreateWindowedShell()
  {
    var window = new HostWindow { Width = 900, Height = 600 };
    var shell = RealShell.Create<RealShell.RealTestContext>(
      register: s => { },
      template: new ViewLocator(),
      rootView: window);
    return (shell, shell.Panel, shell.Shell, shell.Services);
  }

  private static RoutingView SingleCapsule(StagePanel panel)
    => Assert.Single(panel.DerivedHosts());

  private static ContentLayer? CapsuleLayerOf(StagePanel panel, object session)
    => panel.DerivedLayers()
            .FirstOrDefault(layer =>
              ReferenceEquals(((RoutingView)layer.Content!).Location!.Instance, session));

  // ── ConfirmAsync's countdown runs the real pipeline — the button is disabled while it counts, enabled after ──

  [AvaloniaFact]
  public async Task ConfirmAsync_WithCountdown_ThroughRealPipeline()
  {
    var (_, panel, _, sp) = CreateShell();
    var dialogs = sp.GetRequiredService<IRouter>();

    Task<bool> confirmTask = dialogs.ConfirmAsync("proceed?",
      new ConfirmOptions { CountdownSeconds = 1, ConfirmText = "Confirm", CancelText = "Cancel" });
    await WaitUntilAsync(() => panel.DerivedHosts().Count() == 1);

    var session = Assert.IsType<ConfirmDialogSession>(SingleCapsule(panel).Location!.Instance);
    Assert.False(session.IsConfirmEnabled);   // disabled during the countdown

    // After the countdown settles the button enables.
    await WaitUntilAsync(() => session.IsConfirmEnabled, timeoutMs: 5000);
    Assert.Equal("Confirm", session.ConfirmText);

    // Confirming closes the overlay and settles the confirm task with true.
    session.ConfirmCommand.Execute(null);
    Assert.True(await confirmTask.WaitAsync(TimeSpan.FromSeconds(5)));
    Assert.Empty(panel.DerivedHosts());
  }

  // ── the wait session — CreateWait presents the overlay, Dispose closes ──

  [AvaloniaFact]
  public async Task WaitDialog_Dispose_ClosesItsCapsule()
  {
    var (_, panel, _, sp) = CreateShell();
    var dialogs = sp.GetRequiredService<IRouter>();

    using WaitDialogSession wait = dialogs.CreateWait("Working...");
    await WaitUntilAsync(() => panel.DerivedHosts().Count() == 1);
    Assert.IsType<WaitDialogSession>(SingleCapsule(panel).Location!.Instance);

    wait.Dispose();   // the session settles → its overlay closes
    await WaitUntilAsync(() => panel.DerivedHosts().Count() == 0);
  }

  // ── modal policy — the dialog overlay declares Trapped (a lone floating layer; Tab stays inside) ──

  [AvaloniaFact]
  public async Task DialogCapsule_DeclaresTrappedFocusPolicy()
  {
    var (_, panel, _, sp) = CreateShell();
    var dialogs = sp.GetRequiredService<IRouter>();
    var session = new ProbeDialog();

    Task showTask = dialogs.ShowAsync(session);
    await WaitUntilAsync(() => panel.DerivedHosts().Count() == 1);

    ContentLayer layer = Assert.IsType<ContentLayer>(CapsuleLayerOf(panel, session));
    var policy = Assert.IsAssignableFrom<IFocusPolicySurface>(layer.Content);
    Assert.Equal(FocusPolicy.Trapped, policy.FocusPolicy);

    session.Close();
    await showTask;
  }

  // ── closing runs the exit choreography — the session view's AnimateExitAsync runs on close ──

  [AvaloniaFact]
  public async Task DialogClose_RunsExitChoreography()
  {
    var (_, panel, _, sp) = CreateShell();
    var dialogs = sp.GetRequiredService<IRouter>();
    var session = new ExitingDialog();

    Task showTask = dialogs.ShowAsync(session);
    await WaitUntilAsync(() => panel.DerivedHosts().Count() == 1);
    var host = SingleCapsule(panel);
    var dimmer = Assert.IsType<DimmerLayout>(Assert.Single(host.Children));
    var view = Assert.IsType<ExitingDialogView>(Assert.Single(((BodyPanel)dimmer.GetBodyPanel()).Children));
    Assert.Equal(1, view.EnterCount);

    // Closing the session runs the overlay's exit choreography on the view.
    session.Close();
    await showTask;
    Assert.Equal(1, view.ExitCount);
    Assert.Empty(panel.DerivedHosts());
  }

  // ── the arriving view is laid out when the director runs (ISceneTransition's contract) ──

  [AvaloniaFact]
  public async Task DialogEnter_DirectorSeesLaidOutArrivingView()
  {
    var (_, panel, _, sp) = CreateWindowedShell();
    var dialogs = sp.GetRequiredService<IRouter>();
    var session = new LaidOutDialog();

    Task showTask = dialogs.ShowAsync(session);

    LaidOutDialogView? view = null;
    await WaitUntilAsync(() =>
    {
      RoutingView? host = panel.DerivedHosts().FirstOrDefault();
      DimmerLayout? dimmer = host?.Children.OfType<DimmerLayout>().FirstOrDefault();
      view = dimmer is null
        ? null
        : ((BodyPanel)dimmer.GetBodyPanel()).Children
            .OfType<LaidOutDialogView>().FirstOrDefault();
      return view is { EnterRan: true };
    });

    Assert.NotNull(view);

    // ISceneTransition: "an entering view is mounted, laid out and invisible
    // when the method runs".  A derived (dialog) router mounts its host in
    // the same turn the director runs, so this is where the guarantee bites.
    Assert.True(view.AttachedAtEnter, "the arriving view was not attached when the director ran");
    Assert.True(view.WidthAtEnter > 0, "the arriving view had no measured width when the director ran");
    Assert.True(view.HeightAtEnter > 0, "the arriving view had no measured height when the director ran");

    session.Close();
    await showTask.WaitAsync(TimeSpan.FromSeconds(5));
  }

  // ── the dimmer blocks the layer below while the enter animation runs ──

  [AvaloniaFact]
  public async Task DialogEnter_DimmerBlocksLowerLayerWhileAnimating()
  {
    var (_, panel, _, sp) = CreateShell();
    var dialogs = sp.GetRequiredService<IRouter>();
    var session = new BlockingDialog();

    Task showTask = dialogs.ShowAsync(session);

    DimmerLayout? dimmer = null;
    BlockingDialogView? view = null;
    await WaitUntilAsync(() =>
    {
      RoutingView? host = panel.DerivedHosts().FirstOrDefault();
      dimmer = host?.Children.OfType<DimmerLayout>().FirstOrDefault();
      view = dimmer is null
        ? null
        : ((BodyPanel)dimmer.GetBodyPanel()).Children.OfType<BlockingDialogView>().FirstOrDefault();
      return view is { Entered: true };
    });

    Assert.NotNull(dimmer);
    Assert.NotNull(view);

    // The inner director is parked: the enter animation is in flight.
    Assert.True(dimmer.IsHitTestVisible, "the dimmer leaked clicks while the dialog animated in");

    view.Release();
    await WaitUntilAsync(() => view.IsHitTestVisible);

    session.Close();
    await showTask.WaitAsync(TimeSpan.FromSeconds(5));
    Assert.Empty(panel.DerivedHosts());
  }

  // ── a close lower in the stack — the lower session settles itself and closes only its own overlay; the top is untouched ──

  [AvaloniaFact]
  public async Task SessionOwnClose_ClosesItsOwnCapsule_UnderStacking()
  {
    var (_, panel, _, sp) = CreateShell();
    var dialogs = sp.GetRequiredService<IRouter>();
    var bottom = new ProbeDialog();
    var top = new ProbeDialog();

    Task showBottom = dialogs.ShowAsync(bottom);
    Task showTop = dialogs.ShowAsync(top);
    await WaitUntilAsync(() => panel.DerivedHosts().Count() == 2);

    // The bottom session settles itself while the top dialog is open —
    // only the bottom's overlay closes.
    bottom.Close("ok");
    await showBottom.WaitAsync(TimeSpan.FromSeconds(5));

    Assert.True(showBottom.IsCompleted);
    Assert.False(showTop.IsCompleted);
    Assert.Single(panel.DerivedHosts());
    Assert.Same(top, SingleCapsule(panel).Location!.Instance);

    top.Close();
    await showTop.WaitAsync(TimeSpan.FromSeconds(5));
    Assert.Empty(panel.DerivedHosts());
  }

  // ── an external Back is denied by a mandatory dimmer — the dimmer arbitrates and reports ──

  [AvaloniaFact]
  public async Task ExternalBack_VetoedByMandatoryDimmer_ShakesTheDimmer()
  {
    var (_, panel, shell, sp) = CreateShell();
    var dialogs = sp.GetRequiredService<IRouter>();
    var session = new ProbeDialog();

    Task showTask = dialogs.ShowAsync(session, new DefaultDimmerModel { LightDismiss = false });
    await WaitUntilAsync(() => panel.DerivedHosts().Count() == 1);
    var host = SingleCapsule(panel);
    var dimmer = Assert.IsType<DimmerLayout>(Assert.Single(host.Children));
    var body = (BodyPanel)dimmer.GetBodyPanel();
    Assert.Null(body.RenderTransform);

    // An external back is vetoed by the mandatory dimmer — the dimmer is
    // the dismissal arbiter (DimmerView as an IIntentHandler in the
    // overlay's node pipeline) and shakes the hosted body.
    Assert.Equal(IntentResult.Vetoed, await shell.DispatchIntent(null, new BackIntent()));
    Assert.IsType<TranslateTransform>(body.RenderTransform);
    Assert.False(showTask.IsCompleted);

    session.Close();
    await showTask.WaitAsync(TimeSpan.FromSeconds(5));
  }
}
