using Everlong.Nester.Routing;
using Xunit;

namespace Everlong.Nester.Tests.Routing;

/// <summary>
///   Direct tenancy — every derived router rents its layer directly
///   from the shell's broker; there is no intermediate ledger between.
/// </summary>
public class DirectTenancyTests
{
  private static Request Single(Type t) => new(t, null, [Target.Of(t)]);

  [Fact]
  public async Task EveryDerivedRouterIsADirectTenant_ShellSeesEachLease()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();

    await router.RouteAsync(Single(typeof(TestContent)));
    IRouter present = router.Derive();
    await present.RouteAsync(new Request(typeof(TestContent), null,
      [Target.Of(typeof(TestContent))]));

    // The shell's broker sees every derived router's lease — the ground and the
    // derived router are direct tenants (no intermediate ledger).
    Assert.Equal(2, shell.Leases.Count());

    await shell.DispatchIntent(null, new BackIntent());
    await present.Completion!;

    // The derived router closed; the shell's ledger is back to the ground alone.
    Assert.Single(shell.Leases);
  }
}
