using Everlong.Nester.Layer;
using Everlong.Nester.Presentation;
using Everlong.Nester.Routing;
using Everlong.Nester.Shell;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Everlong.Nester.Tests.Routing;

/// <summary>
///   The convergence scene's cross-layer counterpart: a derived router's scene
///   carries the lease of the layer it was derived from, and the base router's
///   scene carries none.
/// </summary>
public class CounterpartScopeTests
{
  private static Request Single() => new(typeof(TestContent), null, [Target.Of(typeof(TestContent))]);

  [Fact]
  public async Task BaseRouter_Scene_HasNoCounterpart()
  {
    var shell = new FakeShell();
    IConvergenceContext? seen = null;
    var router = new TestRouter(shell, ctx => seen = ctx);

    await router.RouteAsync(Single());

    Assert.NotNull(seen);
    Assert.Null(seen!.Counterpart);
  }

  [Fact]
  public async Task DerivedRouter_Scene_CarriesTheDerivingLayer()
  {
    var shell = new FakeShell();
    var router = new TestRouter(shell);
    await router.RouteAsync(Single());
    ILayerLease baseLease = shell.Leases.Single();

    IConvergenceContext? derivedSeen = null;
    shell.RouterFactory = sp => new TestRouter(sp.GetRequiredService<IShell>(), sp, ctx => derivedSeen = ctx);

    IRouter overlay = router.Derive();
    await overlay.RouteAsync(Single());

    Assert.NotNull(derivedSeen);
    Assert.Same(baseLease, derivedSeen!.Counterpart);
  }

  [Fact]
  public async Task NestedOverlay_Scene_CarriesTheLayerItWasDerivedFrom()
  {
    var shell = new FakeShell();
    var router = new TestRouter(shell);
    await router.RouteAsync(Single());

    IRouter first = router.Derive();
    await first.RouteAsync(Single());
    ILayerLease firstLease = shell.Leases.First(l => ReferenceEquals((l.Content as IRoutingView)?.Router, first));

    IConvergenceContext? secondSeen = null;
    shell.RouterFactory = sp => new TestRouter(sp.GetRequiredService<IShell>(), sp, ctx => secondSeen = ctx);
    IRouter second = first.Derive();
    await second.RouteAsync(Single());

    Assert.Same(firstLease, secondSeen!.Counterpart);
  }
}
