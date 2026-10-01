using Everlong.Nester.Layer;
using Xunit;

namespace Everlong.Nester.Tests.Layer;

/// <summary>
///   Contract tests for layer focus: the single foreground grant, the
///   election over eligible tenants, the accept/decline consultation and the
///   pre/post transfer hooks.
/// </summary>
public class LayerFocusTests
{
  private static TestBrokerCore NewCore() => new();

  [Fact]
  public void Focus_NotGranted_ToNonFocusableTenant()
  {
    var core = NewCore();

    core.Acquire(new LayerTestTenant(), LayerPlane.Overlay);

    Assert.Null(core.Focused);
  }

  [Fact]
  public void Focused_IsReadOnly_CannotBeEscalatedToAHandle()
  {
    var core = NewCore();

    core.Acquire(new FocusTenant("holder", []), LayerPlane.Overlay);

    Assert.NotNull(core.Focused);
    Assert.False(core.Focused is ILayerHandle);
  }

  [Fact]
  public void Focus_TopmostEligibleWins()
  {
    var core = NewCore();
    var log = new List<string>();
    var low = new FocusTenant("low", log);
    var high = new FocusTenant("high", log);

    var lowLease = core.Acquire(low, LayerPlane.Ground);
    var highLease = core.Acquire(high, LayerPlane.Overlay);

    Assert.Equal(highLease.Lease, core.Focused);
    Assert.NotEqual(lowLease.Lease, core.Focused);
  }

  [Fact]
  public void Focus_Transfers_OnHigherGrant_AndReturns_OnRelease()
  {
    var core = NewCore();
    var log = new List<string>();
    var low = new FocusTenant("low", log);
    var high = new FocusTenant("high", log);

    var lowLease = core.Acquire(low, LayerPlane.Overlay);
    log.Clear();
    var highLease = core.Acquire(high, LayerPlane.Overlay);

    Assert.Equal(highLease.Lease, core.Focused);
    Assert.Equal(new[] { "high:try:Granted", "low:unfocusing", "high:focusing", "low:unfocused", "high:focused" }, log);

    log.Clear();
    highLease.Release();

    Assert.Equal(lowLease.Lease, core.Focused);
    Assert.Equal(new[] { "high:unfocusing", "high:unfocused", "low:try:Departed", "low:focusing", "low:focused" }, log);
  }

  [Fact]
  public void Focus_DecliningCandidate_DefersToTheNext()
  {
    var core = NewCore();
    var log = new List<string>();
    var keeper = new FocusTenant("keeper", log);
    var decliner = new FocusTenant("decliner", log) { Accept = false };

    var keeperLease = core.Acquire(keeper, LayerPlane.Overlay);
    log.Clear();
    core.Acquire(decliner, LayerPlane.Overlay);

    Assert.Equal(keeperLease.Lease, core.Focused);
    Assert.Contains("decliner:try:Granted", log);
    Assert.DoesNotContain("keeper:unfocused", log);
  }

  [Fact]
  public void Focus_AllDeclining_YieldsNone()
  {
    var core = NewCore();
    var log = new List<string>();

    core.Acquire(new FocusTenant("a", log) { Accept = false }, LayerPlane.Overlay);
    core.Acquire(new FocusTenant("b", log) { Accept = false }, LayerPlane.Overlay);

    Assert.Null(core.Focused);
  }

  [Fact]
  public void Focus_RequestClaims_FromALowerPlane()
  {
    var core = NewCore();
    var log = new List<string>();
    var dock = new FocusTenant("dock", log);
    var overlay = new FocusTenant("overlay", log);

    var dockLease = core.Acquire(dock, LayerPlane.Dock);
    core.Acquire(overlay, LayerPlane.Overlay);
    log.Clear();

    core.RequestFocus(dockLease);

    Assert.Equal(dockLease.Lease, core.Focused);
    Assert.Contains("dock:focusing", log);
    Assert.Contains("dock:focused", log);
  }

  [Fact]
  public void Focus_RequestOnDeadLease_IsIgnored()
  {
    var core = NewCore();
    var log = new List<string>();
    var dock = new FocusTenant("dock", log);
    var overlay = new FocusTenant("overlay", log);

    var dockLease = core.Acquire(dock, LayerPlane.Dock);
    var overlayLease = core.Acquire(overlay, LayerPlane.Overlay);
    dockLease.Release();

    core.RequestFocus(dockLease);

    Assert.Equal(overlayLease.Lease, core.Focused);
  }

  /// <summary>A focusable tenant that records its consultations and can decline.</summary>
  private sealed class FocusTenant(string name, List<string> log) : ILayerTenant, IFocusableLayer
  {
    public bool Accept { get; init; } = true;

    public ValueTask OnEvictedAsync(ILayerLease lease) => ValueTask.CompletedTask;

    public bool TryFocus(LayerFocusContext context)
    {
      log.Add($"{name}:try:{context.Cause}");
      return Accept;
    }

    public void OnFocusing(LayerFocusContext context) => log.Add($"{name}:focusing");

    public void OnFocused(LayerFocusContext context) => log.Add($"{name}:focused");

    public void OnUnfocusing(LayerFocusContext context) => log.Add($"{name}:unfocusing");

    public void OnUnfocused(LayerFocusContext context) => log.Add($"{name}:unfocused");
  }
}
