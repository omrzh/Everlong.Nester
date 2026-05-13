using Everlong.Nester.Routing;
using Xunit;

using Everlong.Nester.Intent;
namespace Everlong.Nester.Tests.Routing;

/// <summary>A chain participant recording its body-change notifications.</summary>
internal class AwareNode : TestContent, IBodyChanged
{
  /// <summary>The body chains received — one entry per notification.</summary>
  public List<IReadOnlyList<object>> BodyCalls { get; } = [];

  public void OnBodyChanged(IReadOnlyList<object> bodyChain) => BodyCalls.Add(bodyChain);
}

/// <summary>Distinct aware types for exercising body-chain diffs.</summary>
internal sealed class AwareA : AwareNode;

internal sealed class AwareB : AwareNode;

internal sealed class AwareC : AwareNode;

internal sealed class AwareD : AwareNode;

internal sealed class AwareE : AwareNode;

/// <summary>
///   The body-change notification contract: a participant whose body — the
///   chain below it — differs from the previously presented chain is
///   notified with its current body; a fresh participant is always
///   notified (its body is new by definition).
/// </summary>
public class BodyChangedTests
{
  private static Request ChainOf(params Type[] types)
    => new(types[^1], null, [.. types.Select(t => Target.Of(t))]);

  private static AwareNode Aware(TestRouter router, int index)
    => (AwareNode)router.Model!.CurrentChain![index].Instance;

