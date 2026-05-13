using Everlong.Nester.Intent;
using Everlong.Nester.Routing;
using Xunit;

namespace Everlong.Nester.Tests.Routing;

/// <summary>
///   Pins the jump verb's visit-log semantics — a jump records a fresh
///   visit to an existing engagement (a push of the held chain, no
///   resolution), never a cursor move; a current engagement records
///   nothing; a dead engagement settles false and rebuilds by description.
/// </summary>
public class JumpTests
{
  private static Request Page(Type page)
    => new(page, null, [Target.Of(page)]);

  private static async Task RouteEach(TestRouter router, params Type[] pages)
  {
    foreach (Type page in pages)
      await router.RouteAsync(Page(page));
  }

  private static List<Type> Leaves(IReadOnlyList<ILocation> entries)
    => entries.Select(site => site.Type).ToList();

  [Fact]
  public async Task JumpToAHistoricalEngagement_RecordsAFreshVisit_ReusingTheHeldInstances()
  {
    (FakeShell _, TestRouter router) = RouterTestHost.Create();
    await RouteEach(router, typeof(PageAlpha), typeof(PageBeta));
    ILocation alphaSite = router.Stack.BackStack()[0];
    TestContent alpha = Assert.IsAssignableFrom<TestContent>(alphaSite.Instance);

    Assert.True(await router.JumpAsync(alphaSite));

    // A fresh visit, not a cursor move — the stack grew by one entry.
    Assert.Equal(3, router.Stack.Count);
    // The held engagement landed again — never a re-materialization.
    Assert.Same(alpha, router.Stack.Location!.Instance);
    Assert.Equal(typeof(PageAlpha), router.Stack.Location!.Type);
    // The visit log reads backwards from the jump — Beta was the previous stop.
    Assert.Equal([typeof(PageBeta), typeof(PageAlpha)], Leaves(router.Stack.BackStack()));
    // The re-visited engagement replays its arrival with the jump direction.
    Assert.Equal(2, alpha.ArrivalCount);
    Assert.Equal(RoutingDirection.Jump, alpha.DirectionOnArrived);
  }

  [Fact]
  public async Task JumpToTheCurrentEngagement_RecordsNothing()
  {
    (FakeShell _, TestRouter router) = RouterTestHost.Create();
    await RouteEach(router, typeof(PageAlpha));
    ILocation site = router.Stack.Location!;
    TestContent alpha = Assert.IsAssignableFrom<TestContent>(site.Instance);

    Assert.True(await router.JumpAsync(site));

    Assert.Equal(1, router.Stack.Count);
    Assert.Equal(1, alpha.ArrivalCount);
    Assert.Equal(RoutingDirection.Route, alpha.DirectionOnArrived);
  }

  [Fact]
  public async Task JumpToADeadEngagement_ReturnsFalse_AndThePlaceRebuildsByDescription()
  {
    (FakeShell _, TestRouter router) = RouterTestHost.Create();
    await RouteEach(router, typeof(PageAlpha), typeof(PageBeta));
    ILocation alphaSite = router.Stack.BackStack()[0];
    TestContent alpha = Assert.IsAssignableFrom<TestContent>(alphaSite.Instance);

    // The trim retires Alpha's entry — the drain releases the engagement.
    Assert.True(router.Model.TrimBackward());
    Assert.True(alpha.Released);

    // No live entry presents the site — the identity is gone.
    Assert.False(await router.JumpAsync(alphaSite));
    Assert.Equal(typeof(PageBeta), router.Stack.Location!.Type);

    // The graceful degrade — the place rebuilds from the site's description.
    await router.RouteAsync(alphaSite.ToLocator());
    Assert.Equal(2, router.Stack.Count);
    Assert.Equal(typeof(PageAlpha), router.Stack.Location!.Type);
    Assert.NotSame(alpha, router.Stack.Location!.Instance);
  }

  [Fact]
  public async Task JumpToAForwardEngagement_ClearsTheForwardTrail_LikeAnyPush()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await RouteEach(router, typeof(PageAlpha), typeof(PageBeta), typeof(PageGamma));
    Assert.Equal(IntentResult.Handled, await shell.DispatchIntent(null, new BackIntent()));
    Assert.Equal(IntentResult.Handled, await shell.DispatchIntent(null, new BackIntent()));
    Assert.Equal(typeof(PageAlpha), router.Stack.Location!.Type);
    ILocation betaSite = router.Stack.ForwardStack()[0];
    TestContent beta = Assert.IsAssignableFrom<TestContent>(betaSite.Instance);
    int arrivalsBefore = beta.ArrivalCount;

    Assert.True(await router.JumpAsync(betaSite));

    // The push erased the stale forward trail — Gamma went with it.
    Assert.Equal(2, router.Stack.Count);
    Assert.Same(beta, router.Stack.Location!.Instance);
    Assert.Equal(typeof(PageBeta), router.Stack.Location!.Type);
    Assert.Equal([typeof(PageAlpha)], Leaves(router.Stack.BackStack()));
    Assert.Empty(router.Stack.ForwardStack());
    Assert.Equal(arrivalsBefore + 1, beta.ArrivalCount);
    Assert.Equal(RoutingDirection.Jump, beta.DirectionOnArrived);
  }

  [Fact]
  public async Task JumpOnAnEmptyStack_ReturnsFalse()
  {
    (FakeShell _, TestRouter router) = RouterTestHost.Create();

    Assert.False(await router.JumpAsync(new TestLocation(typeof(PageAlpha), Args.Empty, new PageAlpha())));
    Assert.Equal(0, router.Stack.Count);
  }

  /// <summary>A page whose membership edge throws on demand — the jump's notify-phase fault shape.</summary>
  private sealed class ThrowingRoutedPage : IRoutable
  {
    internal bool Throw { get; set; }

    public void OnRoutedTo(IRoutingContext context)
    {
      if (Throw)
        throw new InvalidOperationException("routed-to boom");
    }

    public void OnRoutedFrom(IRoutingContext context)
    {
    }
  }

  [Fact]
  public async Task Jump_FaultingNotifier_FaultsTheCallerInsteadOfWaitingForever()
  {
    (FakeShell _, TestRouter router) = RouterTestHost.Create();
    var page = new ThrowingRoutedPage();
    await router.RouteAsync(new Request(typeof(ThrowingRoutedPage), null,
      [Target.Of(typeof(ThrowingRoutedPage), instance: page)]));
    await RouteEach(router, typeof(PageBeta));   // the page's entry is now the back trail

    ILocation site = router.Stack.BackStack()[0];
    page.Throw = true;

    // The visit's notify phase throws before it lands — the jump's relay must
    // fault with it, not stay unsettled.
    var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => router.JumpAsync(site));
    Assert.Contains("routed-to boom", failure.Message);
  }
}
