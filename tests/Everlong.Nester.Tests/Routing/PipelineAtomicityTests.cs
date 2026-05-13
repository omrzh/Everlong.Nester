using Everlong.Nester.Routing;
using Xunit;

namespace Everlong.Nester.Tests.Routing;

/// <summary>
///   The truth transaction's atomicity and supersession — the four-phase
///   kernel under drill: settle-and-return, the supersede-able convergence
///   run, and the lifetime token.
/// </summary>
public class PipelineAtomicityTests
{
  /// <summary>An <see cref="IArrived"/> that blocks until released and signals when it is arriving.</summary>
  private sealed class GatedArrived : IArrived
  {
    private readonly Task _gate;
    private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal GatedArrived(Task gate) => _gate = gate;

    internal Task Entered => _entered.Task;

    public async Task OnArrivedAsync(IRoutingContext context)
    {
      _entered.TrySetResult();
      await _gate;
    }
  }

  private sealed class TokenContent : IArrived
  {
    internal CancellationToken? Received { get; private set; }

    public Task OnArrivedAsync(IRoutingContext context)
    {
      Received = context.Lifetime;
      return Task.CompletedTask;
    }
  }

  /// <summary>An <see cref="IArriving" /> that blocks in the arrival gate and records the run's abort token.</summary>
  private sealed class StalledArriving : IArriving
  {
    private readonly Task _gate;
    private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal StalledArriving(Task gate) => _gate = gate;

    internal Task Entered => _entered.Task;

    internal CancellationToken Token { get; private set; }

    public async Task OnArrivingAsync(IRoutingContext context)
    {
      Token = context.Lifetime;
      _entered.TrySetResult();
      await _gate;
    }
  }

  /// <summary>A participant recording the token its membership edge receives.</summary>
  private sealed class TokenCapturingRouted : IRoutable
  {
    internal CancellationToken Token { get; private set; }

    public void OnRoutedTo(IRoutingContext context) => Token = context.Lifetime;

    public void OnRoutedFrom(IRoutingContext context)
    {
    }
  }

  /// <summary>A participant whose membership edge throws — the pre-commit abort shape.</summary>
  private sealed class ThrowingRoutedTo : IRoutable
  {
    public void OnRoutedTo(IRoutingContext context) => throw new InvalidOperationException("routed-to boom");

    public void OnRoutedFrom(IRoutingContext context)
    {
    }
  }

