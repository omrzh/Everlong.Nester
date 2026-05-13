using Everlong.Nester.Routing;
using Xunit;

using Everlong.Nester.Intent;
using Everlong.Nester.Layer;
using Everlong.Nester.Shell;
using Microsoft.Extensions.DependencyInjection;
namespace Everlong.Nester.Tests.Routing;

/// <summary>
///   Locks the router's navigation-state surface: the surface is always
///   readable (empty location, no traversal reach before the first route),
///   tracks push/back/forward at the committed truth, raises
///   <c>PropertyChanged</c> with current values, each derived router carries its
///   own surface, and a closed derived router no longer routes or re-completes.
/// </summary>
public class RouterStackTests
{
  private static Request ChainOf(Type page)
    => new(page, null, [Target.Of(page)]);

  [Fact]
  public void FreshRouter_SurfaceIsEmptyAndNeverNull()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();

    Assert.NotNull(router.Stack);
    Assert.Null(router.Stack.Location);
    Assert.False(router.Stack.CanGoBack);
    Assert.False(router.Stack.CanGoForward);
    Assert.Equal(0, router.Stack.Count);
  }

  [Fact]
  public async Task Route_PushesStack_SurfaceTracks()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();

    await router.RouteAsync(ChainOf(typeof(PageAlpha)));
    Assert.Single(router.Stack.Location!.Trail);
    Assert.Equal(typeof(PageAlpha), router.Stack.Location!.Type);
    Assert.False(router.Stack.CanGoBack);

    await router.RouteAsync(ChainOf(typeof(PageBeta)));
    Assert.True(router.Stack.CanGoBack);
    Assert.Equal(typeof(PageBeta), router.Stack.Location!.Type);
  }

  [Fact]
  public async Task BackAndForward_SurfaceTracksTraversal()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(ChainOf(typeof(PageAlpha)));
    await router.RouteAsync(ChainOf(typeof(PageBeta)));

    Assert.Equal(IntentResult.Handled, await shell.DispatchIntent(null, new BackIntent()));
    Assert.Equal(typeof(PageAlpha), router.Stack.Location!.Type);
    Assert.False(router.Stack.CanGoBack);
    Assert.True(router.Stack.CanGoForward);

    Assert.Equal(IntentResult.Handled, await shell.DispatchIntent(null, new ForwardIntent()));
    Assert.Equal(typeof(PageBeta), router.Stack.Location!.Type);
    Assert.True(router.Stack.CanGoBack);
    Assert.False(router.Stack.CanGoForward);
  }

  [Fact]
  public async Task TypedSurface_TracksEntriesAndTraversal()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();

    await router.RouteAsync(ChainOf(typeof(PageAlpha)));
    Assert.Equal(1, router.Stack.Count);
    Assert.Equal(typeof(PageAlpha), router.Stack.Location!.Type);
    Assert.Empty(router.Stack.BackStack());
    Assert.Empty(router.Stack.ForwardStack());
    Assert.Null(router.Stack.PeekPrevious());
    Assert.Null(router.Stack.PeekNext());

    await router.RouteAsync(ChainOf(typeof(PageBeta)));
    Assert.Equal(2, router.Stack.Count);
    Assert.Equal(typeof(PageBeta), router.Stack.Location!.Type);
    Assert.Equal(typeof(PageAlpha), router.Stack.PeekPrevious()!.Type);
    Assert.Empty(router.Stack.ForwardStack());
    Assert.Single(router.Stack.BackStack());
    Assert.Equal(typeof(PageAlpha), router.Stack.Snapshot()[0].Type);
    Assert.Equal(typeof(PageBeta), router.Stack.Snapshot()[1].Type);

    Assert.Equal(IntentResult.Handled, await shell.DispatchIntent(null, new BackIntent()));
    Assert.Equal(typeof(PageAlpha), router.Stack.Location!.Type);
    Assert.Equal(typeof(PageBeta), router.Stack.PeekNext()!.Type);
    Assert.Single(router.Stack.ForwardStack());
    Assert.Equal(typeof(PageBeta), router.Stack.ForwardStack()[0].Type);
  }

  [Fact]
  public async Task Landing_RaisesPropertyChanged()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var changes = new List<string?>();
    router.Stack.PropertyChanged += (_, e) => changes.Add(e.PropertyName);

    await router.RouteAsync(ChainOf(typeof(PageAlpha)));

    Assert.Contains(nameof(IRouterStack.Location), changes);
    Assert.Contains(nameof(IRouterStack.CanGoBack), changes);
    Assert.Contains(nameof(IRouterStack.CanGoForward), changes);
    Assert.Contains(nameof(IRouterStack.Count), changes);
  }

  [Fact]
  public async Task Landing_LocationIsCurrentAtNotification()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var captured = new List<ILocation?>();
    router.Stack.PropertyChanged += (_, e) =>
    {
      if (e.PropertyName == nameof(IRouterStack.Location))
        captured.Add(router.Stack.Location);
    };

    await router.RouteAsync(ChainOf(typeof(PageAlpha)));
    await router.RouteAsync(ChainOf(typeof(PageBeta)));

    // The notification carries the committed truth — never the previous landing.
    Assert.Equal(2, captured.Count);
    Assert.Equal(typeof(PageAlpha), captured[0]?.Type);
    Assert.Equal(typeof(PageBeta), captured[1]?.Type);
  }

  [Fact]
  public async Task Location_InstanceIsStableBetweenLandings()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();

    ILocation? first = router.Stack.Location;
    Assert.Same(first, router.Stack.Location);

    await router.RouteAsync(ChainOf(typeof(PageAlpha)));

    ILocation? second = router.Stack.Location;
    Assert.NotSame(first, second);
    Assert.Same(second, router.Stack.Location);
  }

  [Fact]
  public async Task Trim_RaisesSurfaceNotification()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(ChainOf(typeof(PageAlpha)));
    await router.RouteAsync(ChainOf(typeof(PageBeta)));
    var changes = new List<string?>();
    router.Stack.PropertyChanged += (_, e) => changes.Add(e.PropertyName);

    router.Model.TrimBackward();

    // The trim flips the reach and shrinks the count — the property
    // contract notifies (a binding history button must not stay enabled).
    Assert.Contains(nameof(IRouterStack.CanGoBack), changes);
    Assert.Contains(nameof(IRouterStack.Count), changes);
    Assert.Equal(1, router.Stack.Count);
    Assert.False(router.Stack.CanGoBack);
  }

  [Fact]
  public async Task TrimBackward_NoPrevious_IsNoOp()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(ChainOf(typeof(PageAlpha)));

    router.Model.TrimBackward();

    Assert.False(router.Stack.CanGoBack);
    Assert.Equal(1, router.Stack.Count);
    Assert.Equal(typeof(PageAlpha), router.Stack.Location!.Type);
  }

  [Fact]
  public async Task TrimForward_RemovesNextEntry()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(ChainOf(typeof(PageAlpha)));
    await router.RouteAsync(ChainOf(typeof(PageBeta)));
    Assert.Equal(IntentResult.Handled, await shell.DispatchIntent(null, new BackIntent()));

    router.Model.TrimForward();

    Assert.False(router.Stack.CanGoForward);
    Assert.Equal(1, router.Stack.Count);
    Assert.Equal(typeof(PageAlpha), router.Stack.Location!.Type);
  }

  [Fact]
  public async Task TrimForward_NoNext_IsNoOp()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(ChainOf(typeof(PageAlpha)));

    router.Model.TrimForward();

    Assert.False(router.Stack.CanGoForward);
    Assert.Equal(1, router.Stack.Count);
  }

  [Fact]
  public async Task Derived_SurfaceTracksItsOwnStack()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    IRouter result = router.Derive();

    await result.RouteAsync(ChainOf(typeof(PageAlpha)));
    Assert.Single(result.Stack.Location!.Trail);
    Assert.False(result.Stack.CanGoBack);

    await result.RouteAsync(ChainOf(typeof(PageBeta)));
    Assert.True(result.Stack.CanGoBack);
    Assert.Equal(typeof(PageBeta), result.Stack.Location!.Type);
  }

  [Fact]
  public async Task ClosedFloat_NoLongerRoutes_ReentrantCompleteIsNoOp()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    IRouter result = router.Derive();
    await result.RouteAsync(ChainOf(typeof(PageAlpha)));

    result.Completion!.Complete("first");
    Assert.Equal("first", await result.Completion!.Result);

    // A re-entrant completion is a no-op — the first entry wins.
    result.Completion!.Complete("second");
    Assert.Equal("first", await result.Completion!.Result);

    // A closed derived router no longer routes.
    int count = result.Stack.Count;
    await result.RouteAsync(ChainOf(typeof(PageBeta)));
    Assert.Equal(count, result.Stack.Count);
    Assert.Equal(typeof(PageAlpha), result.Stack.Location!.Type);
  }

  [Fact]
  public async Task Close_ReleasesLeaseBeforeResultSettles()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    // Capture the derived routers the scopes resolve — the lease correlation
    // is asserted against the created router, not a facade cast.
    var derivedRouters = new List<TestRouter>();
    shell.RouterFactory = sp =>
    {
      var created = new TestRouter(sp.GetRequiredService<IShell>(), sp);
      derivedRouters.Add(created);
      return created;
    };

    IRouter first = router.Derive();
    await first.RouteAsync(ChainOf(typeof(PageAlpha)));

    int firstZ = LeaseOf(shell, derivedRouters[0]).Z;
    Assert.Contains(firstZ, shell.Leases.Select(l => l.Z));

    first.Completion!.Complete("done");
    Assert.Equal("done", await first.Completion!.Result);

    // The lease is gone by the time the result settles — the next derived router
    // reuses the freed height instead of stacking above a stale lease.
    Assert.DoesNotContain(firstZ, shell.Leases.Select(l => l.Z));
    IRouter second = router.Derive();
    Assert.Equal(firstZ, LeaseOf(shell, derivedRouters[1]).Z);
  }

  private static ILayerLease LeaseOf(FakeShell shell, TestRouter derivedRouter)
    => shell.Leases.Single(lease => ReferenceEquals(lease.Content, derivedRouter.View));
}
