using Everlong.Nester.Presentation;
using Everlong.Nester.Intent;
using Everlong.Nester.Layer;
using Everlong.Nester.Shell;
using Xunit;
using Everlong.Nester.Tests.Hosting;
using Everlong.Nester.Tests.Layer;

namespace Everlong.Nester.Tests.Shell;

/// <summary>
///   Eviction semantics: an evicted lease is recycled outright — removed
///   from the ledger, unmounted, then its tenant notified via
///   <see cref="ILayerTenant.OnEvictedAsync" />.  Distinct from
///   <see cref="ILayerLease.Release" /> (tenant-initiated return).
///   Eviction is reference-only: evicting one lease never touches co-tenants
///   on the same z band.
/// </summary>
public sealed class EvictTests
{
  private sealed class Tenant : ILayerTenant
  {
    public int EvictedCalls { get; private set; }
    public ValueTask OnEvictedAsync(ILayerLease lease)
    {
      EvictedCalls++;
      return ValueTask.CompletedTask;
    }
  }

  private sealed class ThrowingTenant : ILayerTenant
  {
    public int EvictedCalls { get; private set; }
    public ValueTask OnEvictedAsync(ILayerLease lease)
    {
      EvictedCalls++;
      throw new InvalidOperationException("tenant cleanup failed");
    }
  }

  private sealed class RecordingTenant(string name, List<string> log) : ILayerTenant
  {
    public ValueTask OnEvictedAsync(ILayerLease lease)
    {
      log.Add(name);
      return ValueTask.CompletedTask;
    }
  }

  private static TestBrokerCore HeadlessCore() => new();

  [Fact]
  public void ConnectStage_BackFills_LeasesGrantedBeforeConnection()
  {
    // Leases granted before the visual stack is connected run in the virtual
    // mode (slots unmounted); ConnectStage back-fills them onto the panel.
    var panel = new StagePanel();
    var broker = new BareShell();
    broker.Acquire(new Tenant(), new object(), 100);

    ILayerLease lease = broker.LeaseOrder().Single();
    Assert.DoesNotContain(((ContentLayerLease)lease).Surface, panel.Children);   // virtual: not mounted yet

    broker.ConnectStage(panel);

    Assert.Contains(((ContentLayerLease)lease).Surface, panel.Children);         // back-filled on connection
  }

  [Fact]
  public void Evict_RemovesLease_AndUnmountsSlot()
  {
    var core = HeadlessCore();
    var lease = core.Acquire(new Tenant(), 100);

    Assert.True(core.Evict(lease));

    Assert.False(core.IsLive(lease));
    Assert.Empty(core.BottomUp());
  }

  [Fact]
  public void Evict_NotifiesTenant()
  {
    var core = HeadlessCore();
    var tenant = new Tenant();
    var lease = core.Acquire(tenant, 100);

    core.Evict(lease);

    Assert.Equal(1, tenant.EvictedCalls);
  }

  [Fact]
  public void Evict_SecondTime_IsNoop()
  {
    var core = HeadlessCore();
    var tenant = new Tenant();
    var lease = core.Acquire(tenant, 100);

    Assert.True(core.Evict(lease));
    Assert.False(core.Evict(lease));
    Assert.Equal(1, tenant.EvictedCalls);
  }

  [Fact]
  public void Evict_DoesNotAffectCoTenants_OnSameZ()
  {
    var core = HeadlessCore();
    var victim = new Tenant();
    var coTenant = new Tenant();
    var victimLease = core.Acquire(victim, 100);
    var coLease = core.Acquire(coTenant, 100);

    core.Evict(victimLease);

    Assert.True(core.IsLive(coLease));
    Assert.Equal(1, victim.EvictedCalls);
    Assert.Equal(0, coTenant.EvictedCalls);
  }

  // ── Intent dispatch (the lease's intent handler is the operator) ──

  private sealed class TestIntent : IIntent;

  private sealed class Operator : ILayerTenant, IIntentHandler
  {
    public int EvictedCalls { get; private set; }
    public int IntentCalls { get; private set; }
    public bool HandleResult { get; init; } = true;
    public List<Operator>? AskedOrder { get; init; }

    public ValueTask OnEvictedAsync(ILayerLease lease)
    {
      EvictedCalls++;
      return ValueTask.CompletedTask;
    }

    public ValueTask HandleAsync(IntentContext context, IntentDelegate next)
    {
      IntentCalls++;
      AskedOrder?.Add(this);
      if (HandleResult)
      {
        context.Handle();
        return ValueTask.CompletedTask;
      }
      return next(context);
    }
  }

  private static AvaloniaShell Broker() => new BareShell();

  /// <summary>Intent dispatch asks the lease's intent handler (first refusal, z-desc) — the operator answers for its slot.</summary>
  [Fact]
  public async Task Dispatch_IntentHitsTenant()
  {
    var stage = Broker();
    var op = new Operator();
    var lease = stage.Acquire(op, new object(), 100);
    lease.IntentHandler = op;

    Assert.True(await stage.DispatchToLayerLeases(null, new TestIntent()));
    Assert.Equal(1, op.IntentCalls);
  }

  /// <summary>Intent dispatch order: z desc, then latest grant first within the same z.</summary>
  [Fact]
  public async Task Dispatch_Order_ZDesc_ThenGrantDesc()
  {
    var stage = Broker();
    var asked = new List<Operator>();
    var low = new Operator { HandleResult = false, AskedOrder = asked };
    var first = new Operator { HandleResult = false, AskedOrder = asked };
    var second = new Operator { HandleResult = false, AskedOrder = asked };
    stage.Acquire(low, new object(), 0).IntentHandler = low;
    stage.Acquire(first, new object(), 100).IntentHandler = first;
    stage.Acquire(second, new object(), 100).IntentHandler = second;

    Assert.False(await stage.DispatchToLayerLeases(null, new TestIntent()));

    Assert.Equal(new[] { second, first, low }, asked);
  }

  // ── Shell teardown (the eviction cascade) ──

  [Fact]
  public async Task Dispose_EvictsTopmostFirst()
  {
    var shell = new BareShell();
    var log = new List<string>();
    shell.Acquire(new RecordingTenant("low", log), new object(), 0);
    shell.Acquire(new RecordingTenant("high", log), new object(), 100);

    await shell.DisposeAsync();

    Assert.Equal(new[] { "high", "low" }, log);
  }

  [Fact]
  public async Task Dispose_ContinuesTheCascade_WhenATenantThrows()
  {
    var shell = new BareShell();
    var broken = new ThrowingTenant();
    var healthy = new Tenant();
    shell.Acquire(healthy, new object(), 0);
    shell.Acquire(broken, new object(), 100);

    await shell.DisposeAsync();

    Assert.Equal(1, broken.EvictedCalls);
    Assert.Equal(1, healthy.EvictedCalls);
  }

  [Fact]
  public async Task Acquire_AfterDispose_Throws()
  {
    var shell = new BareShell();
    await shell.DisposeAsync();

    Assert.Throws<InvalidOperationException>(
      () => shell.Acquire(new Tenant(), new object(), 0));
  }
}
