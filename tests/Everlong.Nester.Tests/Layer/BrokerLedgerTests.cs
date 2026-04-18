using Everlong.Nester.Intent;
using Everlong.Nester.Layer;
using Xunit;

namespace Everlong.Nester.Tests.Layer;

/// <summary>
///   Contract tests for the z-ledger lease model: every acquire cuts a
///   fresh lease and carries its content (no empty-slot window), same-z
///   bands host multiple tenants on independent slots, the z is granted once
///   and never moves, placement resolves to the closest feasible position
///   inside the requested band, release is reference-only and idempotent,
///   and the intent order is z desc then grant desc.
/// </summary>
public class BrokerLedgerTests
{
  private static TestBrokerCore NewCore() => new();

  // ── Acquire: fresh lease per call, content included ──

  [Fact]
  public void Acquire_AlwaysCutsFreshLease()
  {
    var core = NewCore();
    var tenant = new LayerTestTenant();

    var first = core.Acquire(tenant, 100);
    var second = core.Acquire(tenant, 100);

    Assert.NotSame(first, second);
    Assert.Equal(2, core.BottomUp().Count(l => l.Z == 100));
  }

  [Fact]
  public void Acquire_SameZ_DifferentTenants_Coexist()
  {
    var core = NewCore();

    var first = core.Acquire(new LayerTestTenant(), 100);
    var second = core.Acquire(new LayerTestTenant(), 100);

    Assert.NotSame(first, second);
    Assert.Equal(100, first.Z);
    Assert.Equal(100, second.Z);
    Assert.True(first.IsActive && second.IsActive);
  }

  [Fact]
  public void Acquire_CarriesContent_TheSlotIsNeverEmpty()
  {
    var core = NewCore();
    var tenant = new LayerTestTenant();
    var content = new object();

    var lease = core.Acquire(tenant, content, 100);

    Assert.Same(content, lease.Content);
  }

  [Fact]
  public void Content_Swap_KeepsTheLease()
  {
    var core = NewCore();
    var tenant = new LayerTestTenant();
    var lease = core.Acquire(tenant, 100);
    var latest = new object();

    lease.Content = latest;

    Assert.Same(latest, lease.Content);
    Assert.True(lease.IsActive);
    Assert.Equal(100, lease.Z);
  }

  // ── Release: reference-only, idempotent, no holder gate ──

  [Fact]
  public void Release_RemovesLease_AndUnmounts()
  {
    var stage = new RecordingLayerStage();
    var core = new TestBrokerCore();
    core.Connect(stage);
    var lease = core.Acquire(new LayerTestTenant(), 100);

    lease.Release();

    Assert.False(lease.IsActive);
    Assert.Contains(lease, stage.Unmounted);
    Assert.Empty(core.BottomUp());
  }

  [Fact]
  public void Release_SecondTime_IsNoop()
  {
    var lease = NewCore().Acquire(new LayerTestTenant(), 100);

    lease.Release();
    lease.Release();

    Assert.False(lease.IsActive);
  }

  [Fact]
  public void Release_KeepsTheGrantedZ()
  {
    var lease = NewCore().Acquire(new LayerTestTenant(), 100);

    lease.Release();

    Assert.Equal(100, lease.Z);
  }

  [Fact]
  public void Release_DoesNotAffectCoTenants_OnSameZ()
  {
    var core = NewCore();
    var coTenant = new LayerTestTenant();
    var victim = core.Acquire(new LayerTestTenant(), 100);
    var coLease = core.Acquire(coTenant, coTenant.Content, 100);

    victim.Release();

    Assert.True(coLease.IsActive);
    Assert.Same(coTenant.Content, coLease.Content);
  }

  // ── Placement: the grant is the closest feasible z inside the band ──

  [Fact]
  public void Placement_At_LandsExactly()
  {
    var core = NewCore();

    var lease = core.Acquire(new LayerTestTenant(), new object(), 100);

    Assert.Equal(100, lease.Z);
  }

  [Fact]
  public void Placement_Bottom_TakesTheFloor()
  {
    var core = NewCore();
    core.Acquire(new LayerTestTenant(), new object(), KnownLayers.Dialog.Floor);

    var lease = core.Acquire(new LayerTestTenant(), new object(), KnownLayers.Dialog, LayerPolicy.Floor);

    Assert.Equal(KnownLayers.Dialog.Floor, lease.Z);
  }

  [Fact]
  public void Placement_Ceiling_TakesTheCeiling()
  {
    var core = NewCore();

    var lease = core.Acquire(new LayerTestTenant(), new object(), KnownLayers.Floating, LayerPolicy.Ceiling);

    Assert.Equal(KnownLayers.Floating.Ceiling, lease.Z);
  }

  [Fact]
  public void Placement_Top_EmptyBand_TakesTheFloor()
  {
    var lease = NewCore().Acquire(new LayerTestTenant(), new object(), KnownLayers.Dialog, LayerPolicy.AboveHighest);

    Assert.Equal(KnownLayers.Dialog.Floor, lease.Z);
  }

  [Fact]
  public void Placement_Top_TakesOneAboveTheHighestLiveInTheBand()
  {
    var core = NewCore();
    var first = core.Acquire(new LayerTestTenant(), new object(), KnownLayers.Dialog, LayerPolicy.AboveHighest);

    var second = core.Acquire(new LayerTestTenant(), new object(), KnownLayers.Dialog, LayerPolicy.AboveHighest);

    Assert.Equal(KnownLayers.Dialog.Floor, first.Z);
    Assert.Equal(KnownLayers.Dialog.Floor + 1, second.Z);
  }

