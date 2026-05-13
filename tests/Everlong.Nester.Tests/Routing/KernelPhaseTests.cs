using Everlong.Nester.Routing;
using Xunit;

namespace Everlong.Nester.Tests.Routing;

/// <summary>
///   The four-phase kernel under drill — the deferred-navigation book, the
///   supersede-able convergence run (collapsed-arrival self-heal), the
///   container tree, rapid bursts, the surface trim, and the idle signal.
/// </summary>
public class KernelPhaseTests
{
  /// <summary>A participant recording every callback into a shared log.</summary>
  private sealed class RecordingContent : IRoutable, IAdaptiveParameterized,
    IArriving, IArrived, IDeparting, IDeparted, IReleasable
  {
    private readonly List<string> _log;

    internal RecordingContent(string name, List<string> log)
    {
      Name = name;
      _log = log;
    }

    internal string Name { get; }

    internal Func<IArgs, IArgs, bool>? Compatible { get; set; }

    internal Action<IRoutingContext>? RoutedToHook { get; set; }

    internal Func<IRoutingContext, Task>? ArrivedHook { get; set; }

    internal Func<IRoutingContext, Task>? ArrivingHook { get; set; }

    internal IRoutingContext? LatestContext { get; private set; }

    void IRoutable.OnRoutedTo(IRoutingContext context)
    {
      LatestContext = context;
      _log.Add($"{Name}:To");
      RoutedToHook?.Invoke(context);
    }

    void IRoutable.OnRoutedFrom(IRoutingContext context) => _log.Add($"{Name}:From");

    internal IArgs? Engaged { get; private set; }

    IArgs? IParameterized.EngagedArgs => Engaged;

    bool IAdaptiveParameterized.IsAdaptable(IArgs? requested) => Compatible?.Invoke(Engaged ?? Args.Empty, requested ?? Args.Empty) ?? true;

    void IParameterized.DeliverArgs(IArgs? args)
    {
      Engaged = args;
      _log.Add($"{Name}:Deliver");
    }

    async Task IArriving.OnArrivingAsync(IRoutingContext context)
    {
      _log.Add($"{Name}:Arriving");
      if (ArrivingHook is { } hook)
        await hook(context);
    }

    async Task IArrived.OnArrivedAsync(IRoutingContext context)
    {
      _log.Add($"{Name}:Arrived");
      if (ArrivedHook is { } hook)
        await hook(context);
    }

    void IDeparting.OnDeparting(IRoutingContext context) => _log.Add($"{Name}:Departing");

    void IDeparted.OnDeparted(IRoutingContext context) => _log.Add($"{Name}:Departed");

    void IReleasable.Release() => _log.Add($"{Name}:Release");
  }

  private static Request RouteOf(params RecordingContent[] participants)
    => new(typeof(RecordingContent), null,
           [.. participants.Select(p => Target.Of(p.GetType(), instance: p))]);

  // ── the deferred-navigation book ─────────────────────────────────────────────

  [Fact]
  public async Task Navigation_FromTruthCallbacks_IsDeferredInOrder()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var log = new List<string>();
    var a = new RecordingContent("A", log);
    var b = new RecordingContent("B", log);

    // A's publish-phase callback issues a second navigation — it must
    // defer: the outer transaction's publish completes first.
    a.RoutedToHook = _ => router.RouteAsync(RouteOf(b));

    await router.RouteAsync(RouteOf(a));
    await router.WaitIdleAsync();

