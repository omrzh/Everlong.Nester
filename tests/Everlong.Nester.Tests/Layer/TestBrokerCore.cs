using Everlong.Nester.Intent;
using Everlong.Nester.Layer;

namespace Everlong.Nester.Tests.Layer;

/// <summary>
///   A test-side <see cref="ILayerLedger"/> backed by <see cref="TestLease"/>
///   leases.
/// </summary>
internal sealed class TestBrokerCore : ILayerLedger
{
  private readonly List<Entry> _entries = [];
  private ILayerStage? _stage;

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

  /// <summary>Grants a lease at the neutral band's floor.</summary>
  internal ILayerLease Acquire(ILayerTenant tenant)
    => Acquire(tenant, new object(), KnownLayers.Neutral, LayerPolicy.Floor);

  /// <summary>Grants a lease at the exact z <paramref name="z"/>.</summary>
  internal ILayerLease Acquire(ILayerTenant tenant, int z)
    => Acquire(tenant, new object(), z);

  /// <summary>Grants a lease at the exact z <paramref name="z"/> with the given content.</summary>
  internal ILayerLease Acquire(ILayerTenant tenant, object content, int z)
    => AcquireAt(tenant, content, z);

  /// <summary>Grants a lease at the closest feasible position inside <paramref name="band"/> under <paramref name="policy"/>.</summary>
  internal ILayerLease Acquire(ILayerTenant tenant, object content, LayerBand band, LayerPolicy policy)
    => AcquireAt(tenant, content, ResolveZ(band, policy));

  private ILayerLease AcquireAt(ILayerTenant tenant, object content, int z)
  {
    ILayerLease lease = new TestLease(this, z) { Content = content };
    _stage?.MountLease(lease);
    _entries.Add(new Entry(lease, tenant));
    return lease;
  }

  bool ILayerLedger.IsLive(ILayerLease lease) => IsLive(lease);

  void ILayerLedger.Drop(ILayerLease lease)
  {
    if (Remove(lease) is not null)
      _stage?.UnmountLease(lease);
  }

  /// <summary>Evicts a lease: removed, unmounted, tenant notified.  Returns whether the lease was live.</summary>
  internal bool Evict(ILayerLease lease)
  {
    Entry? entry = Remove(lease);
    if (entry is null)
      return false;
    _stage?.UnmountLease(lease);
    entry.Tenant.OnEvictedAsync(lease);
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
    foreach (var (entry, idx) in _entries.Select((e, i) => (Entry: e, Idx: i))
                 .OrderByDescending(x => x.Entry.Lease.Z)
                 .ThenByDescending(x => x.Idx))
      handlers.Add(entry.Lease.IntentHandler);
    IntentDelegate? pipeline = handlers.Count > 0 ? IntentPipelineHelper.Build(handlers) : null;
    if (pipeline is null)
      return IntentResult.Pass;
    await pipeline(context);
    return context.Result;
  }

  /// <summary>Resolves the granted z: the closest position to the policy that stays inside the band.</summary>
  private int ResolveZ(LayerBand band, LayerPolicy policy)
  {
    if (policy == LayerPolicy.Ceiling)
      return band.Ceiling;
    if (policy != LayerPolicy.AboveHighest)
      return band.Floor;

    int top = band.Floor;
    bool any = false;
    foreach (var entry in _entries)
    {
      int z = entry.Lease.Z;
      if (!band.Contains(z))
        continue;
      if (!any || z > top)
      {
        top = z;
        any = true;
      }
    }

    if (!any)
      return band.Floor;
    return top >= band.Ceiling ? band.Ceiling : top + 1;
  }

  private Entry? Remove(ILayerLease lease)
  {
    var entry = _entries.FirstOrDefault(e => ReferenceEquals(e.Lease, lease));
    if (entry is null)
      return null;
    _entries.Remove(entry);
    return entry;
  }
}