  [Fact]
  public async Task RouteAsync_SettlesAtTheTruth_AndReturns()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var blocker = new GatedArrived(Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));

    // The arrival ceremony is still pending — yet the call has returned:
    // it settled at the truth commit, not at the convergence.
    await router.RouteAsync(new Request(typeof(TestContent), null,
      [Target.Of(typeof(TestContent), instance: blocker)]));
    await blocker.Entered;
  }

  [Fact]
  public async Task NewTruth_Supersedes_TheActiveRun()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var blocker = new GatedArrived(gate.Task);

    Task first = router.RouteAsync(new Request(typeof(TestContent), null,
      [Target.Of(typeof(TestContent), instance: blocker)]));
    await blocker.Entered;   // the first run is inside its arrival ceremony

    // The second request does not throw — it supersedes: its truth commits,
    // and the first run's ceremony is abandoned at its next boundary.
    await router.RouteAsync(new Request(typeof(PageAlpha), null));
    Assert.Equal(typeof(PageAlpha), router.Model!.CurrentChain![0].Type);

    gate.SetResult();
    await first;
    await router.WaitIdleAsync();
  }

  [Fact]
  public async Task SupersededRun_IsCutBeforeItsReveal()
  {
    var revealed = new List<Type>();
    var shell = new FakeShell();
    var router = new TestRouter(shell, reveal: scene =>
    {
      revealed.Add(scene.Chain[^1].Type);
      return Task.CompletedTask;
    });
    var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var stalled = new StalledArriving(gate.Task);

    Task first = router.RouteAsync(new Request(typeof(TestContent), null,
      [Target.Of(typeof(TestContent), instance: stalled)]));
    await stalled.Entered;   // the first run is inside its arrival gate

    // The newer truth commits and cancels the stalled run's token.
    await router.RouteAsync(new Request(typeof(PageAlpha), null));
    Assert.True(stalled.Token.IsCancellationRequested);

    gate.SetResult();
    await first;
    await router.WaitIdleAsync();

    // The cut run was abandoned at its next boundary — the stale chain never
    // reached the view, and the superseding truth owns the surface.
    Assert.Equal(new[] { typeof(PageAlpha) }, revealed);
    Assert.Equal(typeof(PageAlpha), router.View.Location!.Type);
  }

  [Fact]
  public async Task UncommittedTransaction_DoesNotSupersedeTheInFlightRun()
  {
    var revealed = new List<Type>();
    var shell = new FakeShell();
    var router = new TestRouter(shell, reveal: scene =>
    {
      revealed.Add(scene.Chain[^1].Type);
      return Task.CompletedTask;
    });
    var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var stalled = new StalledArriving(gate.Task);

    Task first = router.RouteAsync(new Request(typeof(TestContent), null,
      [Target.Of(typeof(TestContent), instance: stalled)]));
    await stalled.Entered;

    // A transaction that dies before its commit is no truth: the run still
    // converging is not superseded by it.
    await Assert.ThrowsAsync<InvalidOperationException>(
      () => router.RouteAsync(new Request(typeof(ThrowingRoutedTo), null)));
    Assert.False(stalled.Token.IsCancellationRequested);

    gate.SetResult();
    await first;
    await router.WaitIdleAsync();

    // The run converged untouched — it revealed its own chain and still owns
    // the surface.
    Assert.Equal(new[] { typeof(TestContent) }, revealed);
    Assert.Equal(typeof(TestContent), router.View.Location!.Type);
  }

  [Fact]
  public async Task AbortedTransaction_CancelsTheTokenItHandedOut()
  {
    (_, TestRouter router) = RouterTestHost.Create();
    var capturing = new TokenCapturingRouted();

    // The first membership edge takes the token, the second throws — the
    // transaction leaves the pipe without committing, so nothing behind it
    // would ever cancel the token it handed out.
    await Assert.ThrowsAsync<InvalidOperationException>(() => router.RouteAsync(
      new Request(typeof(TokenCapturingRouted), null,
        [Target.Of(typeof(TokenCapturingRouted), instance: capturing),
         Target.Of(typeof(ThrowingRoutedTo))])));

    Assert.True(capturing.Token.IsCancellationRequested);
  }

  [Fact]
  public async Task Lifetime_EndsWhenTheConvergenceEnds()
  {
    (_, TestRouter router) = RouterTestHost.Create();
    var content = new TokenContent();

    await router.RouteAsync(new Request(typeof(TokenContent), null,
      [Target.Of(typeof(TokenContent), instance: content)]));
    await router.WaitIdleAsync();

    // The transfer's lifetime spans its convergence: once the run settled, the
    // token the arrival hook was handed is cancelled.
    Assert.NotNull(content.Received);
    Assert.True(content.Received!.Value.IsCancellationRequested);
  }

  [Fact]
  public async Task Teardown_WhileConverging_EndsTheLifetime()
  {
    var shell = new FakeShell();
    var router = new TestRouter(shell);
    var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var stalled = new StalledArriving(gate.Task);

    Task first = router.RouteAsync(new Request(typeof(TestContent), null,
      [Target.Of(typeof(TestContent), instance: stalled)]));
    await stalled.Entered;   // the run is inside its arrival gate

    // The router tears down with the run still in flight — the transfer's
    // lifetime ends with it, so work holding the token stops.
    router.CloseSync(null);
    Assert.True(stalled.Token.IsCancellationRequested);

    gate.SetResult();
    await first;
    await router.WaitIdleAsync();
  }

  [Fact]
  public void Lifetime_IsCarriedByTheObservationSurface()
  {
    var ctx = new TransactionContext(RoutingDirection.Route);

    Assert.False(ctx.RoutingContext.Lifetime.IsCancellationRequested);
    ctx.Cts.Cancel();
    Assert.True(ctx.RoutingContext.Lifetime.IsCancellationRequested);
  }
}
