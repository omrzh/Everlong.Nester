using Everlong.Nester.Routing;
using Xunit;

using Everlong.Nester.Intent;
namespace Everlong.Nester.Tests.Routing;

public class StackingTests
{
  [Fact]
  public async Task PresentTwo_BackGoesToTopmost_ThenToLower()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var lower = new TestContent();
    var top = new TestContent();

    IRouter presentLower = router.Derive();
    await presentLower.RouteAsync(new Request(typeof(TestContent), null,
      [Target.Of(typeof(TestContent), instance: lower)]));
    IRouter presentTop = router.Derive();
    await presentTop.RouteAsync(new Request(typeof(TestContent), null,
      [Target.Of(typeof(TestContent), instance: top)]));

    // Back goes to the topmost model — it closes with its own result.
    IntentResult handled = await shell.DispatchIntent(null, new BackIntent("top"));
    Assert.Equal(IntentResult.Handled, handled);
    Assert.Equal("top", await presentTop.Completion!);
    // A derived router's close is a terminal ceremony — released, never departed.
    Assert.False(top.Departed);
    Assert.True(top.Released);
    // The top model is gone — the lower one is active again.
    Assert.False(lower.Departed);

    IntentResult handledLower = await shell.DispatchIntent(null, new BackIntent("lower"));
    Assert.Equal(IntentResult.Handled, handledLower);
    Assert.Equal("lower", await presentLower.Completion!);
    Assert.False(lower.Departed);
    Assert.True(lower.Released);
  }

  [Fact]
  public async Task PresentTwo_VetoKeepsTop_WhileLowerWaits()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var top = new TestContent { Mandatory = true };
    var lower = new TestContent();

    IRouter presentLower = router.Derive();
    await presentLower.RouteAsync(new Request(typeof(TestContent), null,
      [Target.Of(typeof(TestContent), instance: lower)]));
    IRouter presentTop = router.Derive();
    await presentTop.RouteAsync(new Request(typeof(TestContent), null,
      [Target.Of(typeof(TestContent), instance: top)]));

    // An external close is vetoed by the top mandatory model — nothing closes.
    await shell.DispatchIntent(null, new BackIntent(null));
    Assert.False(presentTop.Completion!.Result.IsCompleted);
    Assert.False(presentLower.Completion!.Result.IsCompleted);

    // The top model's own close lets the top layer end; the lower one stays.
    await shell.DispatchIntent(top, new BackIntent("go"));
    Assert.Equal("go", await presentTop.Completion!);
    Assert.False(presentLower.Completion!.Result.IsCompleted);
  }
}
