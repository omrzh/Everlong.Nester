using System.ComponentModel;

namespace Everlong.Nester.Layer;

/// <summary>The ledger's lease-facing operations: liveness and removal.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface ILayerLedger
{
  /// <summary>Whether <paramref name="lease" /> is still recorded.</summary>
  bool IsLive(ILayerLease lease);

  /// <summary>Removes <paramref name="lease" /> and unmounts its slot; no eviction notice.</summary>
  void Drop(ILayerLease lease);
}
