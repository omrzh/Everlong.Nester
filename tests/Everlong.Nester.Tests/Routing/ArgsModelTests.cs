using Everlong.Nester.Routing;
using Xunit;

namespace Everlong.Nester.Tests.Routing;

/// <summary>
///   The args model (`docs/design/absorption.md`): the engagement is the
///   node's identity (read back from `EngagedArgs` after delivery), the
///   request is the negotiation input; the absorption scan (in-place
///   revision — no push, no membership edges, a delivery and arrival replay
///   on the revised node), the equal-route no-op, the sharp-edge rule (a
///   shared node is never revised), the delivery-throw abort, and the rigid
///   node's ignored arguments.
/// </summary>
public class ArgsModelTests
{
  /// <summary>A controllable adaptive participant — records deliveries, its engagement, arrivals and membership edges.</summary>
  private sealed class ParamPage : IAdaptiveParameterized, IArrived, IRoutable
  {
    private IArgs? _engaged;

    internal bool Adapt { get; set; } = true;

    internal bool ThrowOnDeliver { get; set; }

    internal bool Correct { get; set; }

    internal IArgs? Delivered { get; private set; }

    internal int DeliverCount { get; private set; }

    internal int Arrived { get; private set; }

    internal int RoutedTo { get; private set; }

    internal int RoutedFrom { get; private set; }

    internal IReadOnlyList<ILocation>? LastArrivings { get; private set; }

    internal IReadOnlyList<ILocation>? LastDepartings { get; private set; }

    public IArgs? EngagedArgs => _engaged;

    bool IAdaptiveParameterized.IsAdaptable(IArgs? requested) => Adapt;

    void IParameterized.DeliverArgs(IArgs? args)
    {
      if (ThrowOnDeliver)
        throw new InvalidOperationException("boom");
      DeliverCount++;
      Delivered = args;
      _engaged = Correct ? new TestArgs("corrected") : args;
    }

    Task IArrived.OnArrivedAsync(IRoutingContext context)
    {
      Arrived++;
      LastArrivings = context.Arrivings;
      LastDepartings = context.Departings;
      return Task.CompletedTask;
    }

    void IRoutable.OnRoutedTo(IRoutingContext context) => RoutedTo++;

    void IRoutable.OnRoutedFrom(IRoutingContext context) => RoutedFrom++;
  }

  /// <summary>A rigid participant — no arguments channel.</summary>
  private sealed class RigidPage
  {
  }

  /// <summary>A parameterized-but-not-adaptive participant — receives arguments at materialization only.</summary>
  private sealed class NonAdaptivePage : IParameterized
  {
    internal IArgs? Delivered { get; private set; }

    public IArgs? EngagedArgs => Delivered;

    public void DeliverArgs(IArgs? args) => Delivered = args;
  }

  private static Request RouteOf(Type type, IArgs? args = null, object? instance = null)
    => new(type, args, [Target.Of(type, args, instance)]);

  private static Request ChainOf(Type layout, Type content, IArgs? args = null, object? contentInstance = null)
    => new(content, args,
           [Target.Of(layout), Target.Of(content, args, contentInstance)]);

  // ── absorption — in-place revision ────────────────────────────────────────────

  [Fact]
  public async Task Absorb_RevisesArgsInPlace_NoPushNoEdgesArrivalReplays()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var page = new ParamPage();
    await router.RouteAsync(RouteOf(typeof(ParamPage), new TestArgs("p1"), page));

    // A type-only re-route with revised arguments — the scan matches the
    // current node and absorbs in place.
    await router.RouteAsync(RouteOf(typeof(ParamPage), new TestArgs("p2")));

