using Everlong.Nester.Routing;
using Xunit;

using Everlong.Nester.Intent;
namespace Everlong.Nester.Tests.Routing;

/// <summary>
///   The direction axis — the operation the router is scheduling — is
///   observable through the lifecycle context, independent of the scene
///   classification the platform consumes.
/// </summary>
public class DirectionTests
{
  private static Request Req(params Type[] chain)
    => new(chain[^1], null, chain.Select(t => Target.Of(t)).ToList());

  [Fact]
  public async Task Route_Arrival_ReportsRoute()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();

    await router.RouteAsync(Req(typeof(LayoutAlpha), typeof(PageAlpha)));
    var page = (TestContent)router.Model!.CurrentChain![1].Instance;

    Assert.Equal(RoutingDirection.Route, page.DirectionOnArrived);
  }

  [Fact]
  public async Task Back_RestoredArrival_ReportsBack()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(Req(typeof(LayoutAlpha), typeof(PageAlpha)));
    var pageA = (TestContent)router.Model!.CurrentChain![1].Instance;
    await router.RouteAsync(Req(typeof(LayoutAlpha), typeof(PageBeta)));

    Assert.Equal(IntentResult.Handled, await shell.DispatchIntent(null, new BackIntent()));

    Assert.Same(pageA, router.Model.CurrentChain![1].Instance);
    Assert.Equal(RoutingDirection.Back, pageA.DirectionOnArrived);
  }

  [Fact]
  public async Task Back_LeavingPage_DepartsWithBack()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(Req(typeof(LayoutAlpha), typeof(PageAlpha)));
    await router.RouteAsync(Req(typeof(LayoutAlpha), typeof(PageBeta)));
    var pageB = (TestContent)router.Model!.CurrentChain![1].Instance;

    Assert.Equal(IntentResult.Handled, await shell.DispatchIntent(null, new BackIntent()));

    Assert.Equal(RoutingDirection.Back, pageB.DirectionOnDeparting);
  }

  [Fact]
  public async Task Forward_RestoredArrival_ReportsForward()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(Req(typeof(LayoutAlpha), typeof(PageAlpha)));
    await router.RouteAsync(Req(typeof(LayoutAlpha), typeof(PageBeta)));
    var pageB = (TestContent)router.Model!.CurrentChain![1].Instance;
    await shell.DispatchIntent(null, new BackIntent());

    Assert.Equal(IntentResult.Handled, await shell.DispatchIntent(null, new ForwardIntent()));

    Assert.Same(pageB, router.Model.CurrentChain![1].Instance);
    Assert.Equal(RoutingDirection.Forward, pageB.DirectionOnArrived);
  }

  [Fact]
  public async Task Refresh_Replay_ReportsRefresh()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(Req(typeof(LayoutAlpha), typeof(PageAlpha)));
    var page = (TestContent)router.Model!.CurrentChain![1].Instance;

    Assert.Equal(IntentResult.Handled, await shell.DispatchIntent(null, new RefreshIntent()));

    Assert.Equal(RoutingDirection.Refresh, page.DirectionOnArrived);
  }

  [Fact]
  public async Task Present_Arrival_ReportsRoute()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var session = new TestContent();
    var request = new Request(typeof(TestContent), null,
      [Target.Of(typeof(TestContent), instance: session)]);

    var r = router.Derive();
    await r.RouteAsync(request);

    Assert.Equal(RoutingDirection.Route, session.DirectionOnArrived);
  }

  [Fact]
  public async Task Present_Close_RunsNoDepartureCeremony_ReleasesTheContent()
  {
    // A overlay close is a terminal ceremony, not a transition: it fires
    // no departure hooks (the chain dies as a whole — Direction would be
    // meaningless on the way out) and the release drain closes the
    // members out.
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var session = new TestContent();
    var request = new Request(typeof(TestContent), null,
      [Target.Of(typeof(TestContent), instance: session)]);
    var r = router.Derive();
    await r.RouteAsync(request);

    IntentResult handled = await shell.DispatchIntent(null, new BackIntent());

    Assert.Equal(IntentResult.Handled, handled);
    Assert.False(session.Departing);
    Assert.False(session.Departed);
    Assert.Null(session.DirectionOnDeparting);
    Assert.True(session.Released);
  }

  [Fact]
  public async Task Route_SameChain_IsANoOp()
  {
    // The same-route request is classified Route (its cause), yet today it
    // lands nothing — equal route = no-op: no push, no ceremony, no
    // delivery.  Deliberate refresh replays go through the RefreshIntent.
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(Req(typeof(LayoutAlpha), typeof(PageAlpha)));
    var page = (TestContent)router.Model!.CurrentChain![1].Instance;

    await router.RouteAsync(Req(typeof(LayoutAlpha), typeof(PageAlpha)));

    Assert.Equal(1, page.ArrivalCount);
    Assert.Equal(1, router.Stack.Count);
    Assert.Equal(RoutingDirection.Route, page.DirectionOnArrived);
  }

  [Fact]
  public async Task Ground_Route_IsNotElevated()
  {
    // The orthogonal trait — the base router's navigations are never
    // elevated, whatever the direction.
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(Req(typeof(LayoutAlpha), typeof(PageAlpha)));
    var page = (TestContent)router.Model!.CurrentChain![1].Instance;

    Assert.False(page.IsElevatedOnArrived);
  }

  [Fact]
  public async Task Present_Arrival_IsElevated()
  {
    // A derived router's maiden route opens an overlay — the arriving
    // participant observes the elevation on its whole navigation context.
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var session = new TestContent();
    var request = new Request(typeof(TestContent), null,
      [Target.Of(typeof(TestContent), instance: session)]);
    var r = router.Derive();
    await r.RouteAsync(request);

    Assert.Equal(RoutingDirection.Route, session.DirectionOnArrived);
    Assert.True(session.IsElevatedOnArrived);
  }
}
