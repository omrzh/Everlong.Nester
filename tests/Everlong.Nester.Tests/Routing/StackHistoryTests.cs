using Everlong.Nester.Intent;
using Everlong.Nester.Routing;
using Xunit;

namespace Everlong.Nester.Tests.Routing;

/// <summary>
///   Pins the history stack's structural contracts — trail ordering,
///   adjacency, forward-trail clearing, and instance retention.  These
///   hold for every representation the stack may take.
/// </summary>
public class StackHistoryTests
{
  private static Request Page(Type page)
    => new(page, null, [Target.Of(page)]);

  private static async Task RouteEach(TestRouter router, params Type[] pages)
  {
    foreach (var page in pages)
      await router.RouteAsync(Page(page));
  }

  private static List<Type> Leaves(IReadOnlyList<ILocation> entries)
    => entries.Select(site => site.Type).ToList();

  [Fact]
  public async Task BackStack_AndForwardStack_OrderNearestFirst()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await RouteEach(router, typeof(PageAlpha), typeof(PageBeta), typeof(PageGamma));

    // Current is Gamma — the back trail reads nearest first.
    Assert.Equal([typeof(PageBeta), typeof(PageAlpha)], Leaves(router.Stack.BackStack()));

    await shell.DispatchIntent(null, new BackIntent());
    await shell.DispatchIntent(null, new BackIntent());

