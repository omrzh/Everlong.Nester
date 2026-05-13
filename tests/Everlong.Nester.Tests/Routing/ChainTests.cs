using Everlong.Nester.Routing;
using Xunit;

namespace Everlong.Nester.Tests.Routing;

public class ChainTests
{
  private static Request Req(params Type[] chain)
    => new(chain[^1], null, chain.Select(t => Target.Of(t)).ToList());

  private static Request ReqNode(params (Type, Args?)[] chain)
    => new(chain[^1].Item1, null, chain.Select(n => Target.Of(n.Item1, n.Item2)).ToList());

  [Fact]
  public async Task ChainDiff_SharedLayoutKept_ContentTurnsOver()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(Req(typeof(LayoutAlpha), typeof(PageAlpha)));
    object layout = router.Model!.CurrentChain![0].Instance;
    object pageA = router.Model.CurrentChain[1].Instance;

    await router.RouteAsync(Req(typeof(LayoutAlpha), typeof(PageBeta)));

    // The shared layout type is reused — never departed.
    Assert.Same(layout, router.Model.CurrentChain![0].Instance);
    Assert.False(((TestContent)layout).Departing);
    Assert.False(((TestContent)layout).Departed);
    Assert.False(((TestContent)layout).Released);
    // The departing content left the screen; its chain stays in the back trail —
    // held by the stack, released only when evicted.
    Assert.True(((TestContent)pageA).Departing);
    Assert.True(((TestContent)pageA).Departed);
    Assert.False(((TestContent)pageA).Released);
    object pageB = router.Model.CurrentChain[1].Instance;
    Assert.True(((TestContent)pageB).Arrived);
    Assert.Same(pageB, router.View.Location!.Instance);
    Assert.Contains(shell.Leases, lease => ReferenceEquals(lease.Content, router.View));
  }

  [Fact]
  public async Task ChainDiff_NewLayout_WholeChainTurnsOver()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(Req(typeof(LayoutAlpha), typeof(PageAlpha)));
    object layoutA = router.Model!.CurrentChain![0].Instance;
    object pageA = router.Model.CurrentChain[1].Instance;

    await router.RouteAsync(Req(typeof(LayoutBeta), typeof(PageAlpha)));

    Assert.True(((TestContent)layoutA).Departing);
    Assert.True(((TestContent)layoutA).Departed);
    Assert.True(((TestContent)pageA).Departing);
    Assert.True(((TestContent)pageA).Departed);
    // The departed chain is retained in the back trail — released only on eviction.
    Assert.False(((TestContent)pageA).Released);
  }

  [Fact]
  public async Task RouteToLayoutWithoutPage_ThenLoadsPage()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();

    // Route to a layout alone — the layout end-of-chain is presented, no page.
    await router.RouteAsync(Req(typeof(LayoutAlpha)));
    object layout = router.Model!.CurrentChain!.Single().Instance;
    Assert.Same(layout, router.Model.Current);
    Assert.Equal(1, router.Stack.Count);
    Assert.Same(layout, router.View.Location!.Instance);
    Assert.Contains(shell.Leases, lease => ReferenceEquals(lease.Content, router.View));

    // Route to the same layout plus a page — the layout is shared (reused).
    await router.RouteAsync(Req(typeof(LayoutAlpha), typeof(PageAlpha)));
    Assert.Same(layout, router.Model.CurrentChain![0].Instance);
    object page = router.Model.Current!;
    Assert.True(((TestContent)page).Arrived);
    Assert.Equal(2, router.Stack.Count);
  }
}
