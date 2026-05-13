using Everlong.Nester.Routing;
using Xunit;

namespace Everlong.Nester.Tests.Routing;

/// <summary>
///   The release invariant (`docs/design/routing.md` §9): the
///   departure ceremonies belong to leaving the presentation — a trimmed
///   (history-evicted) node already ran them when it left, so the trim
///   release runs the <see cref="IReleasable" /> hook only, never a second
///   departure pair.
/// </summary>
public class ReleaseCeremonyTests
{
  /// <summary>A participant counting its departure and release ceremonies.</summary>
  private sealed class Counted : IDeparting, IDeparted, IReleasable
  {
    internal int Departing { get; private set; }

    internal int Departed { get; private set; }

    internal int Released { get; private set; }

    public void OnDeparting(IRoutingContext context) => Departing++;

    public void OnDeparted(IRoutingContext context) => Departed++;

    public void Release() => Released++;
  }

  [Fact]
  public async Task TrimmedNode_RunsReleaseOnly_NeverARedeparture()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var a = new Counted();
    var b = new Counted();

    await router.RouteAsync(new Request(typeof(Counted), null, [Target.Of(typeof(Counted), instance: a)]));
    await router.RouteAsync(new Request(typeof(Counted), null, [Target.Of(typeof(Counted), instance: b)]));
    // Back to A — B leaves the presentation: one departure pair, retained.
    await shell.DispatchIntent(null, new BackIntent());
    await router.WaitIdleAsync();
    Assert.Equal(1, b.Departing);
    Assert.Equal(1, b.Departed);
    Assert.Equal(0, b.Released);

