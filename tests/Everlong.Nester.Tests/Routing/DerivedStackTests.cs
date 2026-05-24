using Everlong.Nester.ComponentModel;
using Everlong.Nester.Dialog;
using Everlong.Nester.Routing;
using Everlong.Nester.Shell;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using Everlong.Nester.Intent;
namespace Everlong.Nester.Tests.Routing;

/// <summary>
///   A derived router holds its own history: a route pushes, back pops, and a root back
///   closes it and settles its result.
/// </summary>
public class DerivedStackTests
{
  private sealed class TestDialog : DialogSessionBase<object?>
  {
  }

  // ── derived lifecycle: push / back / root back closes ─────────────────────────────

  [Fact]
  public async Task Derived_RouteIntent_Pushes_BackTraverses_RootBackCloses()
  {
    var shell = new FakeShell();
    var router = new TestRouter(shell);

    var first = new TestContent();
    IRouter result = router.Derive();
    await result.RouteAsync(
      new Request(typeof(TestContent), null, [Target.Of(typeof(TestContent), instance: first)]));

    Assert.False(result.Stack.CanGoBack);            // one entry — no history yet
    Assert.Same(first, result.Stack.Location!.Trail[0].Instance);

    // A route command consumed by the derived router pushes a second entry (in-router nav).
    var second = new TestContent();
    IntentResult routed = await shell.DispatchIntent(null,
      new RouteIntent(new Locator([Target.Of(typeof(TestContent), instance: second)])));
    Assert.Equal(IntentResult.Handled, routed);
    Assert.True(result.Stack.CanGoBack);
    Assert.Same(second, result.Stack.Location!.Trail[0].Instance);
    Assert.False(result.Completion!.Result.IsCompleted);

    // Back pops the stack back to the first entry — the derived router stays open.
    IntentResult backHandled = await shell.DispatchIntent(null, new BackIntent());
    Assert.Equal(IntentResult.Handled, backHandled);
    Assert.False(result.Stack.CanGoBack);
    Assert.Same(first, result.Stack.Location!.Trail[0].Instance);
    Assert.False(result.Completion!.Result.IsCompleted);

    // A root back closes the derived router — the result settles with the back value.
    IntentResult rootBack = await shell.DispatchIntent(null, new BackIntent("done"));
    Assert.Equal(IntentResult.Handled, rootBack);
    Assert.Equal("done", await result.Completion!.Result);
    Assert.True(result.Completion!.Result.IsCompleted);
  }

  // ── ground route has no completion ─────────────────────────────────────────

  [Fact]
  public async Task Route_HasNoCompletion()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();

    await router.RouteAsync(new Request(typeof(PageAlpha), null));

    var content = (TestContent)router.Model!.Current!;
    Assert.Null(content.SeenCompletion);
  }

  // ── derived chains ride the derived router untouched ──────────────────────────────────

  [Fact]
  public async Task Derived_BareChain_TrustedAsGiven()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var session = new TestDialog();

    IRouter result = router.Derive();
    await result.RouteAsync(
      new Request(typeof(TestDialog), null, [Target.Of(typeof(TestDialog), instance: session)]));

    // A bare session chain rides the derived router untouched — the default dimmer
    // is the Dialog domain's business (built by ShowAsync), not the
    // router's.
    Assert.Single(result.Stack.Location!.Trail);
    Assert.Same(session, result.Stack.Location!.Trail[0].Instance);
  }

  [Fact]
  public async Task Derived_ExplicitChain_TrustedAsGiven()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var session = new TestDialog();

    IRouter result = router.Derive();
    await result.RouteAsync(new Request(typeof(TestDialog), null,
      [Target.Of(typeof(DefaultDimmerModel)), Target.Of(typeof(TestDialog), instance: session)]));

    // The explicit chain rides the derived router untouched.
    Assert.Equal(2, result.Stack.Location!.Trail.Count);
    Assert.Equal(typeof(DefaultDimmerModel), result.Stack.Location!.Trail[0].Type);
    Assert.Same(session, result.Stack.Location!.Trail[1].Instance);
  }

  // ── the closing window — a route landing mid-close is rejected ─────────────

  [Fact]
  public async Task RouteAsync_WhileClosing_IsSwallowed()
  {
    var gate = new TaskCompletionSource();
    var shell = new FakeShell();
    // The reveal yields only for the close truth — route runs pass straight
    // through, the close ceremony suspends on the gate.  The factory ships
    // the same reveal to every router the scopes resolve.
    Func<IConvergenceScene, Task> reveal =
      ctx => ctx.Direction == RoutingDirection.Close ? gate.Task : Task.CompletedTask;
    shell.RouterFactory = sp => new TestRouter(sp.GetRequiredService<IShell>(), sp, reveal);
    var router = new TestRouter(shell, reveal);

    var first = new TestContent();
    TestRouter result = (TestRouter)router.Derive();
    await result.RouteAsync(
      new Request(typeof(TestContent), null, [Target.Of(typeof(TestContent), instance: first)]));
    await result.WaitIdleAsync();
    Assert.Single(result.Stack.Location!.Trail);

    result.Completion!.Complete("ok");   // the close ceremony suspends on the reveal gate

    // A route landing mid-close must not ghost-arrive on the departing
    // stack — the closing window rejects it like the closed state.
    await result.RouteAsync(new Request(typeof(TestContent), null,
      [Target.Of(typeof(TestContent), instance: new TestContent())]));

    Assert.Single(result.Stack.Location!.Trail);   // nothing pushed, no arrival
    Assert.False(result.Completion!.Result.IsCompleted);    // the close is still in flight

    gate.SetResult();
    Assert.Equal("ok", await result.Completion!);
  }
}
