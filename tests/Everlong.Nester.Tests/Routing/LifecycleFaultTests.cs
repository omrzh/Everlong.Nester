using Everlong.Nester.Intent;
using Everlong.Nester.Routing;
using Xunit;

namespace Everlong.Nester.Tests.Routing;

/// <summary>
///   Fault-containment contract: user-code exceptions at the routing
///   orchestration boundary are isolated — a failing participant never
///   aborts the ceremony, the pipeline, or the lease return, and every
///   failure is reported through the router's error channel (single
///   failures reported as-is, multiple failures aggregated).
/// </summary>
public class LifecycleFaultTests
{
  /// <summary>A participant with a configurable throwing hook per contract.</summary>
  private sealed class FaultyParticipant : IParameterized, IArriving, IArrived,
                                           IDeparting, IDeparted, IReleasable, IIntentHandler
  {
    internal Exception? ThrowOnDeparting;
    internal Exception? ThrowOnDeparted;
    internal Exception? ThrowOnRelease = null;
    internal Exception? ThrowOnArriving;
    internal Exception? ThrowOnArrived;
    internal Exception? ThrowOnIntent;

    internal bool DepartingCalled { get; private set; }
    internal bool DepartedCalled { get; private set; }
    internal bool ReleasedCalled { get; private set; }
    internal bool ArrivingCalled { get; private set; }
    internal bool ArrivedCalled { get; private set; }

    public IArgs? EngagedArgs => null;

    public void DeliverArgs(IArgs? args)
    {
    }

    public Task OnArrivingAsync(IRoutingContext context)
    {
      ArrivingCalled = true;
      if (ThrowOnArriving is { } e)
        return Task.FromException(e);
      return Task.CompletedTask;
    }

    public Task OnArrivedAsync(IRoutingContext context)
    {
      ArrivedCalled = true;
      if (ThrowOnArrived is { } e)
        return Task.FromException(e);
      return Task.CompletedTask;
    }

    public void OnDeparting(IRoutingContext context)
    {
      DepartingCalled = true;
      if (ThrowOnDeparting is { } e)
        throw e;
    }

    public void OnDeparted(IRoutingContext context)
    {
      DepartedCalled = true;
      if (ThrowOnDeparted is { } e)
        throw e;
    }

    public void Release()
    {
      ReleasedCalled = true;
      if (ThrowOnRelease is { } e)
        throw e;
    }

    public ValueTask HandleAsync(IntentContext context, IntentDelegate next)
    {
      if (ThrowOnIntent is { } e)
        return ValueTask.FromException(e);
      return next(context);
    }
  }

  private static Request Chain(params object[] instances)
    => new(typeof(FaultyParticipant), null,
      instances.Select(i => Target.Of(i.GetType(), instance: i)).ToArray());

  // ── Sync hooks: isolated + reported ──────────────────

  [Fact]
  public async Task DepartingHookFailure_IsIsolatedAndReportedAsIs()
  {
    var reported = new List<Exception>();
    (_, TestRouter router) = RouterTestHost.Create(reporter: reported.Add);
    var a = new FaultyParticipant();
    var boom = new InvalidOperationException("boom");
    var b = new FaultyParticipant { ThrowOnDeparting = boom };

    await router.RouteAsync(Chain(a, b));
    await router.RouteAsync(Chain(new FaultyParticipant(), new FaultyParticipant()));   // turnover — a, b depart

    Assert.True(a.DepartingCalled);   // the healthy sibling still runs
    Assert.True(b.DepartingCalled);   // the throwing participant ran up to its throw
    var e = Assert.Single(reported);
    Assert.Same(boom, e);             // a single failure is reported as-is, not wrapped
  }

  [Fact]
  public async Task MultipleHookFailures_AreAggregatedIntoOneReport()
  {
    var reported = new List<Exception>();
    (_, TestRouter router) = RouterTestHost.Create(reporter: reported.Add);
    var boom1 = new InvalidOperationException("b1");
    var boom2 = new InvalidOperationException("b2");
    var a = new FaultyParticipant { ThrowOnDeparted = boom1 };
    var b = new FaultyParticipant { ThrowOnDeparted = boom2 };

    await router.RouteAsync(Chain(a, b));
    await router.RouteAsync(Chain(new FaultyParticipant(), new FaultyParticipant()));

    var e = Assert.Single(reported);              // aggregated: exactly one report
    var aggregated = Assert.IsType<ConvergenceException>(e);
    Assert.Equal(2, aggregated.InnerExceptions.Count);   // both inner failures preserved
    Assert.Contains(boom1, aggregated.InnerExceptions);
    Assert.Contains(boom2, aggregated.InnerExceptions);
  }