  [Fact]
  public async Task InitialChain_NotifiesEveryAwareParticipant()
  {
    (_, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(ChainOf(typeof(AwareA), typeof(AwareB)));

    Assert.Single(Aware(router, 0).BodyCalls);
    Assert.Equal(typeof(AwareB), Assert.Single(Aware(router, 0).BodyCalls[0]).GetType());
    Assert.Single(Aware(router, 1).BodyCalls);
    Assert.Empty(Aware(router, 1).BodyCalls[0]);
  }

  [Fact]
  public async Task DeeperNesting_NotifiesEveryLevelWhoseBodyChanged()
  {
    (_, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(ChainOf(typeof(AwareA), typeof(AwareB)));
    await router.RouteAsync(ChainOf(typeof(AwareA), typeof(AwareB), typeof(AwareC)));

    // A: [B] → [B, C]; B: [] → [C]; C fresh → [].
    Assert.Equal([typeof(AwareB)], TypesOf(Aware(router, 0).BodyCalls[0]));
    Assert.Equal([typeof(AwareB), typeof(AwareC)], TypesOf(Aware(router, 0).BodyCalls[1]));
    Assert.Equal([], TypesOf(Aware(router, 1).BodyCalls[0]));
    Assert.Equal([typeof(AwareC)], TypesOf(Aware(router, 1).BodyCalls[1]));
    Assert.Equal([], TypesOf(Aware(router, 2).BodyCalls[0]));
  }

  [Fact]
  public async Task ReplacedChildren_NotifyTheSurvivorAndTheFresh()
  {
    (_, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(ChainOf(typeof(AwareA), typeof(AwareB), typeof(AwareC)));
    await router.RouteAsync(ChainOf(typeof(AwareA), typeof(AwareD), typeof(AwareE)));

    // A survives with a different body; B/C leave the chain — no extra calls.
    Assert.Equal(2, Aware(router, 0).BodyCalls.Count);
    Assert.Equal([typeof(AwareB), typeof(AwareC)], TypesOf(Aware(router, 0).BodyCalls[0]));
    Assert.Equal([typeof(AwareD), typeof(AwareE)], TypesOf(Aware(router, 0).BodyCalls[1]));
    Assert.Single(Aware(router, 1).BodyCalls);   // D: [E]
    Assert.Equal([typeof(AwareE)], TypesOf(Aware(router, 1).BodyCalls[0]));
    Assert.Single(Aware(router, 2).BodyCalls);   // E: []
  }

  [Fact]
  public async Task IdenticalChainRefresh_DoesNotNotify()
  {
    (_, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(ChainOf(typeof(AwareA), typeof(AwareB)));
    await router.RouteAsync(ChainOf(typeof(AwareA), typeof(AwareB)));

    Assert.Single(Aware(router, 0).BodyCalls);
    Assert.Single(Aware(router, 1).BodyCalls);
  }

  [Fact]
  public async Task RemovingTheBody_NotifiesWithEmptyChain()
  {
    (_, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(ChainOf(typeof(AwareA), typeof(AwareB)));
    await router.RouteAsync(ChainOf(typeof(AwareA)));

    Assert.Equal(2, Aware(router, 0).BodyCalls.Count);
    Assert.Equal([typeof(AwareB)], TypesOf(Aware(router, 0).BodyCalls[0]));
    Assert.Empty(Aware(router, 0).BodyCalls[1]);
  }

  [Fact]
  public async Task FullChainReplacement_NotifiesEveryFreshParticipant()
  {
    (_, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(ChainOf(typeof(AwareA), typeof(AwareB), typeof(AwareC)));
    var a = (AwareA)router.Model!.CurrentChain![0].Instance;
    var b = (AwareB)router.Model!.CurrentChain![1].Instance;
    var c = (AwareC)router.Model!.CurrentChain![2].Instance;

    // A/B/C leave the chain entirely — no extra calls; D/E are fresh.
    await router.RouteAsync(ChainOf(typeof(AwareD), typeof(AwareE)));

    Assert.Single(a.BodyCalls);
    Assert.Single(b.BodyCalls);
    Assert.Single(c.BodyCalls);
    Assert.Single(Aware(router, 0).BodyCalls);
    Assert.Equal([typeof(AwareE)], TypesOf(Aware(router, 0).BodyCalls[0]));
    Assert.Single(Aware(router, 1).BodyCalls);
  }

  [Fact]
  public async Task Back_NotifiesTheSurvivingLayout()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(ChainOf(typeof(AwareA), typeof(AwareB)));
    await router.RouteAsync(ChainOf(typeof(AwareA), typeof(AwareC)));
    var a = (AwareA)router.Model!.CurrentChain![0].Instance;

    Assert.Equal(IntentResult.Handled, await shell.DispatchIntent(null, new BackIntent()));

    // The restored page changed the surviving layout's body: [C] → [B].
    Assert.Equal(3, a.BodyCalls.Count);
    Assert.Equal([typeof(AwareB)], TypesOf(a.BodyCalls[2]));
  }

  [Fact]
  public async Task BackThenRefresh_DoesNotNotifyTwice()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(ChainOf(typeof(AwareA), typeof(AwareB)));
    await router.RouteAsync(ChainOf(typeof(AwareA), typeof(AwareC)));
    var a = (AwareA)router.Model!.CurrentChain![0].Instance;

    Assert.Equal(IntentResult.Handled, await shell.DispatchIntent(null, new BackIntent()));
    Assert.Equal(IntentResult.Handled, await shell.DispatchIntent(null, new RefreshIntent()));

    // Back notified [B]; the refresh replays the same chain — silent.
    Assert.Equal(3, a.BodyCalls.Count);
  }

  /// <summary>A layout recording only its truth notification.</summary>
  private sealed class OrderLayout : TestContent, IBodyChanged
  {
    private readonly List<string> _log;

    internal OrderLayout(List<string> log) => _log = log;

    public void OnBodyChanged(IReadOnlyList<object> bodyChain) => _log.Add("Body");
  }

  /// <summary>A page recording only its departure ceremony.</summary>
  private sealed class OrderPage : TestContent, IDeparting, IDeparted
  {
    private readonly List<string> _log;

    internal OrderPage(List<string> log) => _log = log;

    public new void OnDeparting(IRoutingContext context) => _log.Add("Departing");

    public new void OnDeparted(IRoutingContext context) => _log.Add("Departed");
  }

  private static Request ChainOfInstances(params TestContent[] chain)
    => new(chain[^1].GetType(), null, [.. chain.Select(n => Target.Of(n.GetType(), instance: n))]);

  [Fact]
  public async Task BodyChanged_RunsAtCommit_BeforeTheDepartureCeremony()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var log = new List<string>();
    var layout = new OrderLayout(log);
    var pageA = new OrderPage(log);
    var pageB = new OrderPage(log);

    await router.RouteAsync(ChainOfInstances(layout, pageA));
    log.Clear();
    await router.RouteAsync(ChainOfInstances(layout, pageB));

    // Truth first: the layout learns the new body at the commit — before the
    // leaving page departs.
    Assert.Equal(["Body", "Departing", "Departed"], log);

    // Back replays the same order: the truth settles at the cursor move,
    // then the leaving page departs.
    log.Clear();
    Assert.Equal(IntentResult.Handled, await shell.DispatchIntent(null, new BackIntent()));
    Assert.Equal(["Body", "Departing", "Departed"], log);
  }

  private static Type[] TypesOf(IReadOnlyList<object> chain) => [.. chain.Select(n => n.GetType())];
}
