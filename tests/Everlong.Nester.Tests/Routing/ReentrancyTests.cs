using Everlong.Nester.Routing;
using Xunit;

namespace Everlong.Nester.Tests.Routing;

/// <summary>
///   Reentrant navigation probes across the transaction/convergence split: what
///   happens when user code issues a navigation from inside a routing
///   callback.  The three shapes —
///
///   transaction-phase sync callbacks (OnRoutedTo / OnBodyChanged): fire-and-forget
///     defers onto the same pump batch and supersedes the current
///     transaction's convergence (no deadlock); a blocking wait on the returned
///     task starves the pump (deadlock — the documented case);
///   convergence-phase async hooks (OnArrivingAsync / OnArrivedAsync): awaiting
///     a navigation lands the re-entrant transaction (queued while the convergence is
///     still inline in the pump, or a fresh pump after it yielded) and cuts
///     the current run at the next stage boundary — no deadlock.
/// </summary>
public class ReentrancyTests
{
  /// <summary>A participant recording every callback into a shared log, with per-face redirect hooks.</summary>
  private sealed class Probe : IRoutable, IBodyChanged, IParameterized, IArriving, IArrived, IDeparting, IDeparted
  {
    private readonly List<string> _log;

    internal Probe(string name, List<string> log)
    {
      Name = name;
      _log = log;
    }

    internal string Name { get; }

    internal Action? RoutedToRedirect { get; set; }       // fire-and-forget navigation issued from OnRoutedTo (once)

    internal Action? BodyChangedRedirect { get; set; }    // fire-and-forget navigation issued from OnBodyChanged (once)

    internal Func<Task>? ArrivingAwait { get; set; }      // navigation awaited from OnArrivingAsync (once)

    internal Func<Task>? ArrivedAwait { get; set; }       // navigation awaited from OnArrivedAsync (once)

    internal int ArrivingCount { get; private set; }

    internal int ArrivedCount { get; private set; }

    internal bool Departed { get; private set; }

    public void OnRoutedTo(IRoutingContext context)
    {
      _log.Add($"{Name}:To");
      Action? redirect = RoutedToRedirect;
      RoutedToRedirect = null;
      redirect?.Invoke();
    }

    public void OnRoutedFrom(IRoutingContext context) => _log.Add($"{Name}:From");

    public void OnBodyChanged(IReadOnlyList<object> bodyChain)
    {
      // Only a node with a nonempty body (the chain's head) participates —
      // a fresh leaf's empty-body change is not a redirect trigger.
      if (bodyChain.Count == 0)
        return;
      Action? redirect = BodyChangedRedirect;
      BodyChangedRedirect = null;
      redirect?.Invoke();
    }

    public IArgs? EngagedArgs { get; private set; }

    public void DeliverArgs(IArgs? args)
    {
      EngagedArgs = args;
    }

    public async Task OnArrivingAsync(IRoutingContext context)
    {
      ArrivingCount++;
      _log.Add($"{Name}:Arriving");
      Func<Task>? hook = ArrivingAwait;
      ArrivingAwait = null;
      if (hook is not null)
        await hook();
    }

    public async Task OnArrivedAsync(IRoutingContext context)
    {
      ArrivedCount++;
      _log.Add($"{Name}:Arrived");
      Func<Task>? hook = ArrivedAwait;
      ArrivedAwait = null;
      if (hook is not null)
        await hook();
    }

    public void OnDeparting(IRoutingContext context) => _log.Add($"{Name}:Departing");

    public void OnDeparted(IRoutingContext context)
    {
      Departed = true;
      _log.Add($"{Name}:Departed");
    }
  }

  private static Request Chain(params Probe[] participants)
    => new(typeof(Probe), null, [.. participants.Select(p => Target.Of(p.GetType(), instance: p))]);

  // ── transaction-phase sync callbacks — fire-and-forget ─────────────────────────────

  [Fact]
  public async Task TruthCallback_FireForgetNavigation_NoDeadlock_OuterCeremonySuperseded()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var log = new List<string>();
    var a = new Probe("A", log);
    var b = new Probe("B", log);
    a.RoutedToRedirect = () => router.RouteAsync(Chain(b));   // fire-and-forget

    await router.RouteAsync(Chain(a));
    await router.WaitIdleAsync();

