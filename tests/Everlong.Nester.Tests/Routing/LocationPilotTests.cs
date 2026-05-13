using Everlong.Nester.Routing;
using Xunit;

namespace Everlong.Nester.Tests.Routing;

/// <summary>
///   Input-side pilot — the Location reading protocol (config T): a
///   navigable input describes its spec before the truth phases; the
///   request window (<see cref="RouterBase.HandleRouteRequest" />) is the last
///   chance to rewrite the location; the kernel never reads a location's
///   internal complexity, only its description.
/// </summary>
public class LocationPilotTests
{
  /// <summary>A fuzzy location — a page plus its declared shells, described at intake.</summary>
  private sealed record FuzzyLocation(Type Content, params Type[] Shells) : ILocator
  {
    public IReadOnlyList<ITarget> Path { get; } =
    [
      .. Shells.Select(shell => global::Everlong.Nester.Routing.Target.Of(shell)),
      global::Everlong.Nester.Routing.Target.Of(Content),
    ];
  }

  /// <summary>A location that rides a pre-constructed instance — "show this building".</summary>
  private sealed record RidingLocation(object Instance) : ILocator
  {
    public IReadOnlyList<ITarget> Path { get; } =
      [global::Everlong.Nester.Routing.Target.Of(Instance.GetType(), instance: Instance)];
  }

  /// <summary>A router whose request window replaces the location — the expansion must run on the replacement.</summary>
  private sealed class ReplacingRouter(FakeShell shell) : TestRouter(shell)
  {
    protected override void HandleRouteRequest(TransactionContext ctx, TransactNext next)
    {
      if (ctx.Direction == RoutingDirection.Route && ctx.Locator is FuzzyLocation { Content: var c } && c == typeof(PageAlpha))
        ctx.UpdateLocator(new FuzzyLocation(typeof(PageBeta)));
      next(ctx);
    }
  }

  [Fact]
  public async Task FuzzyLocation_ExpandsIntoDeclaredChain_BeforeTheWalk()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();

    await router.RouteAsync(new FuzzyLocation(typeof(PageAlpha), typeof(LayoutAlpha)));

    Assert.Equal(2, router.Model!.CurrentChain!.Length);
    Assert.Equal(typeof(LayoutAlpha), router.Model.CurrentChain[0].Type);
    Assert.Equal(typeof(PageAlpha), router.Model.CurrentChain[1].Type);
  }

  [Fact]
  public async Task RidingLocation_PresentsThePrebuiltInstance()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var building = new PageAlpha();

    await router.RouteAsync(new RidingLocation(building));

    Assert.Same(building, router.View.Location!.Instance);
    Assert.Same(building, router.Model!.CurrentChain![0].Instance);
  }

  [Fact]
  public async Task RequestWindow_ReplacementWins_TheExpansionRunsOnIt()
  {
    var shell = new FakeShell();
    var router = new ReplacingRouter(shell);

    // The caller names Alpha; the window rewrites to Beta before expansion.
    await router.RouteAsync(new FuzzyLocation(typeof(PageAlpha)));

    Assert.Single(router.Model!.CurrentChain!);
    Assert.Equal(typeof(PageBeta), router.Model.CurrentChain[0].Type);
  }

  [Fact]
  public void RequestWindow_ClosesAtTheExpansion_NotAtTheCommit()
  {
    // The expansion is the boundary: it resolves the descriptor into a plan
    // that stands, so the request may no longer be replaced — well before
    // the transaction commits.
    var ctx = new TransactionContext(RoutingDirection.Route, new Request(typeof(PageAlpha), null));
    ctx.ExpandedLocator = ctx.Locator!.Path;

    Assert.False(ctx.IsCommitted);
    Assert.Throws<InvalidOperationException>(() => ctx.UpdateLocator(new Request(typeof(PageBeta), null)));
  }

  [Fact]
  public async Task Route_RemainsADirectLocation_IdentityDescription()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();

    // A generated-style route is itself a location — its description is its own chain.
    await router.RouteAsync(new Locator([Target.Of(typeof(LayoutAlpha)), Target.Of(typeof(PageAlpha))]));

    Assert.Equal(2, router.Model!.CurrentChain!.Length);
    Assert.Equal(typeof(PageAlpha), router.Model.CurrentChain[1].Type);
  }
}
