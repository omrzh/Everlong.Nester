using Everlong.Nester.Shell;
using Everlong.Nester.Routing;
using Xunit;

namespace Everlong.Nester.Tests.Routing;

/// <summary>
///   The async ceremony pipeline's user stage — the observation hook over a
///   committed truth: it sees the ceremony's decision, may wrap the whole
///   convergence, and the context's completion channel settles with the run.
/// </summary>
public class CeremonyHookTests
{
  private sealed class ObservingRouter : TestRouter
  {
    internal List<string> Log { get; } = [];

    internal ObservingRouter(IShell shell) : base(shell) { }

    protected override async Task OnConvergenceAsync(IConvergenceContext ctx, ConvergeNext next)
    {
      Log.Add($"observe:{ctx.Direction}:{ctx.Chain.Count}");
      await next(ctx);
      Log.Add("done");
    }
  }

  private sealed class SkippingRouter : TestRouter
  {
    internal SkippingRouter(IShell shell) : base(shell) { }

    protected override Task OnConvergenceAsync(IConvergenceContext ctx, ConvergeNext next) => Task.CompletedTask;
  }

  private sealed class ThrowingRouter : TestRouter
  {
    internal ThrowingRouter(IShell shell) : base(shell) { }

    protected override Task OnConvergenceAsync(IConvergenceContext ctx, ConvergeNext next)
      => throw new InvalidOperationException("observer boom");
  }

  private sealed class CapturingRouter : TestRouter
  {
    internal IConvergenceContext? Observed { get; private set; }

    internal CapturingRouter(IShell shell) : base(shell) { }

    protected override Task OnConvergenceAsync(IConvergenceContext ctx, ConvergeNext next)
    {
      Observed = ctx;
      return next(ctx);
    }
  }

  /// <summary>Records every ceremony the observation hook sees, oldest first.</summary>
  private sealed class RecordingRouter : TestRouter
  {
    internal List<IConvergenceContext> Ceremonies { get; } = [];

    internal RecordingRouter(IShell shell) : base(shell) { }

    protected override Task OnConvergenceAsync(IConvergenceContext ctx, ConvergeNext next)
    {
      Ceremonies.Add(ctx);
      return next(ctx);
    }
  }

  /// <summary>A participant whose arrival stalls on a gate — a run in flight.</summary>
  private sealed class GatedArrival : IArrived
  {
    private readonly Task _gate;

    internal GatedArrival(Task gate) => _gate = gate;

    public async Task OnArrivedAsync(IRoutingContext context) => await _gate;
  }

  [Fact]
  public async Task ObservationHook_SeesTheDecision_AndSpansTheCeremony()
  {
    var router = new ObservingRouter(new FakeShell());
    var page = new TestContent();

    await router.RouteAsync(new Request(typeof(TestContent), null,
      [Target.Of(typeof(TestContent), instance: page)]));

    Assert.Equal(new[] { "observe:Route:1", "done" }, router.Log);
    Assert.True(page.Arrived);   // the ceremony ran inside the hook's span
  }

  [Fact]
  public async Task ObservationHook_ThatSkipsNext_LeavesTheCeremonyUnrun()
  {
    var router = new SkippingRouter(new FakeShell());
    var page = new TestContent();

    await router.RouteAsync(new Request(typeof(TestContent), null,
      [Target.Of(typeof(TestContent), instance: page)]));

    // The truth committed (the sync pipeline ran); the ceremony never did —
    // the observer owns what it skipped, and the run still settles.
    Assert.Same(page, router.Model!.Current);
    Assert.False(page.Arrived);
    await router.WaitIdleAsync();
  }

  [Fact]
  public async Task ObservationHookFailure_IsReported_AndTheRunSettles()
  {
    var reported = new List<Exception>();
    var shell = new FakeShell { ErrorReporter = reported.Add };
    var router = new ThrowingRouter(shell);

    await router.RouteAsync(new Request(typeof(TestContent), null));
    await router.WaitIdleAsync();

    var failure = Assert.Single(reported);
    Assert.Contains("observer boom", failure.Message);
  }

  [Fact]
  public async Task CeremonyContext_Completion_SettlesWhenTheRunConverges()
  {
    var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var router = new CapturingRouter(new FakeShell());
    var blocker = new GatedArrival(gate.Task);

    await router.RouteAsync(new Request(typeof(TestContent), null,
      [Target.Of(typeof(TestContent), instance: blocker)]));

    IConvergenceContext? ctx = router.Observed;
    Assert.NotNull(ctx);
    Assert.False(ctx!.Completion.IsCompleted);   // the ceremony is still converging

    gate.SetResult();
    await ctx.Completion;                        // settles when the run converges
    Assert.True(ctx.Completion.IsCompleted);
  }

  /// <summary>A participant whose arrival stalls on a gate and counts its release.</summary>
  private sealed class GatedReleasable(Task gate) : IArrived, IReleasable
  {
    internal bool Released { get; private set; }

    public async Task OnArrivedAsync(IRoutingContext context) => await gate;

    public void Release() => Released = true;
  }

  [Fact]
  public async Task OverlappingRuns_KeepTheRunScopeOpenUntilTheLastSettles()
  {
    // A superseded run that unwinds after its superseder launched must not
    // close the run scope early — a trim during the survivor's convergence
    // would drain the release ledger against a still-presented chain.
    var gateA = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var gateB = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var router = new RecordingRouter(new FakeShell());
    var a = new GatedReleasable(gateA.Task);
    var b = new GatedReleasable(gateB.Task);

    await router.RouteAsync(new Request(typeof(GatedReleasable), null,
      [Target.Of(typeof(GatedReleasable), instance: a)]));
    // The superseder commits and launches its own run while the first run
    // is still stalled — the runs overlap.
    await router.RouteAsync(new Request(typeof(GatedReleasable), null,
      [Target.Of(typeof(GatedReleasable), instance: b)]));
    Assert.Equal(2, router.Ceremonies.Count);

    // Unwind the superseded run after the superseder is in flight — its
    // scope closes, the survivor's stays open.
    gateA.SetResult();
    await router.Ceremonies[0].Completion;
    Assert.True(router.Model!.IsConvergenceRunning);   // the survivor still converges

    // A trim during the survivor's convergence defers the orphan's release
    // to the run's end-of-ceremony drain.
    Assert.True(router.Model.TrimBackward());
    Assert.False(a.Released);

    // The survivor converges — its endpoint drains what the trim ledgered.
    gateB.SetResult();
    await router.WaitIdleAsync();
    Assert.True(a.Released);
  }
}
