using System.ComponentModel;

namespace Everlong.Nester.Layer;

/// <summary>The visual stack a ledger mounts lease slots onto.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface ILayerStage
{
  /// <summary>Mounts the slot of <paramref name="lease" />.</summary>
  void MountLease(ILayerLease lease);

  /// <summary>Detaches the slot of <paramref name="lease" />.</summary>
  void UnmountLease(ILayerLease lease);
}