    Assert.Equal(2, router.Stack.Count);
    // The outer publish finished before the deferred transaction started.
    int toA = log.IndexOf("A:To");
    int toB = log.IndexOf("B:To");
    Assert.True(toA >= 0 && toB > toA);
  }

  [Fact]
  public async Task DeferredNavigation_PreservesIssueOrder()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var log = new List<string>();
    var a = new RecordingContent("A", log);
    var b = new RecordingContent("B", log);
    var c = new RecordingContent("C", log);

    // Two navigations from inside one publish — B first, C second.
    a.RoutedToHook = _ =>
    {
      router.RouteAsync(RouteOf(b));
      router.RouteAsync(RouteOf(c));
    };

    await router.RouteAsync(RouteOf(a));
    await router.WaitIdleAsync();

    // Order-preserving: B's transaction settled before C's.
    Assert.True(log.IndexOf("B:To") < log.IndexOf("C:To"));
    Assert.Equal(3, router.Stack.Count);
  }

  // ── supersession and the collapsed arrival ─────────────────────────────────────

  [Fact]
  public async Task SupersededRun_UnfinishedArrival_RerunsOnReentry()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var log = new List<string>();
    var a = new RecordingContent("A", log);
    var b = new RecordingContent("B", log);
    var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    b.ArrivingHook = _ => gate.Task;   // the run stalls in B's arrival gate — the ceremony never starts

    await router.RouteAsync(RouteOf(a, b));
    // Supersede: a new truth replaces the run stuck in B's ceremony.
    await router.RouteAsync(RouteOf(a));
    Assert.DoesNotContain("B:Arrived", log);

    gate.SetResult();
    await router.WaitIdleAsync();

    // A cut arrival leaves nothing behind — the next entry simply runs the
    // ceremony again, from scratch.
    await router.RouteAsync(RouteOf(a, b));
    await router.WaitIdleAsync();
    Assert.Contains("B:To", log);
    Assert.Contains("B:Arrived", log);
    Assert.Equal(2, log.Count(e => e == "B:Arriving"));   // the ceremony re-ran on reentry
  }

  [Fact]
  public async Task WaitIdle_SpansTheWholeConvergence()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var log = new List<string>();
    var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var a = new RecordingContent("A", log) { ArrivedHook = _ => gate.Task };

    Task route = router.RouteAsync(RouteOf(a));
    Assert.True(route.IsCompleted);              // settled at the truth
    Assert.False(router.WaitIdleAsync().IsCompleted);   // the run is still in flight

    gate.SetResult();
    await router.WaitIdleAsync();
    Assert.Contains("A:Arrived", log);
  }

  // ── the container tree ───────────────────────────────────────────────────────

  [Fact]
  public async Task Tree_ReuseExtendsAcrossHistoryEntries()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var log = new List<string>();
    var a = new RecordingContent("A", log);
    var b = new RecordingContent("B", log);
    var c = new RecordingContent("C", log);
    var d = new RecordingContent("D", log);
    var e = new RecordingContent("E", log);
    var f = new RecordingContent("F", log);

    await router.RouteAsync(RouteOf(a, b, c));
    await router.WaitIdleAsync();
    await router.RouteAsync(RouteOf(d, e));
    await router.WaitIdleAsync();

    // Routing back into the history prefix [A,B,+] — A and B revive as the
    // held instances (state, arrival record and view continue).
    await router.RouteAsync(RouteOf(a, b, f));
    await router.WaitIdleAsync();

    Assert.Same(a, router.Model!.CurrentChain![0].Instance);
    Assert.Same(b, router.Model.CurrentChain[1].Instance);
    Assert.Same(f, router.Model.CurrentChain[2].Instance);
    // Membership is edge-triggered: A and B never left the current chain
    // here (they re-entered via the tree), so no re-arrival edge fired for
    // them — but they do arrive again in this truth's ceremony.
    Assert.Equal(2, log.Count(e => e == "A:To"));
  }

  [Fact]
  public async Task Tree_IncompatibleSiblings_Coexist()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var log = new List<string>();
    var a1 = new RecordingContent("A", log) { Compatible = (_, _) => false };
    var a2 = new RecordingContent("A", log);

    await router.RouteAsync(RouteOf(a1));
    await router.WaitIdleAsync();
    // Incompatible with the held sibling — a fresh node materializes and
    // coexists in the tree.
    await router.RouteAsync(RouteOf(a2));
    await router.WaitIdleAsync();

    Assert.Same(a2, router.Model!.CurrentChain![0].Instance);
    Assert.Equal(2, router.Stack.Count);

    // The first sibling is still held by the back trail — traversing back
    // revives it.
    await shell.DispatchIntent(null, new BackIntent());
    await router.WaitIdleAsync();
    Assert.Same(a1, router.Model.CurrentChain![0].Instance);
  }

  // ── rapid bursts ─────────────────────────────────────────────────────────────

  [Fact]
  public async Task RapidTraversals_LandOnTheFinalTruth()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var log = new List<string>();
    var a = new RecordingContent("A", log);
    var b = new RecordingContent("B", log);
    var c = new RecordingContent("C", log);

    await router.RouteAsync(RouteOf(a));
    await router.RouteAsync(RouteOf(b));
    await router.RouteAsync(RouteOf(c));
    await router.WaitIdleAsync();

    for (int i = 0; i < 5; i++)
    {
      await shell.DispatchIntent(null, new BackIntent());
      await shell.DispatchIntent(null, new ForwardIntent());
    }
    await router.WaitIdleAsync();

    Assert.Same(c, router.Model!.CurrentChain![0].Instance);
    Assert.Equal(3, router.Stack.Count);
  }

  // ── the surface trim — honest at any moment ──────────────────────────────────

  [Fact]
  public async Task Trim_DeclinedInTruthCallbacks_HonoredInTheCeremony()
  {
    // The trim answers honestly at any moment: declined while a sync truth
    // is in flight (publish callbacks), honored once the ceremony runs
    // (arrival completions) — the back trail entry is gone by then.
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var log = new List<string>();
    bool? routedToResult = null;
    bool? arrivedResult = null;
    var a = new RecordingContent("A", log);
    var b = new RecordingContent("B", log)
    {
      RoutedToHook = _ => routedToResult = router.Stack.TrimBackward(),
      ArrivedHook = _ =>
      {
        arrivedResult = router.Stack.TrimBackward();
        return Task.CompletedTask;
      },
    };

    await router.RouteAsync(RouteOf(a));
    await router.WaitIdleAsync();
    await router.RouteAsync(RouteOf(b));
    await router.WaitIdleAsync();

    Assert.False(routedToResult);          // publish — the stack is being rewritten
    Assert.True(arrivedResult);            // ceremony — the back trail entry was removed
    Assert.Equal(1, router.Stack.Count);
    Assert.False(router.Model.CanGoForward);
    Assert.Contains("A:Release", log);    // the trimmed orphan released by the run's drain
  }

  [Fact]
  public async Task IdleTrim_DrainsImmediately_AndAnswersHonestly()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var log = new List<string>();
    var a = new RecordingContent("A", log);
    var b = new RecordingContent("B", log);

    await router.RouteAsync(RouteOf(a));
    await router.RouteAsync(RouteOf(b));
    await router.WaitIdleAsync();

    // Idle — no run in flight: the orphan is released immediately.
    Assert.True(router.Model!.TrimBackward());
    Assert.Contains("A:Release", log);
    Assert.Equal(1, router.Stack.Count);
    Assert.False(router.Model.CanGoBack);
    Assert.False(router.Model.TrimBackward());   // nothing behind now
  }

  [Fact]
  public async Task TrimBackward_QueuesOrphanRelease()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var log = new List<string>();
    var a = new RecordingContent("A", log);
    var b = new RecordingContent("B", log);
    var c = new RecordingContent("C", log);

    await router.RouteAsync(RouteOf(a));
    await router.RouteAsync(RouteOf(b));
    await router.RouteAsync(RouteOf(c));
    await router.WaitIdleAsync();
    await shell.DispatchIntent(null, new BackIntent());   // pointer on B
    await router.WaitIdleAsync();

    // Trim the entry behind the pointer ([A]) — the history-only mutation;
    // idle, so the orphan is released right away.
    Assert.True(router.Model!.TrimBackward());
    Assert.Equal(2, router.Stack.Count);
    Assert.Contains("A:Release", log);

    // The tree forgot A: routing the type again materializes a fresh node.
    var a2 = new RecordingContent("A2", log);
    await router.RouteAsync(RouteOf(a2));
    await router.WaitIdleAsync();
    Assert.Contains("A2:To", log);
  }

  // ── teardown ─────────────────────────────────────────────────────────────────

  [Fact]
  public async Task Teardown_SharedNode_IsReleasedOnce()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var log = new List<string>();
    var a = new RecordingContent("A", log);
    var b = new RecordingContent("B", log);
    var c = new RecordingContent("C", log);

    await router.RouteAsync(RouteOf(a, b));
    await router.WaitIdleAsync();
    await router.RouteAsync(RouteOf(a, c));   // A is shared across both entries
    await router.WaitIdleAsync();

    router.CloseSync(null);

    // A appears in two entries but releases exactly once — a sync teardown
    // runs no departure pair (the router is dying, not navigating); the
    // release tail is the whole ceremony.
    Assert.Equal(0, log.Count(e => e == "A:Departing"));
    Assert.Equal(0, log.Count(e => e == "A:Departed"));
    Assert.Equal(1, log.Count(e => e == "A:Release"));
    Assert.Equal(1, log.Count(e => e == "B:Release"));
    Assert.Equal(1, log.Count(e => e == "C:Release"));
  }
}
