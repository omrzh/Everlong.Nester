using Everlong.Nester.Intent;
using Everlong.Nester.Routing;
using Everlong.Nester.Shell;
using Xunit;

namespace Everlong.Nester.Tests.Routing;

/// <summary>A page that vetoes the shell's close probe.</summary>
internal sealed class VetoClosePage : IIntentHandler
{
  public ValueTask HandleAsync(IntentContext context, IntentDelegate next)
  {
    if (context.Intent is TryCloseIntent)
    {
      context.Veto();
      return ValueTask.CompletedTask;
    }
    return next(context);
  }
}

/// <summary>A page that vetoes route-request navigation.</summary>
internal sealed class VetoRoutePage : IIntentHandler
{
  public ValueTask HandleAsync(IntentContext context, IntentDelegate next)
  {
    if (context.Intent is RouteIntent)
    {
      context.Veto();
      return ValueTask.CompletedTask;
    }
    return next(context);
  }
}

/// <summary>
///   The intent-chain consultation: the router answers every intent by first
///   asking its domain content — the ground's presented page implements
///   <see cref="IIntentHandler"/> to veto — then interprets its own domain
///   commands (<see cref="IRouteIntent"/>); anything else falls through the
///   shell's chain.
/// </summary>
public class IntentChainTests
{
  private static Request ChainOf(Type page)
    => new(page, null, [Target.Of(page)]);

  [Fact]
  public async Task GroundPage_VetoesTheShellCloseProbe()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(ChainOf(typeof(VetoClosePage)));

    // The shell probes a close — the page vetoes it.
    Assert.Equal(IntentResult.Vetoed, await shell.DispatchIntent(null, new TryCloseIntent()));
    Assert.IsType<VetoClosePage>(router.Model!.Current);
  }

  [Fact]
  public async Task GroundPage_VetoesRouteIntent()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(ChainOf(typeof(VetoRoutePage)));

    // A route request as an intent (the UI gesture path) is subject to the
    // page's veto — the navigation does not run.
    Assert.Equal(IntentResult.Vetoed, await shell.DispatchIntent(null, new RouteIntent(new Locator(typeof(PageAlpha)))));
    Assert.IsType<VetoRoutePage>(router.Model!.Current);
  }

  [Fact]
  public async Task RouteIntent_PassesThroughNonVetoingPage()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(ChainOf(typeof(PageAlpha)));

    // A non-vetoing page answers false to the route command — the router navigates.
    Assert.Equal(IntentResult.Handled, await shell.DispatchIntent(null, new RouteIntent(new Locator(typeof(PageBeta)))));
    Assert.IsType<PageBeta>(router.Model!.Current);
  }

  [Fact]
  public async Task NonRoutingIntent_UnansweredByThePage_FallsThrough()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(ChainOf(typeof(PageAlpha)));

    // An unknown intent reaches the router, the page declines, and the
    // router (not a routing command) answers Pass — the shell chain moves on.
    Assert.Equal(IntentResult.Pass, await shell.DispatchIntent(null, new ShowIntent()));
  }
}
