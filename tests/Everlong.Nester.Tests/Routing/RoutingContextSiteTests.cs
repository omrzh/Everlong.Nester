using Everlong.Nester.Shell;
using Microsoft.Extensions.DependencyInjection;
using Everlong.Nester.Routing;
using Xunit;

namespace Everlong.Nester.Tests.Routing;

/// <summary>
///   Pins the two-ended routing context — the transition's departure and
///   arrival sites, read identically from the transaction and the
///   convergence phases.
/// </summary>
public class RoutingContextSiteTests
{
  /// <summary>A participant recording the context's sites at the membership and arrival edges.</summary>
  private sealed class Probe : IRoutable, IArrived
  {
    public ILocation? DepartureOnRoutedTo { get; private set; }

    public ILocation? ArrivalOnRoutedTo { get; private set; }

    public ILocation? DepartureOnRoutedFrom { get; private set; }

    public ILocation? DepartureOnArrived { get; private set; }

    public ILocation? ArrivalOnArrived { get; private set; }

    void IRoutable.OnRoutedTo(IRoutingContext context)
    {
      DepartureOnRoutedTo = context.Departure;
      ArrivalOnRoutedTo = context.Arrival;
    }

    void IRoutable.OnRoutedFrom(IRoutingContext context)
      => DepartureOnRoutedFrom = context.Departure;

    public Task OnArrivedAsync(IRoutingContext context)
    {
      DepartureOnArrived = context.Departure;
      ArrivalOnArrived = context.Arrival;
      return Task.CompletedTask;
    }
  }

  [Fact]
  public async Task FirstRoute_DepartsNothing_ArrivesAtItself()
  {
    var probe = new Probe();
    var (_, router) = RouterTestHost.Create();
    await router.RouteAsync(Instance(probe));
    await router.WaitIdleAsync();

    Assert.Null(probe.DepartureOnRoutedTo);
    Assert.Same(probe, probe.ArrivalOnRoutedTo!.Instance);
    Assert.Null(probe.DepartureOnArrived);
    Assert.Same(probe, probe.ArrivalOnArrived!.Instance);
  }

  [Fact]
  public async Task StructuralTurnover_DepartsThePreviousTerminal()
  {
    var first = new Probe();
    var second = new Probe();
    var (_, router) = RouterTestHost.Create();
    await router.RouteAsync(Instance(first));
    await router.RouteAsync(Instance(second));
    await router.WaitIdleAsync();

    // The entering member's edges — pre-commit and post-commit agree on the
    // departed terminal; the leaving member sees its own terminal.
    Assert.Same(first, second.DepartureOnRoutedTo!.Instance);
    Assert.Same(second, second.ArrivalOnRoutedTo!.Instance);
    Assert.Same(first, second.DepartureOnArrived!.Instance);
    Assert.Same(first, first.DepartureOnRoutedFrom!.Instance);
  }

  [Fact]
  public async Task Back_DepartsThePoppedSite_ArrivesAtTheReturnedSite()
  {
    var first = new Probe();
    var second = new Probe();
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(Instance(first));
    await router.RouteAsync(Instance(second));

    await shell.DispatchIntent(null, new BackIntent());
    await router.WaitIdleAsync();

    Assert.Same(second, first.DepartureOnRoutedTo!.Instance);
    Assert.Same(first, first.ArrivalOnRoutedTo!.Instance);
    Assert.Same(second, second.DepartureOnRoutedFrom!.Instance);
  }

  [Fact]
  public async Task Refresh_DepartsAndArrivesAtTheSameSite()
  {
    var probe = new Probe();
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(Instance(probe));

    await shell.DispatchIntent(null, new RefreshIntent());
    await router.WaitIdleAsync();

    Assert.Same(probe, probe.DepartureOnArrived!.Instance);
    Assert.Same(probe, probe.ArrivalOnArrived!.Instance);
  }

  [Fact]
  public async Task CapsuleClose_ArrivesNothing_DepartsTheClosingSite()
  {
    IConvergenceContext? close = null;
    var shell = new FakeShell();
    shell.RouterFactory = sp => new TestRouter(sp.GetRequiredService<IShell>(), sp, ctx =>
    {
      if (ctx.Direction == RoutingDirection.Close)
        close = ctx;
    });
    var router = new TestRouter(shell, ctx => Task.CompletedTask);

    var session = new TestContent();
    var derived = (TestRouter)router.Derive();
    await derived.RouteAsync(new Request(typeof(TestContent), null,
      [Target.Of(typeof(TestContent), instance: session)]));

    await shell.DispatchIntent(null, new BackIntent());
    await derived.WaitIdleAsync();

    // A close lands nothing — the closing site is the departure, never an arrival.
    Assert.NotNull(close);
    Assert.Null(close!.Arrival);
    Assert.Same(session, close.Departure!.Instance);
  }

  private static Request Instance(Probe probe)
    => new(typeof(Probe), null, [Target.Of(typeof(Probe), instance: probe)]);
}