    // No deadlock: the second transaction queued behind the first in the same
    // pump batch and landed after it; the first transaction's convergence was
    // superseded before it launched — A never arrived.
    Assert.Equal(2, router.Stack.Count);
    Assert.Same(b, router.Model!.Current);
    Assert.Equal(0, a.ArrivingCount);
    Assert.Equal(0, a.ArrivedCount);
    Assert.Equal(1, b.ArrivedCount);
    Assert.True(log.IndexOf("A:To") < log.IndexOf("B:To"));   // issue order preserved
  }

  [Fact]
  public async Task TruthCallback_AsyncVoidNavigation_NoDeadlock_FollowUpRunsAfterLanding()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var log = new List<string>();
    var a = new Probe("A", log);
    var b = new Probe("B", log);
    var followUp = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

    // A transaction-phase (sync) callback launches an async void method that
    // awaits the navigation — the await yields instead of blocking, so the
    // re-entrant transaction queues behind the current one and the follow-up
    // only observes the landing.
    a.RoutedToRedirect = async () =>
    {
      await router.RouteAsync(Chain(b));
      log.Add("FollowUp");
      followUp.SetResult();
    };

    await router.RouteAsync(Chain(a));
    await followUp.Task;
    await router.WaitIdleAsync();

    Assert.Same(b, router.Model!.Current);
    Assert.Equal(2, router.Stack.Count);
    Assert.Equal(0, a.ArrivingCount);   // the outer convergence was superseded, as with plain fire-and-forget
    Assert.Equal(1, b.ArrivedCount);
    // The follow-up observes the landing — it runs after the second transaction's publish.
    Assert.True(log.IndexOf("FollowUp") > log.IndexOf("B:To"));
  }

  [Fact]
  public async Task BodyChangedCallback_FireForgetNavigation_NoDeadlock_IntermediateNeverArrived()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var log = new List<string>();
    var layout = new Probe("Layout", log);
    var leafA = new Probe("A", log);
    var leafB = new Probe("B", log);
    var leafC = new Probe("C", log);

    await router.RouteAsync(Chain(layout, leafA));
    await router.WaitIdleAsync();
    Assert.Equal(1, leafA.ArrivedCount);   // the first convergence ran normally

    // The head redirects (once) when its body changes — from nav2's publish.
    layout.BodyChangedRedirect = () => router.RouteAsync(Chain(layout, leafC));

    await router.RouteAsync(Chain(layout, leafB));
    await router.WaitIdleAsync();

    Assert.Same(leafC, router.Model!.Current);
    Assert.Equal(3, router.Stack.Count);
    // The intermediate B never arrived (its convergence was superseded by the
    // redirect's transaction) and departed with the winning convergence instead.
    Assert.Equal(0, leafB.ArrivingCount);
    Assert.Equal(0, leafB.ArrivedCount);
    Assert.True(leafB.Departed);
    Assert.Equal(1, leafC.ArrivedCount);
  }

  // ── transaction-phase sync callbacks — the blocking shape (the documented deadlock) ─

  [Fact]
  public void TruthCallback_BlockingNavigation_DeadlocksThePump()
  {
    var log = new List<string>();
    bool completed = false;
    Exception? failure = null;
    var worker = new Thread(() =>
    {
      try
      {
        (FakeShell shell, TestRouter router) = RouterTestHost.Create();
        var a = new Probe("A", log);
        var b = new Probe("B", log);
        // OnRoutedTo runs inside the pump's sync region — blocking on the
        // re-entrant landing can never complete: the pump only reaches the
        // queued transaction after this callback returns.
        a.RoutedToRedirect = () => router.RouteAsync(Chain(b)).GetAwaiter().GetResult();
        router.RouteAsync(Chain(a)).GetAwaiter().GetResult();
        completed = true;
      }
      catch (Exception e)
      {
        failure = e;
      }
    })
    { IsBackground = true };
    worker.Start();

    Assert.False(worker.Join(TimeSpan.FromSeconds(3)), "The pump is starved — a blocking navigation from a transaction callback must not return.");
    Assert.False(completed);
    Assert.Null(failure);
    // Deliberately abandoned: the worker is stuck inside OnRoutedTo forever.
  }

  // ── convergence-phase async hooks — the awaited shape ───────────────────────────

  [Fact]
  public async Task ArrivedHook_AwaitedNavigation_NoDeadlock_RunCutAtCompletion()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var log = new List<string>();
    var a = new Probe("A", log);
    var b = new Probe("B", log);
    a.ArrivedAwait = async () => await router.RouteAsync(Chain(b));   // awaited, once

    await router.RouteAsync(Chain(a));
    await router.WaitIdleAsync();

    // No deadlock: the awaited transaction queued while the convergence was still
    // inline in the pump; the first run was cut at its completion stage
    // (A had already arrived), the re-entrant convergence ran to the end.
    Assert.Same(b, router.Model!.Current);
    Assert.Equal(2, router.Stack.Count);
    Assert.Equal(1, a.ArrivedCount);
    Assert.Equal(1, b.ArrivedCount);
    Assert.True(log.IndexOf("A:Arrived") < log.IndexOf("B:To"));
  }

  [Fact]
  public async Task ArrivingHook_AwaitedNavigation_NoDeadlock_ArrivalCutAtTheGate()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var log = new List<string>();
    var a = new Probe("A", log);
    var b = new Probe("B", log);
    a.ArrivingAwait = async () => await router.RouteAsync(Chain(b));   // awaited, once

    await router.RouteAsync(Chain(a));
    await router.WaitIdleAsync();

    // The awaited navigation landed and its convergence left A and entered B;
    // A's own arrival gate was cut mid-run (it never completed its arrival)
    // and it departed with the superseding run instead.
    Assert.Same(b, router.Model!.Current);
    Assert.Equal(2, router.Stack.Count);
    Assert.Equal(1, a.ArrivingCount);
    Assert.Equal(0, a.ArrivedCount);       // cut at the gate — never arrived
    Assert.True(a.Departed);               // departed by the superseding run
    Assert.Equal(1, b.ArrivedCount);
  }
}
