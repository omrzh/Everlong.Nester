using Everlong.Nester.Intent;
using Everlong.Nester.Layer;
using Xunit;

namespace Everlong.Nester.Tests.Layer;

/// <summary>
///   Contract tests for the plane-ledger lease model: every acquire cuts a
///   fresh lease and carries its content (no empty-slot window), a
///   non-stacking plane hosts every tenant on its floor, a stacking plane
///   grants one above its highest live lease and never leaves the plane,
///   the z is granted once and never moves, release is reference-only and
///   idempotent, and the intent order is z desc then grant desc.
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

    var first = core.Acquire(tenant, LayerPlane.Notice);
    var second = core.Acquire(tenant, LayerPlane.Notice);

    Assert.NotSame(first, second);
    Assert.Equal(2, core.BottomUp().Count(l => l.Z == LayerPlanes.Range(LayerPlane.Notice).Floor));
  }

  [Fact]
  public void Acquire_NonStackingPlane_SameZ_DifferentTenants_Coexist()
  {
    var core = NewCore();

    var first = core.Acquire(new LayerTestTenant(), LayerPlane.Notice);
    var second = core.Acquire(new LayerTestTenant(), LayerPlane.Notice);

    Assert.NotSame(first, second);
    Assert.Equal(LayerPlanes.Range(LayerPlane.Notice).Floor, first.Z);
    Assert.Equal(LayerPlanes.Range(LayerPlane.Notice).Floor, second.Z);
    Assert.True(first.IsLive && second.IsLive);
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
    Assert.True(lease.IsLive);
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

    Assert.False(lease.IsLive);
    Assert.Contains(lease, stage.Unmounted);
    Assert.Empty(core.BottomUp());
  }

  [Fact]
  public void Release_SecondTime_IsNoop()
  {
    var lease = NewCore().Acquire(new LayerTestTenant(), 100);

    lease.Release();
    lease.Release();

    Assert.False(lease.IsLive);
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

    Assert.True(coLease.IsLive);
    Assert.Same(coTenant.Content, coLease.Content);
  }

  // ── Placement: the plane decides the grant ──

  [Fact]
  public void Placement_NonStackingPlane_LandsEveryLeaseOnTheFloor()
  {
    var core = NewCore();

    var lease = core.Acquire(new LayerTestTenant(), LayerPlane.Notice);

    Assert.Equal(LayerPlanes.Range(LayerPlane.Notice).Floor, lease.Z);
  }

  [Fact]
  public void Placement_StackingPlane_EmptyTakesTheFloor()
  {
    var lease = NewCore().Acquire(new LayerTestTenant(), LayerPlane.Overlay);

    Assert.Equal(LayerPlanes.Range(LayerPlane.Overlay).Floor, lease.Z);
  }

  [Fact]
  public void Placement_StackingPlane_TakesOneAboveTheHighestLive()
  {
    var core = NewCore();
    var first = core.Acquire(new LayerTestTenant(), LayerPlane.Overlay);

    var second = core.Acquire(new LayerTestTenant(), LayerPlane.Overlay);

    Assert.Equal(LayerPlanes.Range(LayerPlane.Overlay).Floor, first.Z);
    Assert.Equal(LayerPlanes.Range(LayerPlane.Overlay).Floor + 1, second.Z);
  }

  [Fact]
  public void Placement_StackingPlane_IgnoresOtherPlanes()
  {
    var core = NewCore();
    core.Acquire(new LayerTestTenant(), LayerPlane.Notice);

    var lease = core.Acquire(new LayerTestTenant(), LayerPlane.Overlay);

    Assert.Equal(LayerPlanes.Range(LayerPlane.Overlay).Floor, lease.Z);
  }

  [Fact]
  public void Placement_StackingPlane_DoesNotReuseHoles()
  {
    var core = NewCore();
    var range = LayerPlanes.Range(LayerPlane.Overlay);
    var floor = core.Acquire(new LayerTestTenant(), LayerPlane.Overlay);
    var raised = core.Acquire(new LayerTestTenant(), LayerPlane.Overlay);
    floor.Release();

    var lease = core.Acquire(new LayerTestTenant(), LayerPlane.Overlay);

    Assert.Equal(range.Floor + 2, lease.Z);
    Assert.Equal(range.Floor, floor.Z);
    Assert.Equal(range.Floor + 1, raised.Z);
  }

  [Fact]
  public void Placement_StackingPlane_FullPlane_SharesTheCeiling()
  {
    var core = NewCore();
    var range = LayerPlanes.Range(LayerPlane.Dock);

    ILayerLease? last = null;
    for (int i = 0; i <= range.Ceiling - range.Floor + 1; i++)
      last = core.Acquire(new LayerTestTenant(), LayerPlane.Dock);

    Assert.Equal(range.Ceiling, last!.Z);
  }

  [Fact]
  public void Placement_NeverLeavesThePlane()
  {
    var core = NewCore();
    var range = LayerPlanes.Range(LayerPlane.Overlay);

    for (int i = 0; i < 8; i++)
    {
      var lease = core.Acquire(new LayerTestTenant(), LayerPlane.Overlay);
      Assert.InRange(lease.Z, range.Floor, range.Ceiling);
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
    Assert.True(lease.IsLive);
    Assert.Same(tenant.Content, lease.Content);
    Assert.Equal(100, lease.Z);

    lease.IsVisible = true;

    Assert.True(lease.IsVisible);
  }
}
