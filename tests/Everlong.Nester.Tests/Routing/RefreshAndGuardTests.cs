using Everlong.Nester.Routing;
using Xunit;

using Everlong.Nester.Intent;
namespace Everlong.Nester.Tests.Routing;

/// <summary>
///   The refresh contract (RefreshIntent replays the arrival over the current
///   chain) and the ground back guard (the leaving page's IIntentHandler gets
///   first refusal before the back traversal).
/// </summary>
public class RefreshAndGuardTests
{
  private static Request ChainOf(Type page, Args? args = null)
    => new(page, args, [Target.Of(page, args)]);

  [Fact]
  public async Task RefreshIntent_ReplaysArrival_WithoutNewEntry()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(ChainOf(typeof(TestContent)));
    var page = (TestContent)router.Model!.Current!;
    Assert.Equal(1, page.ArrivalCount);
    Assert.Equal(1, router.Stack.Count);

    Assert.Equal(IntentResult.Handled, await shell.DispatchIntent(null, new RefreshIntent()));
    Assert.Same(page, router.Model.Current);      // the same instance re-arrives
    Assert.Equal(2, page.ArrivalCount);           // the arrival replays in place
    Assert.Equal(1, router.Stack.Count);          // no new stack entry
  }

  [Fact]
  public async Task RefreshIntent_OnSingleEntry_IsConsumed()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(ChainOf(typeof(TestContent)));

    // A refresh with no history still answers (consumed, ground preserved).
    Assert.Equal(IntentResult.Handled, await shell.DispatchIntent(null, new RefreshIntent()));
    Assert.NotNull(router.Model!.Current);
  }

  [Fact]
  public async Task BackIntent_LeavingPageGuardVetoesTheTraversal()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var guard = new PageBeta { Mandatory = true };
    await router.RouteAsync(ChainOf(typeof(PageAlpha)));
    await router.RouteAsync(new Request(typeof(PageBeta), null,
      [Target.Of(typeof(PageBeta), instance: guard)]));
    Assert.Same(guard, router.Model!.Current);

    // The mandatory page vetoes the external back — it stays on stage.
    Assert.Equal(IntentResult.Vetoed, await shell.DispatchIntent(null, new BackIntent()));
    Assert.Same(guard, router.Model.Current);
  }

  [Fact]
  public async Task BackIntent_LeavingPageGuardAllowsTheTraversal()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var pass = new PageBeta { Mandatory = false };
    await router.RouteAsync(ChainOf(typeof(PageAlpha)));
    object alpha = router.Model!.Current!;
    await router.RouteAsync(new Request(typeof(PageBeta), null,
      [Target.Of(typeof(PageBeta), instance: pass)]));

    // A non-vetoing page guard lets the traversal through.
    Assert.Equal(IntentResult.Handled, await shell.DispatchIntent(null, new BackIntent()));
    Assert.NotSame(pass, router.Model.Current);
    Assert.Same(alpha, router.Model.Current);
  }

  [Fact]
  public async Task RefreshIntent_OnEmptyStack_LandsNothing()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    int navigated = 0;
    router.Stack.PropertyChanged += (_, e) =>
    {
      if (e.PropertyName == nameof(IRouterStack.Location))
        navigated++;
    };

    Assert.Equal(IntentResult.Handled, await shell.DispatchIntent(null, new RefreshIntent()));

    Assert.Equal(0, router.Stack.Count);
    Assert.Equal(0, navigated);   // an empty refresh lands nothing — no phantom navigation
  }

  /// <summary>A participant counting its membership edges and arrival replays.</summary>
  private sealed class EdgeProbe : IRoutable, IArrived
  {
    internal int RoutedTo { get; private set; }

    internal int RoutedFrom { get; private set; }

    internal int Arrived { get; private set; }

    void IRoutable.OnRoutedTo(IRoutingContext context) => RoutedTo++;

    void IRoutable.OnRoutedFrom(IRoutingContext context) => RoutedFrom++;

    public Task OnArrivedAsync(IRoutingContext context)
    {
      Arrived++;
      return Task.CompletedTask;
    }
  }

  [Fact]
  public async Task RefreshIntent_FiresNoMembershipEdges_ButReplaysTheArrival()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var page = new EdgeProbe();
    var request = new Request(typeof(EdgeProbe), null,
      [Target.Of(typeof(EdgeProbe), instance: page)]);

    await router.RouteAsync(request);
    await router.WaitIdleAsync();
    Assert.Equal(1, page.RoutedTo);   // the join edge fired on presentation
    Assert.Equal(1, page.Arrived);

    // A refresh replays the arrival in place — nobody joined or left, so
    // the membership edges stay silent while the ceremony re-runs.
    Assert.Equal(IntentResult.Handled, await shell.DispatchIntent(null, new RefreshIntent()));
    await router.WaitIdleAsync();

    Assert.Equal(1, page.RoutedTo);
    Assert.Equal(0, page.RoutedFrom);
    Assert.Equal(2, page.Arrived);   // the arrival ceremony replayed
  }
}
