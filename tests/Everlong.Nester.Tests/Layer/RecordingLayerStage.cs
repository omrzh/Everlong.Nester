using Everlong.Nester.Layer;

namespace Everlong.Nester.Tests.Layer;

/// <summary>Records the mount/unmount calls a ledger issues.</summary>
internal sealed class RecordingLayerStage : ILayerStage
{
  /// <summary>Leases mounted, in call order.</summary>
  internal List<ILayerLease> Mounted { get; } = [];

  /// <summary>Leases unmounted, in call order.</summary>
  internal List<ILayerLease> Unmounted { get; } = [];

  /// <inheritdoc />
  public void MountLease(ILayerLease lease) => Mounted.Add(lease);

  /// <inheritdoc />
  public void UnmountLease(ILayerLease lease) => Unmounted.Add(lease);
}