  [Fact]
  public void Placement_Top_IgnoresOtherBands()
  {
    var core = NewCore();
    core.Acquire(new LayerTestTenant(), new object(), KnownLayers.Notice.Floor);

    var lease = core.Acquire(new LayerTestTenant(), new object(), KnownLayers.Dialog, LayerPolicy.AboveHighest);

    Assert.Equal(KnownLayers.Dialog.Floor, lease.Z);
  }

  [Fact]
  public void Placement_Top_DoesNotReuseHoles()
  {
    var core = NewCore();
    var band = new LayerBand(10, 20);
    core.Acquire(new LayerTestTenant(), new object(), 10);
    core.Acquire(new LayerTestTenant(), new object(), 12);

    var lease = core.Acquire(new LayerTestTenant(), new object(), band, LayerPolicy.AboveHighest);

    Assert.Equal(13, lease.Z);
  }

  [Fact]
  public void Placement_Top_FullBand_SharesTheCeiling()
  {
    var core = NewCore();
    var band = new LayerBand(10, 12);

    var first = core.Acquire(new LayerTestTenant(), new object(), band, LayerPolicy.AboveHighest);
    var second = core.Acquire(new LayerTestTenant(), new object(), band, LayerPolicy.AboveHighest);
    var third = core.Acquire(new LayerTestTenant(), new object(), band, LayerPolicy.AboveHighest);
    var fourth = core.Acquire(new LayerTestTenant(), new object(), band, LayerPolicy.AboveHighest);

    Assert.Equal(new[] { 10, 11, 12, 12 }, new[] { first.Z, second.Z, third.Z, fourth.Z });
    Assert.All(new[] { first, second, third, fourth }, l => Assert.True(l.IsActive));
  }

  [Fact]
  public void Placement_Top_NeverLeavesTheBand()
  {
    var core = NewCore();
    var band = new LayerBand(10, 12);

    for (int i = 0; i < 6; i++)
    {
      var lease = core.Acquire(new LayerTestTenant(), new object(), band, LayerPolicy.AboveHighest);
      Assert.True(band.Contains(lease.Z));
    }
  }

  // ── Intent order ──

  [Fact]
  public void BottomUp_OrdersZDesc_ThenGrantDesc()
  {
    var core = NewCore();
    var ground1 = core.Acquire(new LayerTestTenant(), 0);
    var dlg1 = core.Acquire(new LayerTestTenant(), 100);
    var air1 = core.Acquire(new LayerTestTenant(), 200);
    var dlg2 = core.Acquire(new LayerTestTenant(), 100);

    var order = core.BottomUp().ToList();

    Assert.Equal(new[] { air1, dlg2, dlg1, ground1 }, order);
  }

  [Fact]
  public async Task Dispatch_Order_ZDesc_ThenGrantDesc()
  {
    var log = new List<string>();
    var core = NewCore();
    var a = new HandlerTenant("A", log);
    var b = new HandlerTenant("B", log);
    core.Acquire(a, 0).IntentHandler = a;
    core.Acquire(b, 100).IntentHandler = b;

    await core.TryDispatch(new IntentContext(new MarkerIntent(), null));

    Assert.Equal(new[] { "B", "A" }, log);
  }

  private sealed class HandlerTenant(string name, List<string> log) : ILayerTenant, IIntentHandler
  {
    public ValueTask OnEvictedAsync(ILayerLease lease) => ValueTask.CompletedTask;

    public async ValueTask HandleAsync(IntentContext context, IntentDelegate next)
    {
      log.Add(name);
      await next(context);
    }
  }

  private sealed class MarkerIntent : IIntent;

  // ── Mounting ──

  [Fact]
  public void Acquire_MountsSlot_IntoVisualTree()
  {
    var stage = new RecordingLayerStage();
    var core = new TestBrokerCore();
    core.Connect(stage);

    var lease = core.Acquire(new LayerTestTenant(), 100);

    Assert.Contains(lease, stage.Mounted);
  }

  [Fact]
  public void Connect_BackFills_LeasesGrantedBeforeConnection()
  {
    var core = new TestBrokerCore();
    var lease = core.Acquire(new LayerTestTenant(), 100);
    var stage = new RecordingLayerStage();

    core.Connect(stage);

    Assert.Contains(lease, stage.Mounted);
  }

  [Fact]
  public void Connect_SameStageTwice_DoesNotRemount()
  {
    var core = new TestBrokerCore();
    var stage = new RecordingLayerStage();
    core.Connect(stage);
    core.Acquire(new LayerTestTenant(), 100);

    core.Connect(stage);

    Assert.Single(stage.Mounted);
  }

  // ── Visibility: presentation only — hiding never touches the lease ──

  [Fact]
  public void IsVisible_HidesTheSlot_ButKeepsTheLeaseAlive()
  {
    var core = NewCore();
    var tenant = new LayerTestTenant();
    var lease = core.Acquire(tenant, tenant.Content, 100);

    Assert.True(lease.IsVisible);

    lease.IsVisible = false;

    Assert.False(lease.IsVisible);
    Assert.True(lease.IsActive);
    Assert.Same(tenant.Content, lease.Content);
    Assert.Equal(100, lease.Z);

    lease.IsVisible = true;

    Assert.True(lease.IsVisible);
  }
}
