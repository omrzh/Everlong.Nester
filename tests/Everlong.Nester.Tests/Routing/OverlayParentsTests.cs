using Everlong.Nester.Layer;
using Everlong.Nester.Routing;
using Xunit;

namespace Everlong.Nester.Tests.Routing;

/// <summary>
///   A derived router's parents — the default layouts every route is
///   completed with, prepended to the plan and shared across the overlay's
///   entries.
/// </summary>
public class OverlayParentsTests
{
  [Fact]
  public async Task Parents_ArePrepended_AndSharedAcrossEntries()
  {
    var shell = new FakeShell();
    var router = new TestRouter(shell);

    var layout = new TestContent();
    var first = new TestContent();
    var second = new TestContent();

    IRouter overlay = router.Derive(new DeriveOptions
    {
      Band = KnownLayers.Dialog,
      Parents = [Target.Of(typeof(TestContent), instance: layout)],
    });

    await overlay.RouteAsync(new Locator([Target.Of(typeof(TestContent), instance: first)]));

    // The parent is in front of the route's own target — the chain is the
    // overlay's completion of it.
    ILocation layoutNode = overlay.Stack.Location!.Trail[0];
    Assert.Same(layout, layoutNode.Instance);
    Assert.Equal(2, overlay.Stack.Location.Trail.Count);
    Assert.Same(first, overlay.Stack.Location.Trail[1].Instance);

    // A second route reuses the same layout node — the parent is the shared
    // prefix, never a fresh node per entry.
    await overlay.RouteAsync(new Locator([Target.Of(typeof(TestContent), instance: second)]));
    Assert.Same(layoutNode, overlay.Stack.Location!.Trail[0]);
    Assert.Same(second, overlay.Stack.Location.Trail[1].Instance);
    Assert.Equal(2, overlay.Stack.Count);
  }

  [Fact]
  public async Task WithoutParents_TheRouteStandsAlone()
  {
    var shell = new FakeShell();
    var router = new TestRouter(shell);

    IRouter overlay = router.Derive(new DeriveOptions { Band = KnownLayers.Dialog });
    await overlay.RouteAsync(new Locator(typeof(TestContent)));

    Assert.Single(overlay.Stack.Location!.Trail);
  }
}
