using Everlong.Nester.Routing;
using Xunit;
using Everlong.Nester.Intent;
using Everlong.Nester.Presentation;
namespace Everlong.Nester.Tests.Routing;

public class NavigationViewTests
{
  private static Request ChainOf(Type t) => new(t, null, [Target.Of(t)]);

  [Fact]
  public async Task View_CarriesChain_AndIsInstalledInLayer()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();

    await router.RouteAsync(ChainOf(typeof(PageAlpha)));
    IRoutingView view = router.View;

    Assert.Single(view.Location!.Trail);
    Assert.Equal(typeof(PageAlpha), view.Location!.Trail[0].Type);
    Assert.Contains(shell.Leases, lease => ReferenceEquals(lease.Content, router.View));
    Assert.Equal(typeof(PageAlpha), view.Location!.Type);
  }

  [Fact]
  public async Task View_StatusFollowsCurrent()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();

    await router.RouteAsync(ChainOf(typeof(PageAlpha)));
    IRoutingView view = router.View;
    Assert.Equal(typeof(PageAlpha), view.Location!.Type);

    await router.RouteAsync(ChainOf(typeof(PageBeta)));
    Assert.Equal(typeof(PageBeta), view.Location!.Type);

    // Back restores the view's status to the previous current.
    Assert.Equal(IntentResult.Handled, await shell.DispatchIntent(null, new BackIntent()));
    Assert.Equal(typeof(PageAlpha), view.Location!.Type);
  }
}