    Assert.Same(page, router.Model!.CurrentChain![0].Instance);
    Assert.Equal(1, router.Stack.Count);   // no phantom entry
    Assert.Equal(2, page.DeliverCount);    // the instantiation, then the re-engagement
    Assert.Equal(new TestArgs("p2"), page.Delivered);
    Assert.Equal(new TestArgs("p2"), page.EngagedArgs);
    Assert.Equal(new TestArgs("p2"), router.Model.CurrentChain[0].Args);
    Assert.Equal(1, page.RoutedTo);        // never left — no membership edge
    Assert.Equal(2, page.Arrived);         // the arrival ceremony replayed
    Assert.Equal(0, page.RoutedFrom);
  }

  [Fact]
  public async Task Absorb_PassesThroughAnEqualPrefixLayout()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var page = new ParamPage();
    await router.RouteAsync(ChainOf(typeof(LayoutAlpha), typeof(ParamPage), new TestArgs("p1"), page));

    await router.RouteAsync(ChainOf(typeof(LayoutAlpha), typeof(ParamPage), new TestArgs("p2")));

    Assert.Same(page, router.Model!.CurrentChain![1].Instance);
    Assert.Equal(new TestArgs("p2"), page.EngagedArgs);
    Assert.Equal(1, router.Stack.Count);
    Assert.Equal(1, page.RoutedTo);        // the layout and the page never left
    Assert.Equal(2, page.Arrived);         // only the revised page replays its arrival
  }

  [Fact]
  public async Task Absorb_Correction_RecordsTheEngagement()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var page = new ParamPage();
    await router.RouteAsync(RouteOf(typeof(ParamPage), new TestArgs("p1"), page));

    page.Correct = true;
    await router.RouteAsync(RouteOf(typeof(ParamPage), new TestArgs("p2")));

    // The node's identity is the engagement it serves, not the request.
    Assert.Equal(new TestArgs("corrected"), router.Model!.CurrentChain![0].Args);
    Assert.Equal(new TestArgs("corrected"), page.EngagedArgs);
    Assert.Equal(1, router.Stack.Count);

    // A request equal to the served value is a fast hit — no re-negotiation.
    await router.RouteAsync(RouteOf(typeof(ParamPage), new TestArgs("corrected")));
    Assert.Equal(1, router.Stack.Count);
    Assert.Equal(2, page.Arrived);
  }

  [Fact]
  public async Task Materialization_RecordsTheEngagement()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var page = new ParamPage { Correct = true };

    // An unspecified request — the node answers with its own default, and
    // that default becomes the node's identity.
    await router.RouteAsync(RouteOf(typeof(ParamPage), null, page));

    Assert.Equal(new TestArgs("corrected"), router.Model!.CurrentChain![0].Args);
    Assert.Equal(new TestArgs("corrected"), page.EngagedArgs);
  }

  [Fact]
  public async Task UnspecifiedRequest_ReusesTheSameNode()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var page = new ParamPage();

    await router.RouteAsync(ChainOf(typeof(LayoutAlpha), typeof(ParamPage), contentInstance: page));
    int deliveries = page.DeliverCount;

    await router.RouteAsync(RouteOf(typeof(PageBeta)));                           // away
    await router.RouteAsync(ChainOf(typeof(LayoutAlpha), typeof(ParamPage)));     // back, same request

    // The request is the identity — the return engagement reuses the node.
    Assert.Same(page, router.Model!.CurrentChain![^1].Instance);
    Assert.Equal(deliveries, page.DeliverCount);
  }

  [Fact]
  public async Task Absorb_ExposesTheMovingSidesOnTheLanding()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var page = new ParamPage();
    await router.RouteAsync(RouteOf(typeof(ParamPage), new TestArgs("p1"), page));

    // A materialization arrives — nothing departs.
    Assert.Same(page, Assert.Single(page.LastArrivings!).Instance);
    Assert.Empty(page.LastDepartings!);

    await router.RouteAsync(RouteOf(typeof(ParamPage), new TestArgs("p2")));

    // A revision is a re-engagement: the same node on both sides.
    Assert.Same(page, Assert.Single(page.LastArrivings!).Instance);
    Assert.Same(page, Assert.Single(page.LastDepartings!).Instance);
  }

  [Fact]
  public async Task Refresh_DeliversNothing()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var page = new ParamPage();
    await router.RouteAsync(RouteOf(typeof(ParamPage), new TestArgs("p1"), page));
    int deliveries = page.DeliverCount;

    await shell.DispatchIntent(null, new RefreshIntent());
    await router.WaitIdleAsync();

    Assert.Equal(deliveries, page.DeliverCount);     // a refresh replays the arrival, not the delivery
    Assert.Equal(2, page.Arrived);
  }

  // ── decline and the sharp-edge rule ───────────────────────────────────────────

  [Fact]
  public async Task Decline_FallsBackToAFreshNode_LeavingTheOldEntryIntact()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var page = new ParamPage();
    await router.RouteAsync(RouteOf(typeof(ParamPage), new TestArgs("p1"), page));

    page.Adapt = false;
    await router.RouteAsync(RouteOf(typeof(ParamPage), new TestArgs("p2")));

    // Declined — a fresh node materializes; the back entry keeps its own
    // instance and identity.
    Assert.NotSame(page, router.Model!.CurrentChain![0].Instance);
    Assert.Equal(2, router.Stack.Count);
    Assert.Equal(1, page.RoutedTo);
    Assert.Equal(new TestArgs("p1"), page.Delivered);

    await shell.DispatchIntent(null, new BackIntent());
    await router.WaitIdleAsync();
    Assert.Same(page, router.Model.CurrentChain![0].Instance);
    Assert.Equal(new TestArgs("p1"), router.Model.CurrentChain[0].Args);
  }

  [Fact]
  public async Task SharedNode_IsNeverRevised_StructuralFreshWins()
  {
    // The same node is held by the current entry and the forward trail — a
    // revision would rewrite the forward entry's identity (the sharp-edge
    // rule), so the request lands a fresh node instead.
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var page = new ParamPage();
    await router.RouteAsync(RouteOf(typeof(ParamPage), new TestArgs("p1"), page));
    await router.RouteAsync(ChainOf(typeof(LayoutAlpha), typeof(ParamPage), new TestArgs("p1"), page));
    await shell.DispatchIntent(null, new BackIntent());
    await router.WaitIdleAsync();
    Assert.Same(page, router.Model!.CurrentChain![0].Instance);
    Assert.True(router.Model.CanGoForward);

    await router.RouteAsync(RouteOf(typeof(ParamPage), new TestArgs("p2")));

    Assert.NotSame(page, router.Model.CurrentChain![0].Instance);
    Assert.Equal(new TestArgs("p1"), page.Delivered);
    Assert.Equal(new TestArgs("p2"), router.Model.CurrentChain[0].Args);

    // The back trail that shares the node still shows its own engagement.
    await shell.DispatchIntent(null, new BackIntent());
    await router.WaitIdleAsync();
    Assert.Same(page, router.Model.CurrentChain![0].Instance);
    Assert.Equal(new TestArgs("p1"), router.Model.CurrentChain[0].Args);
  }

  // ── abort on a throwing delivery ──────────────────────────────────────────────

  [Fact]
  public async Task FreshDeliveryThrow_FaultsTheCaller_NothingLands()
  {
    var reported = new List<Exception>();
    (FakeShell shell, TestRouter router) = RouterTestHost.Create(reporter: reported.Add);
    var page = new ParamPage { ThrowOnDeliver = true };

    var failure = await Assert.ThrowsAsync<InvalidOperationException>(
      () => router.RouteAsync(RouteOf(typeof(ParamPage), new TestArgs("p1"), page)));

    Assert.Contains("boom", failure.Message);
    Assert.Empty(reported);             // the fault reached the caller, not the channel
    Assert.Equal(0, router.Stack.Count);
  }

  [Fact]
  public async Task AbsorbDeliveryThrow_AbortsTheTransaction()
  {
    var reported = new List<Exception>();
    (FakeShell shell, TestRouter router) = RouterTestHost.Create(reporter: reported.Add);
    var page = new ParamPage();
    await router.RouteAsync(RouteOf(typeof(ParamPage), new TestArgs("p1"), page));

    page.ThrowOnDeliver = true;

    // The delivery runs before the commit — a throwing revision faults the
    // caller and nothing lands.
    var failure = await Assert.ThrowsAsync<InvalidOperationException>(
      () => router.RouteAsync(RouteOf(typeof(ParamPage), new TestArgs("p2"))));

    Assert.Contains("boom", failure.Message);
    Assert.Empty(reported);                     // the fault rides the caller, not the channel
    Assert.Same(page, router.Model!.CurrentChain![0].Instance);
    Assert.Equal(new TestArgs("p1"), router.Model.CurrentChain[0].Args);
    Assert.Equal(new TestArgs("p1"), page.EngagedArgs);
    Assert.Equal(1, router.Stack.Count);
  }

  // ── traverse delivers nothing; rigid nodes ignore arguments ──────────────────

  [Fact]
  public async Task Traverse_ReplaysMembership_WithoutRedelivering()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var page = new ParamPage();
    await router.RouteAsync(RouteOf(typeof(ParamPage), new TestArgs("p1"), page));
    await router.RouteAsync(RouteOf(typeof(PageBeta)));   // a different page on top

    await shell.DispatchIntent(null, new BackIntent());
    await router.WaitIdleAsync();

    // Re-entry fired the edge and replayed the arrival — the arguments
    // were not re-delivered (the engagement never changed).
    Assert.Same(page, router.Model!.CurrentChain![0].Instance);
    Assert.Equal(2, page.RoutedTo);
    Assert.Equal(2, page.Arrived);
    Assert.Equal(new TestArgs("p1"), page.Delivered);
  }

  [Fact]
  public async Task RigidNode_IgnoresRequestedArguments()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(RouteOf(typeof(RigidPage), new TestArgs("x")));
    object first = router.Model!.CurrentChain![0].Instance;

    // The rigid node serves no arguments, and a request with different
    // (meaningless) arguments still lands on the same node.
    Assert.Null(router.Model.CurrentChain[0].Args);
    await router.RouteAsync(RouteOf(typeof(RigidPage), new TestArgs("y")));
    Assert.Same(first, router.Model.CurrentChain![0].Instance);
    Assert.Null(router.Model.CurrentChain[0].Args);
    Assert.Equal(1, router.Stack.Count);
  }

  [Fact]
  public async Task NonAdaptive_RevisedArgs_LandsAFreshNode()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var page = new NonAdaptivePage();
    await router.RouteAsync(RouteOf(typeof(NonAdaptivePage), new TestArgs("p1"), page));

    // A revised-args re-route never absorbs — the page is parameterized but
    // not adaptive, so a fresh node materializes.
    await router.RouteAsync(RouteOf(typeof(NonAdaptivePage), new TestArgs("p2")));

    Assert.NotSame(page, router.Model!.CurrentChain![0].Instance);
    Assert.Equal(2, router.Stack.Count);
    Assert.Equal(new TestArgs("p1"), page.Delivered);   // the original never re-negotiated
    Assert.Equal(new TestArgs("p2"), router.Model.CurrentChain[0].Args);
  }

  [Fact]
  public async Task Absorb_Correction_PublishesTheEngagement()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var page = new ParamPage();
    await router.RouteAsync(RouteOf(typeof(ParamPage), new TestArgs("p1"), page));

    page.Correct = true;
    await router.RouteAsync(RouteOf(typeof(ParamPage), new TestArgs("p2")));

    // The location's identity is the engagement the node serves.
    IReadOnlyList<ILocation> location = router.View.Location!.Trail;
    Assert.Same(page, location[0].Instance);
    Assert.Equal(new TestArgs("corrected"), location[0].Args);
    Assert.Equal(new TestArgs("corrected"), page.EngagedArgs);
    Assert.Equal(1, router.Stack.Count);
  }
}
