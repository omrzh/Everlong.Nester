using Everlong.Nester.Layer;
using Everlong.Nester.Tests.Layer;
using Everlong.Nester.Routing;
using Xunit;

using Everlong.Nester.Intent;
using Everlong.Nester.Presentation;
namespace Everlong.Nester.Tests.Routing;

public class ResultRouterTests
{
  [Fact]
  public async Task Derived_ExternalBack_ClosesWithResult()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var content = new TestContent();
    var request = new Request(typeof(TestContent), new TestArgs("hi"),
      [Target.Of(typeof(TestContent), new TestArgs("hi"), content)]);

    IRouter result = router.Derive();
    await result.RouteAsync(request);

    IntentResult handled = await shell.DispatchIntent(null, new BackIntent("ok"));

    Assert.Equal(IntentResult.Handled, handled);
    Assert.Equal("ok", await result.Completion!.Result);
    Assert.Equal("hi", Assert.IsType<TestArgs>(content.ReceivedArgs).Value);
    Assert.True(content.Arrived);
    // The external back on the last derived router entry is a terminal close — it
    // runs no departure ceremony; the drain releases the content.
    Assert.False(content.Departing);
    Assert.False(content.Departed);
    Assert.True(content.Released);
  }

  [Fact]
  public async Task Derived_Mandatory_VetoesExternalCloseButNotOwn()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var content = new TestContent { Mandatory = true };
    var request = new Request(typeof(TestContent), null,
      [Target.Of(typeof(TestContent), instance: content)]);

    IRouter result = router.Derive();
    await result.RouteAsync(request);

    // An external close is vetoed — the layer stays open.
    IntentResult handled = await shell.DispatchIntent(null, new BackIntent(null));
    Assert.Equal(IntentResult.Vetoed, handled);
    Assert.False(result.Completion!.Result.IsCompleted);

    // The content's own close (sender = itself) passes the veto.
    IntentResult ownHandled = await shell.DispatchIntent(content, new BackIntent("done"));
    Assert.Equal(IntentResult.Handled, ownHandled);
    Assert.Equal("done", await result.Completion!.Result);
  }

  [Fact]
  public async Task Derived_ResolvesTargetFromServiceProvider()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    shell.Provider.Register<TestContent>(new TestContent());
    // Instance is null — the router resolves TestContent from the shell scope.
    var request = new Request(typeof(TestContent), null);

    IRouter result = router.Derive();
    await result.RouteAsync(request);
    await shell.DispatchIntent(null, new BackIntent("resolved"));

    Assert.Equal("resolved", await result.Completion!.Result);
  }

  [Fact]
  public async Task Derived_Chain_OutermostRevealed_GuardIsContent()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var content = new TestContent();
    var request = new Request(typeof(PageAlpha), null,
      [Target.Of(typeof(LayoutAlpha)), Target.Of(typeof(PageAlpha), instance: content)]);

    IRouter result = router.Derive();
    await result.RouteAsync(request);

    // The model's owned view is installed in the layer and publishes the
    // content view model's truth.
    ILayerLease derivedLease = shell.Leases.First(l => l is TestLease);
    var view = (IRoutingView)derivedLease.Content!;
    Assert.Same(content, view.Location!.Instance);
    Assert.NotNull(view.Location);
    var entry = view.Location!.Trail[0];
    TestContent layout = (TestContent)entry.Instance!;

    IntentResult handled = await shell.DispatchIntent(null!, new BackIntent("ok"));
    Assert.Equal(IntentResult.Handled, handled);
    Assert.Equal("ok", await result.Completion!.Result);

    // The whole presented chain dies with the terminal close — no
    // departure pair fires; the drain releases layout and content.
    Assert.False(layout.Departing);
    Assert.False(layout.Departed);
    Assert.True(layout.Released);
  }
}
