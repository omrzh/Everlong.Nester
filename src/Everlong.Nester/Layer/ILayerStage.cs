using System.ComponentModel;

namespace Everlong.Nester.Layer;

/// <summary>The visual stack a ledger mounts lease slots onto.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface ILayerStage
{
  /// <summary>Mounts the slot of <paramref name="handle" />.</summary>
  void MountLease(ILayerHandle handle);

  /// <summary>Detaches the slot of <paramref name="handle" />.</summary>
  void UnmountLease(ILayerHandle handle);
}