  // ── Close ceremony: the lease return must never be lost ──

  [Fact]
  public async Task CloseAsync_HookFailure_StillSettlesAndReturnsLease()
  {
    var reported = new List<Exception>();
    (FakeShell shell, TestRouter router) = RouterTestHost.Create(reporter: reported.Add);
    var boom = new InvalidOperationException("boom");
    var failing = new FaultyParticipant { ThrowOnRelease = boom };

    IRouter present = router.Derive();
    await present.RouteAsync(Chain(new FaultyParticipant(), failing));
    Assert.Equal(2, shell.Leases.Count());   // ground + derived router live

    present.Completion!.Complete("ok");   // close — the release drain throws

    Assert.Equal("ok", await present.Completion!);   // the result settles — never a hang
    Assert.Single(shell.Leases);          // the derived router's layer returns — never a leak
    Assert.Single(reported);
  }

  [Fact]
  public async Task Fault_HookFailure_StillSettlesAndReturnsLease()
  {
    var reported = new List<Exception>();
    (FakeShell shell, TestRouter router) = RouterTestHost.Create(reporter: reported.Add);
    var failing = new FaultyParticipant { ThrowOnRelease = new InvalidOperationException("boom") };
    IRouter present = router.Derive();
    await present.RouteAsync(Chain(new FaultyParticipant(), failing));
    // Derive hands back the concrete derived router itself —
    // no facade to unwrap for the internal fault path.
    var inner = (TestRouter)present;
    var pipelineBoom = new InvalidOperationException("pipeline boom");

    inner.FaultRouterCompletion(pipelineBoom);   // the fault path — its release sequence hits the throwing hook

    var surfaced = await Assert.ThrowsAsync<InvalidOperationException>(() => present.Completion!.Result);
    Assert.Same(pipelineBoom, surfaced);      // the fault channel settles with the pipeline failure
    Assert.Single(shell.Leases);   // the layer returns — never a leak
    Assert.Single(reported);
  }

  // ── After-step: one broken arrival must not skip the rest ──

  [Fact]
  public async Task ArrivedHookFailure_DoesNotSkipSiblingsOrDepartureRitual()
  {
    var reported = new List<Exception>();
    (_, TestRouter router) = RouterTestHost.Create(reporter: reported.Add);
    var oldA = new FaultyParticipant();
    var oldB = new FaultyParticipant();
    await router.RouteAsync(Chain(oldA, oldB));

    var a2 = new FaultyParticipant { ThrowOnArrived = new InvalidOperationException("boom") };
    var b2 = new FaultyParticipant();
    await router.RouteAsync(Chain(a2, b2));   // turnover — old chain departs, new chain arrives

    Assert.True(b2.ArrivedCalled);            // the sibling after the throwing one still arrives
    Assert.True(oldA.DepartedCalled);         // the departure sequence still runs
    Assert.True(oldB.DepartedCalled);
    Assert.False(oldA.ReleasedCalled);        // the old chain stays in the back trail —
    Assert.False(oldB.ReleasedCalled);        // released only when evicted
    Assert.Single(reported);
  }

  // ── Arriving: a broken pre-reveal hook still reveals ──

  [Fact]
  public async Task ArrivingHookFailure_StillRevealsAndArrives()
  {
    var reported = new List<Exception>();
    (_, TestRouter router) = RouterTestHost.Create(reporter: reported.Add);
    var a = new FaultyParticipant { ThrowOnArriving = new InvalidOperationException("boom") };
    var b = new FaultyParticipant();

    await router.RouteAsync(Chain(a, b));   // the pipeline must not abort

    Assert.True(a.ArrivedCalled);           // the request crossed the reveal into arrived
    Assert.True(b.ArrivedCalled);
    Assert.Single(reported);
  }

  // ── Intent chain: a broken guard faults the dispatch to its caller ──

  [Fact]
  public async Task GuardIntentFailure_FaultsTheDispatchCaller()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(Chain(new PageAlpha()));
    object alpha = router.Model!.Current!;
    var guard = new FaultyParticipant { ThrowOnIntent = new InvalidOperationException("boom") };
    await router.RouteAsync(Chain(guard));