    // Trim B's retained entry — the eviction runs the release tail only.
    Assert.True(router.Model!.TrimForward());
    Assert.Equal(1, b.Departing);   // unchanged — no second ceremony
    Assert.Equal(1, b.Departed);
    Assert.Equal(1, b.Released);
    Assert.False(router.Model.CanGoForward);
  }

  /// <summary>A page that stalls its arrival at a gate — the test is the only one who opens it.</summary>
  private sealed class Gated(Task gate) : IArrived, IReleasable
  {
    internal int Released { get; private set; }

    public async Task OnArrivedAsync(IRoutingContext context) => await gate;

    public void Release() => Released++;
  }

  [Fact]
  public async Task SharedNode_SurvivesTheTrimOfOneEntry()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var layout = new Counted();
    var pageA = new Counted();
    var pageB = new Counted();

    await router.RouteAsync(new Request(typeof(Counted), null,
      [Target.Of(typeof(Counted), instance: layout), Target.Of(typeof(Counted), instance: pageA)]));
    await router.RouteAsync(new Request(typeof(Counted), null,
      [Target.Of(typeof(Counted), instance: layout), Target.Of(typeof(Counted), instance: pageB)]));
    await shell.DispatchIntent(null, new BackIntent());
    await router.WaitIdleAsync();

    // Trim the forward entry [layout, pageB] — layout is shared with the
    // current entry, so only pageB is evicted.
    Assert.True(router.Model!.TrimForward());
    Assert.Equal(0, layout.Released);
    Assert.Equal(1, pageB.Released);
  }

  [Fact]
  public async Task CapEvictedEntry_ClosedWhileConverging_ReleasesThroughTheCloseDrain()
  {
    (_, TestRouter router) = RouterTestHost.Create();
    TestRouter present = (TestRouter)router.Derive();
    present.Stack.MaxDepth = 1;
    var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var a = new Counted();
    var b = new Gated(gate.Task);

    await present.RouteAsync(new Request(typeof(Counted), null, [Target.Of(typeof(Counted), instance: a)]));
    // The cap evicted A into the release ledger; B's run stalls mid-
    // convergence.
    await present.RouteAsync(new Request(typeof(Gated), null, [Target.Of(typeof(Gated), instance: b)]));

    present.Completion!.Complete("x");
    Assert.Equal("x", await present.Completion!.Result);

    // The close drain must sweep the ledger too — the evicted pair
    // releases alongside the presented one.
    Assert.Equal(1, a.Released);
    Assert.Equal(1, b.Released);

    // The superseded run unwinds — nobody releases twice.
    gate.SetResult();
    await present.WaitIdleAsync();
    Assert.Equal(1, a.Released);
    Assert.Equal(1, b.Released);
  }

  [Fact]
  public async Task TrimmedEntry_ClosedWhileConverging_ReleasesThroughTheCloseDrain()
  {
    (_, TestRouter router) = RouterTestHost.Create();
    TestRouter present = (TestRouter)router.Derive();
    var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var a = new Counted();
    var b = new Gated(gate.Task);

    await present.RouteAsync(new Request(typeof(Counted), null, [Target.Of(typeof(Counted), instance: a)]));
    await present.RouteAsync(new Request(typeof(Gated), null, [Target.Of(typeof(Gated), instance: b)]));

    // B converges with A's trail entry trimmed into the ledger mid-run.
    Assert.True(present.Model.TrimBackward());
    present.Completion!.Complete("x");
    Assert.Equal("x", await present.Completion!.Result);

    Assert.Equal(1, a.Released);
    Assert.Equal(1, b.Released);

    gate.SetResult();
    await present.WaitIdleAsync();
    Assert.Equal(1, a.Released);
    Assert.Equal(1, b.Released);
  }

  // ── materialized-but-uncommitted ──────────────────────────────────────

  /// <summary>A participant whose membership edge aborts the transaction — the node materialized for it never commits.</summary>
  private sealed class AbortOnEnter : IRoutable, IReleasable
  {
    internal int Released { get; private set; }

    public void OnRoutedTo(IRoutingContext context) => throw new InvalidOperationException("abort");

    public void OnRoutedFrom(IRoutingContext context)
    {
    }

    public void Release() => Released++;
  }

  [Fact]
  public async Task MaterializedNode_AbortedBeforeCommit_Releases()
  {
    (_, TestRouter router) = RouterTestHost.Create();
    var page = new AbortOnEnter();

    // The route materializes a fresh node, then its membership edge throws —
    // the transaction aborts before the commit, abandoning the instance.
    await Assert.ThrowsAsync<InvalidOperationException>(
      () => router.RouteAsync(new Request(typeof(AbortOnEnter), null,
        [Target.Of(typeof(AbortOnEnter), instance: page)])));

    Assert.Equal(1, page.Released);
  }

  [Fact]
  public async Task MaterializedNode_StillHeldElsewhere_IsNotReleased()
  {
    (_, TestRouter router) = RouterTestHost.Create();
    var layout = new LayoutAlpha();
    var page = new PageAlpha();
    var abort = new AbortOnEnter();

    await router.RouteAsync(new Request(typeof(LayoutAlpha), null,
      [Target.Of(typeof(LayoutAlpha), instance: layout), Target.Of(typeof(PageAlpha), instance: page)]));
    await router.WaitIdleAsync();

    // The same page instance materializes again at a different position; the
    // aborting sibling throws before the commit.  The page stays pinned by the
    // first entry, so only the abandoned sibling releases.
    await Assert.ThrowsAsync<InvalidOperationException>(
      () => router.RouteAsync(new Request(typeof(PageAlpha), null,
        [Target.Of(typeof(PageAlpha), instance: page), Target.Of(typeof(AbortOnEnter), instance: abort)])));

    Assert.Equal(1, abort.Released);
    Assert.False(page.Released);   // still held by the first entry
    Assert.Same(page, router.Model!.CurrentChain![1].Instance);
  }

  // ── instance identity ─────────────────────────────────────────────────

  /// <summary>A participant with value equality — two distinct instances compare equal.</summary>
  private sealed class Valued(string key) : IReleasable
  {
    private readonly string _key = key;

    internal int Released { get; private set; }

    public override bool Equals(object? obj) => obj is Valued other && other._key == _key;

    public override int GetHashCode() => _key.GetHashCode();

    public void Release() => Released++;
  }

  [Fact]
  public async Task ValueEqualInstances_AreHeldSeparately()
  {
    (_, TestRouter router) = RouterTestHost.Create();
    var first = new Valued("same");
    var second = new Valued("same");

    await router.RouteAsync(new Request(typeof(Valued), null,
      [Target.Of(typeof(Valued), instance: first)]));
    await router.RouteAsync(new Request(typeof(Valued), null,
      [Target.Of(typeof(Valued), instance: second)]));
    await router.WaitIdleAsync();

    // Two equal-but-distinct instances are two holdings: trimming the entry
    // behind the current one evicts `first` alone.  A retention table keyed
    // by value collapses the pair into one holding and leaves `first` held.
    Assert.True(router.Model!.TrimBackward());
    Assert.Equal(1, first.Released);
    Assert.Equal(0, second.Released);
  }
}
