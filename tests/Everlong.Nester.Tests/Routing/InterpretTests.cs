using Everlong.Nester.Shell;
using Microsoft.Extensions.DependencyInjection;
using Everlong.Nester.Routing;
using Xunit;

using Everlong.Nester.Intent;
namespace Everlong.Nester.Tests.Routing;

/// <summary>
///   The sync pipeline's user stage — the single RouterBase-subclass hook:
///   rewriting the request (interpretation) or short-circuiting it (veto).
/// </summary>
public class InterpretTests
{
  /// <summary>A router whose sync hook wraps the target in a layout shell — interpretation.</summary>
  private sealed class LayoutWrappingRouter : TestRouter
  {
    internal LayoutWrappingRouter(IShell shell) : base(shell) { }

    protected override void HandleRouteRequest(TransactionContext ctx, TransactNext next)
    {
      if (ctx.Direction == RoutingDirection.Route && ctx.Locator is { } location)
        ctx.UpdateLocator(new Locator([Target.Of(typeof(LayoutAlpha)), .. location.Path]));
      next(ctx);
    }
  }

  /// <summary>A router whose sync hook vetoes every request — interception.</summary>
  private sealed class VetoingRouter : TestRouter
  {
    internal VetoingRouter(IShell shell) : base(shell) { }

    internal VetoingRouter(IShell shell, IServiceProvider services) : base(shell, services) { }

    protected override void HandleRouteRequest(TransactionContext ctx, TransactNext next)
    {
      // No next — the request is short-circuited.
    }
  }

  [Fact]
  public async Task Interpret_WrapsTargetInLayoutShell()
  {
    var shell = new FakeShell();
    var router = new LayoutWrappingRouter(shell);

    // The caller only says "go to PageAlpha" — the interpret hook wraps it.
    await router.RouteAsync(new Request(typeof(PageAlpha), null));

    Assert.Equal(2, router.Model!.CurrentChain!.Length);
    Assert.Equal(typeof(LayoutAlpha), router.Model.CurrentChain[0].Type);
    Assert.Equal(typeof(PageAlpha), router.Model.CurrentChain[1].Type);
    Assert.Same(router.Model.CurrentChain[1].Instance, router.View.Location!.Instance);
  }

  [Fact]
  public async Task Interpret_RunsForFloatsToo()
  {
    var shell = new FakeShell();
    var router = new LayoutWrappingRouter(shell);

    IRouter result = router.Derive();
    await result.RouteAsync(new Request(typeof(PageAlpha), null));
    Assert.Equal(IntentResult.Handled, await shell.DispatchIntent(null, new BackIntent("ok")));
    Assert.Equal("ok", await result.Completion!.Result);
  }

  [Fact]
  public async Task Veto_GroundRoute_IsConsumedSilently()
  {
    var shell = new FakeShell();
    var router = new VetoingRouter(shell);

    await router.RouteAsync(new Request(typeof(PageAlpha), null));

    Assert.Null(router.Model!.Current);     // nothing committed
    Assert.Equal(0, router.Stack.Count);
  }

  [Fact]
  public async Task Veto_FloatRoute_SettlesThePresentation()
  {
    var shell = new FakeShell();
    var router = new TestRouter(shell);
    shell.RouterFactory = sp => new VetoingRouter(sp.GetRequiredService<IShell>(), sp);

    IRouter present = router.Derive();
    await present.RouteAsync(new Request(typeof(PageAlpha), null));

    // A vetoed first presentation settles — the creator never hangs.
    Assert.Null(await present.Completion!);
    Assert.Single(shell.Leases);            // the derived router's layer returned
  }
}
