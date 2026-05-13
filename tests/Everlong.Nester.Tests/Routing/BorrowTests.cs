using Everlong.Nester.Shell;
using Microsoft.Extensions.DependencyInjection;
using Everlong.Nester.Routing;
using Xunit;

namespace Everlong.Nester.Tests.Routing;

/// <summary>
///   The borrowed environment — a derived router's ceremonies observe the
///   presenting router's presented site, captured at the derivation.
/// </summary>
public class BorrowTests
{
  [Fact]
  public async Task DerivedCeremony_ObservesTheBorrowedEnvironment()
  {
    IConvergenceContext? overlay = null;
    var shell = new FakeShell();
    shell.RouterFactory = sp => new TestRouter(sp.GetRequiredService<IShell>(), sp,
      ctx =>
      {
        if (ctx.Borrowed is not null)
          overlay ??= ctx;
      });
    var router = new TestRouter(shell, ctx => Task.CompletedTask);

    await router.RouteAsync(new Request(typeof(PageAlpha), null,
      [Target.Of(typeof(LayoutAlpha)), Target.Of(typeof(PageAlpha))]));
    Location[] under = router.Model!.CurrentChain!;

    var session = new TestContent();
    var derived = (TestRouter)router.Derive();
    await derived.RouteAsync(new Request(typeof(TestContent), null,
      [Target.Of(typeof(TestContent), instance: session)]));
    await derived.WaitIdleAsync();

    // The maiden ceremony of the overlay carries the borrowed site — the
    // presenting chain's content terminal; the chain is its ancestor path.
    Assert.NotNull(overlay);
    Location? borrowed = overlay!.Borrowed;
    Assert.NotNull(borrowed);
    Assert.Same(under[1].Instance, borrowed.Instance);
    Assert.Same(under[0].Instance, borrowed.Parent!.Instance);
    Assert.True(overlay.IsElevated);
  }

  [Fact]
  public async Task GroundCeremony_BorrowsNothing()
  {
    IConvergenceContext? ground = null;
    var shell = new FakeShell();
    var router = new TestRouter(shell, ctx =>
    {
      if (ctx.Borrowed is null)
        ground ??= ctx;
    });

    await router.RouteAsync(new Request(typeof(PageAlpha), null,
      [Target.Of(typeof(LayoutAlpha)), Target.Of(typeof(PageAlpha))]));
    await router.WaitIdleAsync();

    Assert.NotNull(ground);
    Assert.Null(ground!.Borrowed);
    Assert.False(ground.IsElevated);
  }
}
