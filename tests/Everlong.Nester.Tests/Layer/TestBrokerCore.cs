using Everlong.Nester.Intent;
using Everlong.Nester.Layer;

namespace Everlong.Nester.Tests.Layer;

/// <summary>
///   A test-side broker/ledger over <see cref="TestLease" /> — the plane
///   table and focus election mirror the shell broker, with exact-z overloads
///   for suites that pin dispatch order.
/// </summary>
internal sealed class TestBrokerCore : ILayerBroker, ILayerLedger
{
  private readonly List<Entry> _entries = [];
  private ILayerStage? _stage;
  private ILayerLease? _focused;
  private bool _electing;
  private bool _focusDirty;

  private sealed record Entry(ILayerLease Lease, ILayerTenant Tenant);

  /// <summary>Connects the ledger to the stage and mounts every live lease.</summary>
  internal void Connect(ILayerStage? stage)
  {
    if (ReferenceEquals(_stage, stage))
      return;
    if (_stage is not null)
    {
      foreach (var entry in _entries)
        _stage.UnmountLease(entry.Lease);
    }

    _stage = stage;
    if (stage is null)
      return;
    foreach (var entry in _entries)
      stage.MountLease(entry.Lease);
  }

  /// <summary>Grants a lease in <paramref name="plane" /> at the plane's stacking position.</summary>
  internal ILayerLease Acquire(ILayerTenant tenant, LayerPlane plane)
    => Acquire(tenant, new object(), plane);

  /// <summary>Grants a lease with the given content in <paramref name="plane" /> at the plane's stacking position.</summary>
  internal ILayerLease Acquire(ILayerTenant tenant, object content, LayerPlane plane)
    => AcquireAt(tenant, content, plane, ResolveZ(plane));

  /// <summary>Grants a lease at the exact z <paramref name="z" /> in the overlay plane.</summary>
  internal ILayerLease Acquire(ILayerTenant tenant, int z)
    => Acquire(tenant, new object(), z);

  /// <summary>Grants a lease at the exact z <paramref name="z" /> with the given content.</summary>
  internal ILayerLease Acquire(ILayerTenant tenant, object content, int z)
    => AcquireAt(tenant, content, LayerPlane.Overlay, z);

  private ILayerLease AcquireAt(ILayerTenant tenant, object content, LayerPlane plane, int z)
  {
    ILayerLease lease = new TestLease(this, plane, z) { Content = content };
    _stage?.MountLease(lease);
    _entries.Add(new Entry(lease, tenant));
    ReelectFocus(LayerFocusCause.Granted);
    return lease;
  }

  /// <inheritdoc />
  ILayerLease ILayerBroker.Acquire(ILayerTenant tenant, object content, LayerPlane plane)
    => Acquire(tenant, content, plane);

  /// <inheritdoc />
  public ILayerLease? Focused => _focused;

  /// <inheritdoc />
  public void RequestFocus(ILayerLease lease)
  {
    if (!IsLive(lease) || TenantOf(lease) is not IFocusableLayer)
      return;
    ApplyFocus(lease, LayerFocusCause.Requested);
  }

  bool ILayerLedger.IsLive(ILayerLease lease) => IsLive(lease);

  void ILayerLedger.Drop(ILayerLease lease)
  {
    var entry = Remove(lease);
    if (entry is null)
      return;
    _stage?.UnmountLease(lease);
    DepartFocus(lease, entry.Tenant);
    ReelectFocus(LayerFocusCause.Departed);
  }

  /// <summary>Evicts a lease: removed, unmounted, tenant notified.  Returns whether the lease was live.</summary>
  internal bool Evict(ILayerLease lease)
  {
    Entry? entry = Remove(lease);
    if (entry is null)
      return false;
    _stage?.UnmountLease(lease);
    DepartFocus(lease, entry.Tenant);
    entry.Tenant.OnEvictedAsync(lease);
    ReelectFocus(LayerFocusCause.Departed);
    return true;
  }

  /// <summary>Whether the lease is still live.</summary>
  internal bool IsLive(ILayerLease lease)
    => _entries.Any(e => ReferenceEquals(e.Lease, lease));

