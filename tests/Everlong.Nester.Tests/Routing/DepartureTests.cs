using Everlong.Nester.Routing;
using Xunit;

using Everlong.Nester.Intent;
namespace Everlong.Nester.Tests.Routing;

public class DepartureTests
{
  /// <summary>A participant recording every lifecycle call in a shared log.</summary>
  private sealed class Recorder : IArriving, IArrived, IDeparting, IDeparted, IReleasable
  {
    private readonly List<(string Hook, Recorder Who)> _log;

    internal Recorder(List<(string Hook, Recorder Who)> log) => _log = log;

    public Task OnArrivingAsync(IRoutingContext context)
    {
      _log.Add(("Arriving", this));
      return Task.CompletedTask;
    }

    public Task OnArrivedAsync(IRoutingContext context)
    {
      _log.Add(("Arrived", this));
      return Task.CompletedTask;
    }

    public void OnDeparting(IRoutingContext context) => _log.Add(("Departing", this));

    public void OnDeparted(IRoutingContext context) => _log.Add(("Departed", this));

    public void Release() => _log.Add(("Release", this));
  }

  private static Request Chain(Recorder layout, Recorder page)
    => new(typeof(Recorder), null,
      [Target.Of(typeof(Recorder), instance: layout),
       Target.Of(typeof(Recorder), instance: page)]);

  [Fact]
  public async Task Turnover_PhasesRunInMirrorOrder()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var log = new List<(string Hook, Recorder Who)>();
    var layoutA = new Recorder(log);
    var pageA = new Recorder(log);
    var layoutB = new Recorder(log);
    var pageB = new Recorder(log);

    await router.RouteAsync(Chain(layoutA, pageA));
    log.Clear();

    await router.RouteAsync(Chain(layoutB, pageB));

    // Before the reveal: departing innermost first, then arriving outermost
    // first.  After the reveal the mirror completes: departed innermost
    // first, then arrived outermost first.  The leaving chain stays in the
    // back trail — held by the stack, released only when evicted (close /
    // trail clear / trim).
    Assert.Equal(
      [("Departing", pageA), ("Departing", layoutA),
       ("Arriving", layoutB), ("Arriving", pageB),
       ("Departed", pageA), ("Departed", layoutA),
       ("Arrived", layoutB), ("Arrived", pageB)],
      log);
  }

  [Fact]
  public async Task Back_RunsDepartureRitualOnLeavingChain()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    // The turnover above already departed B's chain — the log restarts for
    // the Back ceremony.  Replay never releases: the forward trail keeps
    // the leaving chain's instances.
    var log = new List<(string Hook, Recorder Who)>();
    var layoutA = new Recorder(log);
    var pageA = new Recorder(log);
    var layoutB = new Recorder(log);
    var pageB = new Recorder(log);

    await router.RouteAsync(Chain(layoutA, pageA));
    log.Clear();
    await router.RouteAsync(Chain(layoutB, pageB));
    log.Clear();

    await shell.DispatchIntent(null, new BackIntent());

    // Back: departing innermost first, then arriving outermost first; after
    // the reveal the mirror completes — departed innermost first, then
    // arrived outermost first.  Replay does not release — the forward trail
    // keeps its instances.
    Assert.Equal(
      [("Departing", pageB), ("Departing", layoutB),
       ("Arriving", layoutA), ("Arriving", pageA),
       ("Departed", pageB), ("Departed", layoutB),
       ("Arrived", layoutA), ("Arrived", pageA)],
      log);
  }

  /// <summary>A page that trims itself from the forward stack when departed.</summary>
  private sealed class SelfTrimmingPage : IArriving, IArrived, IDeparting, IDeparted, IReleasable
  {
    internal TestRouter? Router { get; set; }

    internal bool Released { get; set; }

    public Task OnArrivingAsync(IRoutingContext context) => Task.CompletedTask;

    public Task OnArrivedAsync(IRoutingContext context) => Task.CompletedTask;

    public void OnDeparting(IRoutingContext context) { }

    public void OnDeparted(IRoutingContext context) => Router?.Stack.TrimForward();

    public void Release() => Released = true;
  }

  [Fact]
  public async Task Back_OnDeparted_TrimForward_ReleasesOrphans()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var layout = new Recorder([]);
    var pageA = new Recorder([]);
    var pageB = new SelfTrimmingPage { Router = router };

    await router.RouteAsync(Chain(layout, pageA));
    await router.RouteAsync(new Request(typeof(SelfTrimmingPage), null,
      [Target.Of(typeof(Recorder), instance: layout),
       Target.Of(typeof(SelfTrimmingPage), instance: pageB)]));

    Assert.Equal(2, router.Stack.Count);

    // Back to pageA — pageB's OnDeparted trims itself (forward) from the
    // stack; the only orphan is pageB (layout is shared with pageA).
    await shell.DispatchIntent(null, new BackIntent());

    Assert.Equal(1, router.Stack.Count);
    Assert.Same(pageA, router.Model!.CurrentChain?[^1].Instance);
    Assert.True(pageB.Released, "the trimmed page's orphan is released");
  }

  [Fact]
  public async Task CapsuleClose_ReleasesTheWholeChain_RunsNoDepartureCeremony()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var log = new List<(string Hook, Recorder Who)>();
    var layout = new Recorder(log);
    var content = new Recorder(log);

    IRouter present = router.Derive();
    await present.RouteAsync(Chain(layout, content));
    log.Clear();

    // A terminal close is not a transition: no departure pair fires — the
    // chain dies as a whole and the release drain closes its members out.
    await shell.DispatchIntent(null, new BackIntent("ok"));

    Assert.Equal(
      [("Release", content), ("Release", layout)],
      log);
    Assert.Equal("ok", await present.Completion!);
  }

  [Fact]
  public async Task RouteTurnover_KeepsLeavingChainInBackTrail_ReleasesOnlyWhenEvicted()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var log = new List<(string Hook, Recorder Who)>();
    var layout = new Recorder(log);
    var pageA = new Recorder(log);
    var pageB = new Recorder(log);
    var pageC = new Recorder(log);

    await router.RouteAsync(Chain(layout, pageA));
    log.Clear();
    await router.RouteAsync(Chain(layout, pageB));

    // Turnover: pageA departed but its chain stays in the back trail — the
    // stack still holds it, so Release never fires.
    Assert.Contains(log, e => e.Hook == "Departed" && ReferenceEquals(e.Who, pageA));
    Assert.DoesNotContain(log, e => e.Hook == "Release");

    // Back re-presents the SAME instance — a released participant would
    // arrive broken.
    log.Clear();
    Assert.Equal(IntentResult.Handled, await shell.DispatchIntent(null, new BackIntent()));
    Assert.Contains(log, e => e.Hook == "Arriving" && ReferenceEquals(e.Who, pageA));
    Assert.DoesNotContain(log, e => e.Hook == "Release");

    // A fresh route clears the forward trail — pageB's chain is evicted and
    // released; pageA (back trail) is still held.
    log.Clear();
    await router.RouteAsync(Chain(layout, pageC));
    Assert.Contains(log, e => e.Hook == "Release" && ReferenceEquals(e.Who, pageB));
    Assert.DoesNotContain(log, e => e.Hook == "Release" && ReferenceEquals(e.Who, pageA));

    // Closing the router evicts everything — pageA is released exactly once.
    log.Clear();
    router.CloseSync(null);
    Assert.Single(log, e => e.Hook == "Release" && ReferenceEquals(e.Who, pageA));
  }
}
