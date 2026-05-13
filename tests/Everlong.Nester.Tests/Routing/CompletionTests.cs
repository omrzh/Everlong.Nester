using Everlong.Nester.Routing;
using Xunit;

namespace Everlong.Nester.Tests.Routing;

public class CompletionTests
{
  [Fact]
  public async Task GroundRequest_HasNoCompletion()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(new Request(typeof(PageAlpha), null));

    var content = (TestContent)router.Model!.Current!;
    Assert.Null(content.SeenCompletion);
  }

  [Fact]
  public async Task PresentedRequest_CompletesWithResult()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var content = new TestContent();
    IRouter present = router.Derive();
    await present.RouteAsync(new Request(typeof(TestContent), null,
      [Target.Of(typeof(TestContent), instance: content)]));

    await WaitArrivedAsync(content);
    Assert.NotNull(content.SeenCompletion);   // a presented request has a waiting party

    content.SeenCompletion!.Complete("done");
    Assert.Equal("done", await present.Completion!);
    // A close is a terminal ceremony — no departure edge or pair fires;
    // the release drain closes the content out.
    Assert.False(content.Departed);
    Assert.True(content.Released);
  }

  [Fact]
  public async Task Completion_IsIdempotent()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var content = new TestContent();
    IRouter present = router.Derive();
    await present.RouteAsync(new Request(typeof(TestContent), null,
      [Target.Of(typeof(TestContent), instance: content)]));

    await WaitArrivedAsync(content);
    content.SeenCompletion!.Complete("first");
    content.SeenCompletion!.Complete("second");

    Assert.Equal("first", await present.Completion!);
  }

  [Fact]
  public async Task EmptyClose_LandsNothing_AndStillSettlesTheResult()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    IRouter present = router.Derive();
    int navigated = 0;
    present.Stack.PropertyChanged += (_, e) =>
    {
      if (e.PropertyName == nameof(IRouterStack.Location))
        navigated++;
    };

    // Close before anything presented — no chain to depart.
    ((TestRouter)present).Completion!.Complete("x");

    Assert.Equal("x", await present.Completion!);
    Assert.Equal(0, navigated);
  }

  private static async Task WaitArrivedAsync(TestContent content)
  {
    for (int i = 0; i < 200 && !content.Arrived; i++)
      await Task.Yield();
  }
}
