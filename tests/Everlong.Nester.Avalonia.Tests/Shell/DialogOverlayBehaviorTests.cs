using Everlong.Nester.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Everlong.Nester.Controls;
using Everlong.Nester.Dialog;
using Everlong.Nester.Intent;
using Everlong.Nester.Presentation;
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

  private sealed class ViewLocator : IDataTemplate
  {
    public bool Match(object? data) => data is ProbeDialog or ExitingDialog or ConfirmDialogSession;

    public Control? Build(object? data) => data switch
    {
      ExitingDialog => new ExitingDialogView(),
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

  private static NavigationHost SingleCapsule(StagePanel panel)
    => Assert.Single(panel.DerivedHosts());

  private static ContentLayer? CapsuleLayerOf(StagePanel panel, object session)
    => panel.DerivedLayers()
            .FirstOrDefault(layer =>
              ReferenceEquals(((NavigationHost)layer.Content!).Location!.Instance, session));

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
    var view = Assert.IsType<ExitingDialogView>(Assert.Single(((LayoutBody)dimmer.GetLayoutBody()).Children));
    Assert.Equal(1, view.EnterCount);

    // Closing the session runs the overlay's exit choreography on the view.
    session.Close();
    await showTask;
    Assert.Equal(1, view.ExitCount);
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
    var body = (LayoutBody)dimmer.GetLayoutBody();
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