  /// <summary>The intent dispatch order: z desc, latest grant first within a z.</summary>
  internal IEnumerable<ILayerLease> BottomUp()
    => _entries.Select((e, index) => (e, index))
               .OrderByDescending(x => x.e.Lease.Z)
               .ThenByDescending(x => x.index)
               .Select(x => x.e.Lease);

  /// <summary>Intent dispatch over the live leases (z desc, latest grant first) — each lease's intent handler gets first refusal.</summary>
  internal async ValueTask<IntentResult> TryDispatch(IntentContext context)
  {
    var handlers = new List<IIntentHandler>();
    foreach (ILayerLease lease in BottomUp())
      handlers.Add(lease.IntentHandler);
    IntentDelegate? pipeline = handlers.Count > 0 ? IntentPipelineHelper.Build(handlers) : null;
    if (pipeline is null)
      return IntentResult.Pass;
    await pipeline(context);
    return context.Result;
  }

  /// <summary>Resolves the granted z: the plane's floor, or one above the plane's highest live z when the plane stacks.</summary>
  private int ResolveZ(LayerPlane plane)
  {
    LayerRange range = LayerPlanes.Range(plane);
    if (!LayerPlanes.Stacks(plane))
      return range.Floor;

    int top = range.Floor;
    bool any = false;
    foreach (var entry in _entries)
    {
      int z = entry.Lease.Z;
      if (!range.Contains(z))
        continue;
      if (!any || z > top)
      {
        top = z;
        any = true;
      }
    }

    if (!any)
      return range.Floor;
    return top >= range.Ceiling ? range.Ceiling : top + 1;
  }

  // ── focus — mirrors the shell broker ─────────────────────────────────────

  private void ReelectFocus(LayerFocusCause cause)
  {
    if (_electing)
    {
      _focusDirty = true;
      return;
    }

    _electing = true;
    try
    {
      do
      {
        _focusDirty = false;
        ApplyFocus(Elect(cause), cause);
      } while (_focusDirty);
    }
    finally
    {
      _electing = false;
    }
  }

  private ILayerLease? Elect(LayerFocusCause cause)
  {
    var context = new LayerFocusContext(cause);
    foreach (var entry in _entries.Select((e, index) => (e, index))
                 .OrderByDescending(x => x.e.Lease.Z)
                 .ThenByDescending(x => x.index))
    {
      if (entry.e.Tenant is IFocusableLayer focusable && focusable.TryFocus(context))
        return entry.e.Lease;
    }

    return null;
  }

  private void ApplyFocus(ILayerLease? winner, LayerFocusCause cause)
  {
    if (ReferenceEquals(winner, _focused))
      return;

    var context = new LayerFocusContext(cause);
    ILayerLease? outgoing = _focused;
    IFocusableLayer? outgoingFocus = TenantOf(outgoing) as IFocusableLayer;
    IFocusableLayer? incomingFocus = TenantOf(winner) as IFocusableLayer;

    outgoingFocus?.OnUnfocusing(context);
    incomingFocus?.OnFocusing(context);

    _focused = winner;

    outgoingFocus?.OnUnfocused(context);
    incomingFocus?.OnFocused(context);
  }

  private void DepartFocus(ILayerLease lease, ILayerTenant tenant)
  {
    if (!ReferenceEquals(_focused, lease) || tenant is not IFocusableLayer focusable)
      return;

    var context = new LayerFocusContext(LayerFocusCause.Departed);
    focusable.OnUnfocusing(context);
    _focused = null;
    focusable.OnUnfocused(context);
  }

  private ILayerTenant? TenantOf(ILayerLease? lease)
    => lease is null ? null : _entries.FirstOrDefault(e => ReferenceEquals(e.Lease, lease))?.Tenant;

  private Entry? Remove(ILayerLease lease)
  {
    var entry = _entries.FirstOrDefault(e => ReferenceEquals(e.Lease, lease));
    if (entry is null)
      return null;
    _entries.Remove(entry);
    return entry;
  }
}
