using Avalonia.Controls;
using Everlong.Nester.Presentation;
using Xunit;

namespace Everlong.Nester.Tests.Shell;

/// <summary>
///   The director working surface — chain accessors, reveal-before, the
///   next-director lookup, and the scoped hand-off.
/// </summary>
public sealed class TransitionContextTests
{
  private sealed class FakeDirector : ContentControl, ISceneTransition
  {
    public Task AnimateEnterAsync(TransitionContext context, CancellationToken token) => Task.CompletedTask;

    public Task AnimateExitAsync(TransitionContext context, CancellationToken token) => Task.CompletedTask;
  }

  private sealed class PlainView : ContentControl;

  private static TransitionContext Ctx(TransitionKind change, Control[] arriving, Control[] departing)
    => new(new FlyingCanvas(), change)
    {
      ArrivingChain = arriving,
      DepartingChain = departing,
    };

  [Fact]
  public void Heads_DeriveFromChainOutermost()
  {
    var frame = new PlainView();
    var leaf = new PlainView();
    var ctx = Ctx(TransitionKind.Enter, [frame, leaf], []);

    Assert.Same(frame, ctx.ArrivingHead);
    Assert.Null(ctx.DepartingHead);
    Assert.Same(leaf, ctx.ArrivingChain[^1]);
  }

  [Fact]
  public void RevealBefore_RevealsViewsAboveTheDirector()
  {
    var frame = new PlainView();
    var leaf = new PlainView();
    var ctx = Ctx(TransitionKind.Enter, [frame, leaf], []);
    frame.Opacity = 0;
    frame.IsHitTestVisible = false;

    ctx.RevealBefore(leaf);

    Assert.Equal(1, frame.Opacity);
    Assert.True(frame.IsHitTestVisible);
  }

  [Fact]
  public void RevealBefore_HeadDirector_IsNoop()
  {
    var frame = new PlainView();
    var leaf = new PlainView();
    var ctx = Ctx(TransitionKind.Enter, [frame, leaf], []);
    frame.Opacity = 0;

    ctx.RevealBefore(frame);

    Assert.Equal(0, frame.Opacity);
  }

  [Fact]
  public void RevealBefore_DirectorNotInChain_IsNoop()
  {
    var frame = new PlainView();
    var stranger = new PlainView();
    var ctx = Ctx(TransitionKind.Enter, [frame], []);
    frame.Opacity = 0;

    ctx.RevealBefore(stranger);

    Assert.Equal(0, frame.Opacity);
  }

  [Fact]
  public void NextDirectorAfter_ReturnsFirstQualifierBelow()
  {
    var head = new PlainView();
    var inner = new FakeDirector();
    var leaf = new FakeDirector();
    var ctx = Ctx(TransitionKind.Enter, [head, inner, leaf], []);

    Assert.Same(inner, ctx.NextDirectorAfter(head));
    Assert.Same(leaf, ctx.NextDirectorAfter(inner));
    Assert.Null(ctx.NextDirectorAfter(leaf));
  }

  [Fact]
  public void NextDirectorAfter_OnExitWalksTheDepartingChain()
  {
    var head = new PlainView();
    var leaf = new FakeDirector();
    var ctx = Ctx(TransitionKind.Exit, [], [head, leaf]);

    Assert.Same(leaf, ctx.NextDirectorAfter(head));
  }

  [Fact]
  public void ScopedFrom_Enter_DropsThroughDirectorAndKeepsOwnLevelOnOtherSide()
  {
    var main = new PlainView();
    var chrome = new FakeDirector();
    var leaf = new PlainView();
    var oldChrome = new PlainView();
    var oldLeaf = new PlainView();
    var ctx = Ctx(TransitionKind.Enter, [main, chrome, leaf], [oldChrome, oldLeaf]);

    TransitionContext scoped = ctx.ScopedFrom(chrome);

    Assert.Equal(new Control[] { leaf }, scoped.ArrivingChain);
    Assert.Equal(new Control[] { oldLeaf }, scoped.DepartingChain);
  }

  [Fact]
  public void ScopedFrom_Exit_DropsThroughDirectorAndKeepsOwnLevelOnOtherSide()
  {
    var oldMain = new PlainView();
    var oldChrome = new FakeDirector();
    var oldLeaf = new PlainView();
    var main = new PlainView();
    var leaf = new PlainView();
    var ctx = Ctx(TransitionKind.Exit, [main, leaf], [oldMain, oldChrome, oldLeaf]);

    TransitionContext scoped = ctx.ScopedFrom(oldChrome);

    Assert.Equal(new Control[] { oldLeaf }, scoped.DepartingChain);
    Assert.Equal(new Control[] { leaf }, scoped.ArrivingChain);
  }

  [Fact]
  public void ScopedFrom_DirectorNotInChain_ReturnsSame()
  {
    var leaf = new PlainView();
    var stranger = new PlainView();
    var ctx = Ctx(TransitionKind.Enter, [leaf], []);

    Assert.Same(ctx, ctx.ScopedFrom(stranger));
  }
}
