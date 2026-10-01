using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Everlong.Nester.Presentation;
using Xunit;

namespace Everlong.Nester.Tests.Shell;

/// <summary>
///   The delegation protocol a pass-through director uses — the lever it moves
///   down the arriving path on entry and the pure hand-off it makes on the way
///   out.
/// </summary>
[Collection("RealShell")]
public sealed class TransitionDelegationTests
{
  private sealed class DirectorView : ContentControl, ISceneTransition
  {
    internal List<TransitionContext> Enters { get; } = [];

    internal List<TransitionContext> Exits { get; } = [];

    public Task AnimateEnterAsync(TransitionContext context, CancellationToken token)
    {
      Enters.Add(context);
      return Task.CompletedTask;
    }

    public Task AnimateExitAsync(TransitionContext context, CancellationToken token)
    {
      Exits.Add(context);
      return Task.CompletedTask;
    }
  }

  /// <summary>A body-holding director whose delegation is the shipped pass-through.</summary>
  private sealed class FrameView : ContentControl, IBodyHolder, ISceneTransition
  {
    private readonly BodyPanel _body = new();

    internal FrameView() => Content = _body;

    public IBodyPanel GetBodyPanel() => _body;

    public Task AnimateEnterAsync(TransitionContext context, CancellationToken token)
      => this.PassThroughAsync(context, token);

    public Task AnimateExitAsync(TransitionContext context, CancellationToken token)
      => this.PassExitAsync(context, token);
  }

  private sealed class BodyNode(Control view) : IViewLocation<Control>
  {
    public Control? View { get; set; } = view;
  }

  private static IViewLocation<Control> Attach(FrameView frame, Control child)
  {
    IBodyPanel<Control> body = frame.GetBodyPanel();
    var node = new BodyNode(child);
    body.Add(node);
    body.SetActiveChild(node);
    return node;
  }

  [AvaloniaFact]
  public async Task PassThrough_DipsTheArrivingChild_RevealsTheFrame_AndHandsTheChildTheContext()
  {
    var frame = new FrameView();
    var child = new DirectorView();
    Attach(frame, child);

    var departing = new DirectorView();
    var context = new TransitionContext(null, TransitionKind.Enter)
    {
      Arriving = frame,
      Departing = departing,
    };

    await frame.AnimateEnterAsync(context, CancellationToken.None);

    Assert.Equal(1, frame.Opacity);
    Assert.True(frame.IsHitTestVisible);
    Assert.Equal(0, child.Opacity);
    Assert.False(child.IsHitTestVisible);
    Assert.Equal(1, departing.Opacity);            // the departing head stays visible

    TransitionContext handed = Assert.Single(child.Enters);
    Assert.Same(child, handed.Arriving);
    Assert.Same(departing, handed.Departing);
  }

  [AvaloniaFact]
  public async Task PassThrough_WithoutAnInnerDirector_RevealsTheFrameAndDipsTheChild()
  {
    var frame = new FrameView();
    var plain = new ContentControl();
    Attach(frame, plain);

    var context = new TransitionContext(null, TransitionKind.Enter) { Arriving = frame };
    await frame.AnimateEnterAsync(context, CancellationToken.None);

    Assert.Equal(1, frame.Opacity);
    Assert.Equal(0, plain.Opacity);                // the framework's final restore settles it
  }

  [AvaloniaFact]
  public async Task PassThrough_OnRefresh_LeavesTheReengagedChildVisible()
  {
    var frame = new FrameView();
    var child = new DirectorView();
    Attach(frame, child);

    var context = new TransitionContext(null, TransitionKind.Refresh) { Arriving = frame };
    await frame.AnimateEnterAsync(context, CancellationToken.None);

    Assert.Equal(1, child.Opacity);
    Assert.True(child.IsHitTestVisible);
    Assert.Same(child, Assert.Single(child.Enters).Arriving);
  }

  [AvaloniaFact]
  public async Task PassExit_WalksToTheDepartingChild_WithoutMovingVisibility()
  {
    var frame = new FrameView();
    IBodyPanel<Control> body = frame.GetBodyPanel();

    var departingChild = new DirectorView();
    var departingNode = new BodyNode(departingChild);
    body.Add(departingNode);

    var arrivingChild = new DirectorView();
    var arrivingNode = new BodyNode(arrivingChild);
    body.Add(arrivingNode);
    body.SetActiveChild(arrivingNode);             // the arriving side took the body

    arrivingChild.Opacity = 0;                     // the framework dipped the arriving head
    var context = new TransitionContext(null, TransitionKind.Exit)
    {
      Arriving = arrivingChild,
      Departing = frame,
    };

    await frame.AnimateExitAsync(context, CancellationToken.None);

    Assert.Equal(1, frame.Opacity);
    Assert.Equal(1, departingChild.Opacity);
    Assert.Equal(0, arrivingChild.Opacity);        // delegation moved nothing

    TransitionContext handed = Assert.Single(departingChild.Exits);
    Assert.Same(departingChild, handed.Departing);
    Assert.Same(arrivingChild, handed.Arriving);
  }

  [AvaloniaFact]
  public async Task PassExit_WithoutAnArrivingSibling_WalksToTheActiveChild()
  {
    var frame = new FrameView();
    var child = new DirectorView();
    Attach(frame, child);

    var context = new TransitionContext(null, TransitionKind.Dismiss) { Departing = frame };
    await frame.AnimateExitAsync(context, CancellationToken.None);

    TransitionContext handed = Assert.Single(child.Exits);
    Assert.Same(child, handed.Departing);
    Assert.Null(handed.Arriving);
    Assert.Equal(1, frame.Opacity);
  }
}