    // Current is Alpha — the forward trail reads nearest first too.
    Assert.Equal([typeof(PageBeta), typeof(PageGamma)], Leaves(router.Stack.ForwardStack()));
  }

  [Fact]
  public async Task Snapshot_ReadsOldestFirst_TraversalKeepsEntries()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await RouteEach(router, typeof(PageAlpha), typeof(PageBeta), typeof(PageGamma));
    await shell.DispatchIntent(null, new BackIntent());

    // Traversal moves the cursor only — every entry stays.
    Assert.Equal(
      [typeof(PageAlpha), typeof(PageBeta), typeof(PageGamma)],
      Leaves(router.Stack.Snapshot()));
    Assert.Equal(3, router.Stack.Count);
    Assert.Equal(typeof(PageBeta), router.Stack.Location!.Type);
  }

  [Fact]
  public async Task PushFromMidStack_ClearsTheForwardTrail()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await RouteEach(router, typeof(PageAlpha), typeof(PageBeta), typeof(PageGamma));
    await shell.DispatchIntent(null, new BackIntent());
    await shell.DispatchIntent(null, new BackIntent());
    await router.RouteAsync(Page(typeof(PageDelta)));

    // The push lands behind the cursor's position and erases the stale
    // forward trail — a back from Delta returns to Alpha, never to the
    // dropped Beta/Gamma.
    Assert.Equal(2, router.Stack.Count);
    Assert.Equal(typeof(PageDelta), router.Stack.Location!.Type);
    Assert.Empty(router.Stack.ForwardStack());
    Assert.Equal([typeof(PageAlpha)], Leaves(router.Stack.BackStack()));
    Assert.Equal([typeof(PageAlpha), typeof(PageDelta)], Leaves(router.Stack.Snapshot()));
  }

  [Fact]
  public async Task BackForwardRoundtrip_RestoresTheSameInstances()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await RouteEach(router, typeof(PageAlpha), typeof(PageBeta), typeof(PageGamma));
    object alpha = router.Stack.BackStack()[^1].Instance;
    object gamma = router.Stack.Location!.Instance;

    await shell.DispatchIntent(null, new BackIntent());
    await shell.DispatchIntent(null, new BackIntent());
    Assert.Same(alpha, router.Stack.Location!.Instance);

    await shell.DispatchIntent(null, new ForwardIntent());
    await shell.DispatchIntent(null, new ForwardIntent());
    Assert.Same(gamma, router.Stack.Location!.Instance);
    Assert.Equal(3, router.Stack.Count);
  }

  [Fact]
  public async Task BackAtTheFoot_IsConsumed_TheStackUnchanged()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(Page(typeof(PageAlpha)));

    Assert.Equal(IntentResult.Handled, await shell.DispatchIntent(null, new BackIntent()));

    Assert.Equal(1, router.Stack.Count);
    Assert.Equal(typeof(PageAlpha), router.Stack.Location!.Type);
    Assert.False(router.Stack.CanGoBack);
  }

  [Fact]
  public async Task ForwardAtTheTop_PassesThrough()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await RouteEach(router, typeof(PageAlpha), typeof(PageBeta));

    Assert.Equal(IntentResult.Pass, await shell.DispatchIntent(null, new ForwardIntent()));

    Assert.Equal(2, router.Stack.Count);
    Assert.Equal(typeof(PageBeta), router.Stack.Location!.Type);
  }

  [Fact]
  public async Task TrimBackward_FromMidStack_RemovesOnlyTheEntryBehind()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await RouteEach(router, typeof(PageAlpha), typeof(PageBeta), typeof(PageGamma));
    await shell.DispatchIntent(null, new BackIntent());

    Assert.True(router.Model.TrimBackward());

    // Current stays on Beta; the entry behind is gone, the one ahead stays.
    Assert.Equal(2, router.Stack.Count);
    Assert.Equal(typeof(PageBeta), router.Stack.Location!.Type);
    Assert.Empty(router.Stack.BackStack());
    Assert.Equal([typeof(PageGamma)], Leaves(router.Stack.ForwardStack()));
  }

  [Fact]
  public async Task TrimForward_FromMidStack_RemovesOnlyTheEntryAhead()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await RouteEach(router, typeof(PageAlpha), typeof(PageBeta), typeof(PageGamma));
    await shell.DispatchIntent(null, new BackIntent());

    Assert.True(router.Model.TrimForward());

    Assert.Equal(2, router.Stack.Count);
    Assert.Equal(typeof(PageBeta), router.Stack.Location!.Type);
    Assert.Empty(router.Stack.ForwardStack());
    Assert.Equal([typeof(PageAlpha)], Leaves(router.Stack.BackStack()));
  }

  [Fact]
  public async Task TrimBelowTheCursor_LeavesTraversalIntact()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await RouteEach(router, typeof(PageAlpha), typeof(PageBeta), typeof(PageGamma));
    await shell.DispatchIntent(null, new BackIntent());

    // Alpha sits below the cursor — removing it must not disturb Beta's
    // position or the forward reach.
    Assert.True(router.Model.TrimBackward());
    await shell.DispatchIntent(null, new ForwardIntent());

    Assert.Equal(typeof(PageGamma), router.Stack.Location!.Type);
    Assert.Equal([typeof(PageBeta)], Leaves(router.Stack.BackStack()));
    Assert.Equal(2, router.Stack.Count);
  }

  [Fact]
  public async Task TrimAboveTheCursor_LeavesTraversalIntact()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await RouteEach(router, typeof(PageAlpha), typeof(PageBeta), typeof(PageGamma));
    await shell.DispatchIntent(null, new BackIntent());

    Assert.True(router.Model.TrimForward());
    await shell.DispatchIntent(null, new BackIntent());

    // The cursor lands on Alpha with Beta as the (shifted) forward trail.
    Assert.Equal(typeof(PageAlpha), router.Stack.Location!.Type);
    Assert.Empty(router.Stack.BackStack());
    Assert.Equal([typeof(PageBeta)], Leaves(router.Stack.ForwardStack()));
    Assert.Equal(2, router.Stack.Count);
  }

  // ── the depth ceiling — eviction happens at the push, never elsewhere ───

  [Fact]
  public async Task MaxDepth_Unset_StackGrowsUnbounded()
  {
    (_, TestRouter router) = RouterTestHost.Create();
    await RouteEach(router, typeof(PageAlpha), typeof(PageBeta), typeof(PageGamma), typeof(PageDelta));

    Assert.Equal(4, router.Stack.Count);
  }

  [Fact]
  public async Task MaxDepth_AtTheLimit_NothingIsEvicted()
  {
    (_, TestRouter router) = RouterTestHost.Create();
    router.Stack.MaxDepth = 3;
    await RouteEach(router, typeof(PageAlpha), typeof(PageBeta), typeof(PageGamma));

    Assert.Equal(3, router.Stack.Count);
    Assert.Equal([typeof(PageBeta), typeof(PageAlpha)], Leaves(router.Stack.BackStack()));
  }

  [Fact]
  public async Task MaxDepth_BeyondTheLimit_EvictsTheOldest_KeepsTraversalCorrect()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    router.Stack.MaxDepth = 2;
    await RouteEach(router, typeof(PageAlpha), typeof(PageBeta), typeof(PageGamma), typeof(PageDelta));

    // Steady state — the ceiling holds after every push, oldest first out.
    Assert.Equal(2, router.Stack.Count);
    Assert.Equal([typeof(PageGamma), typeof(PageDelta)], Leaves(router.Stack.Snapshot()));
    Assert.Equal(typeof(PageDelta), router.Stack.Location!.Type);

    // The cursor survived the evictions below it — back lands on Gamma.
    Assert.Equal(IntentResult.Handled, await shell.DispatchIntent(null, new BackIntent()));
    Assert.Equal(typeof(PageGamma), router.Stack.Location!.Type);
    Assert.False(router.Stack.CanGoBack);
  }

  [Fact]
  public async Task MaxDepth_TraversalNeverEvicts()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    router.Stack.MaxDepth = 3;
    await RouteEach(router, typeof(PageAlpha), typeof(PageBeta), typeof(PageGamma));
    await shell.DispatchIntent(null, new BackIntent());
    await shell.DispatchIntent(null, new BackIntent());

    Assert.Equal(3, router.Stack.Count);
    Assert.Equal([typeof(PageBeta), typeof(PageGamma)], Leaves(router.Stack.ForwardStack()));
  }

  [Fact]
  public async Task MaxDepth_TighteningAppliesOnTheNextPush()
  {
    (_, TestRouter router) = RouterTestHost.Create();
    await RouteEach(router, typeof(PageAlpha), typeof(PageBeta), typeof(PageGamma));

    // Tightening an existing stack defers to the push — never an eager
    // mutation at the setter.
    router.Stack.MaxDepth = 2;
    Assert.Equal(3, router.Stack.Count);

    await router.RouteAsync(Page(typeof(PageDelta)));
    Assert.Equal(2, router.Stack.Count);
    Assert.Equal([typeof(PageGamma), typeof(PageDelta)], Leaves(router.Stack.Snapshot()));
  }

  [Fact]
  public async Task MaxDepth_ZeroOrNegative_IsRejected()
  {
    (_, TestRouter router) = RouterTestHost.Create();

    Assert.Throws<ArgumentOutOfRangeException>(() => router.Stack.MaxDepth = 0);
    Assert.Throws<ArgumentOutOfRangeException>(() => router.Stack.MaxDepth = -1);
    Assert.Null(router.Stack.MaxDepth);
  }

  [Fact]
  public async Task MaxDepth_EvictedEntries_ReleaseThroughTheLedger()
  {
    (_, TestRouter router) = RouterTestHost.Create();
    router.Stack.MaxDepth = 2;
    var a = new ReleasablePage();
    var b = new ReleasablePage();
    var c = new ReleasablePage();
    var d = new ReleasablePage();

    await router.RouteAsync(PageOf(a));
    await router.RouteAsync(PageOf(b));
    await router.RouteAsync(PageOf(c));
    await router.RouteAsync(PageOf(d));
    await router.WaitIdleAsync();

    // The evicted pair released exactly once — the survivors never.
    Assert.Equal(1, a.Released);
    Assert.Equal(1, b.Released);
    Assert.Equal(0, c.Released);
    Assert.Equal(0, d.Released);
  }

  private static Request PageOf(ReleasablePage page)
    => new(typeof(ReleasablePage), null, [Target.Of(typeof(ReleasablePage), instance: page)]);

  /// <summary>A page that counts its release tail — eviction ledger accounting.</summary>
  private sealed class ReleasablePage : IArrived, IReleasable
  {
    internal int Released { get; private set; }

    public Task OnArrivedAsync(IRoutingContext context) => Task.CompletedTask;

    public void Release() => Released++;
  }
}