    // The node pipeline carries no per-node guard — a page that throws
    // during the consultation escalates out of the dispatch; the caller
    // (the fake shell has no stage catch) receives the exception.
    await Assert.ThrowsAsync<InvalidOperationException>(
      () => shell.DispatchIntent(null, new BackIntent()).AsTask());
    Assert.Same(guard, router.Model.Current);   // the traversal did not run
  }

  // ── VM/View isolation: a failing VM must not skip the view's callback ──

  private sealed class VmViewPair : IDeparting, IArriving, IArrived
  {
    internal bool Called { get; private set; }

    public void OnDeparting(IRoutingContext context) => Called = true;

    public Task OnArrivingAsync(IRoutingContext context) { Called = true; return Task.CompletedTask; }

    public Task OnArrivedAsync(IRoutingContext context) { Called = true; return Task.CompletedTask; }
  }

  [Fact]
  public void VmFailure_DoesNotSkipView_Departing()
  {
    var view = new VmViewPair();
    var vm = new FaultyParticipant { ThrowOnDeparting = new InvalidOperationException("vm boom") };
    var node = new TestLocation(typeof(FaultyParticipant), new Args(), vm) { View = view };
    var failures = new List<Exception>();
    var ctx = new FaultFakeContext();

    LifecycleHelper.RunDeparting(node, ctx, failures);

    Assert.True(vm.DepartingCalled);   // the VM ran (up to its throw)
    Assert.True(view.Called);          // the view still runs
    Assert.Single(failures);
  }

  [Fact]
  public async Task VmFailure_DoesNotSkipView_Arriving()
  {
    var view = new VmViewPair();
    var vm = new FaultyParticipant { ThrowOnArriving = new InvalidOperationException("vm boom") };
    var node = new TestLocation(typeof(FaultyParticipant), new Args(), vm) { View = view };
    var failures = new List<Exception>();
    var ctx = new FaultFakeContext();

    await LifecycleHelper.RunArrivingAsync(node, ctx, failures);

    Assert.True(vm.ArrivingCalled);
    Assert.True(view.Called);
    Assert.Single(failures);
  }

  [Fact]
  public void ViewFailure_Departing_DoesNotSkipVm()
  {
    var view = new FaultyParticipant { ThrowOnDeparting = new InvalidOperationException("view boom") };
    var vm = new VmViewPair();
    var node = new TestLocation(typeof(VmViewPair), new Args(), vm) { View = view };
    var failures = new List<Exception>();
    var ctx = new FaultFakeContext();

    LifecycleHelper.RunDeparting(node, ctx, failures);

    Assert.True(view.DepartingCalled);   // view ran (up to its throw)
    Assert.True(vm.Called);             // VM still runs — it's dispatched first
    Assert.Single(failures);
  }

  /// <summary>A routing context for unit tests outside the full router pipeline.</summary>
  private sealed class FaultFakeContext : IRoutingContext
  {
    public ILocation? Arrival => null;
    public RoutingDirection Direction => RoutingDirection.Route;
    public bool IsElevated => false;
    public CancellationToken Lifetime => CancellationToken.None;
    public IReadOnlyList<ILocation> Arrivings => [];
    public IReadOnlyList<ILocation> Departings => [];
    public IFeatureCollection Features => new FeatureCollection();
    public ILocation? Departure => null;
  }

  /// <summary>A hook that honors the supersession token — the documented pattern.</summary>
  private sealed class TokenHonoringParticipant : IArriving
  {
    public async Task OnArrivingAsync(IRoutingContext context)
      => await Task.Delay(Timeout.Infinite, context.Lifetime);
  }

  [Fact]
  public async Task ForeignCancellation_IsReportedLikeAnyFailure()
  {
    var reported = new List<Exception>();
    (_, TestRouter router) = RouterTestHost.Create(reporter: reported.Add);
    // A cancellation the router did not cause (an HttpClient timeout is shaped
    // exactly like this) must surface — not vanish into the supersession path.
    var oce = new TaskCanceledException("foreign token");
    var a = new FaultyParticipant { ThrowOnArrived = oce };

    await router.RouteAsync(Chain(a));
    await router.WaitIdleAsync();

    var e = Assert.Single(reported);
    Assert.Same(oce, e);
  }

  [Fact]
  public async Task SupersessionCancellation_InHooks_IsSilent()
  {
    var reported = new List<Exception>();
    (_, TestRouter router) = RouterTestHost.Create(reporter: reported.Add);
    var a = new TokenHonoringParticipant();

    await router.RouteAsync(new Request(typeof(TokenHonoringParticipant), null,
      [Target.Of(typeof(TokenHonoringParticipant), instance: a)]));
    // Supersede — the stalled arrival gate observes the token and unwinds.
    await router.RouteAsync(Chain(new FaultyParticipant()));
    await router.WaitIdleAsync();

    Assert.Empty(reported);   // our own cancellation is never a fault
  }
}
