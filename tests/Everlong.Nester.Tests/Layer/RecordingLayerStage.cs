using Everlong.Nester.Layer;

namespace Everlong.Nester.Tests.Layer;

/// <summary>Records the mount/unmount calls a ledger issues.</summary>
internal sealed class RecordingLayerStage : ILayerStage
{
  /// <summary>Handles mounted, in call order.</summary>
  internal List<ILayerHandle> Mounted { get; } = [];

  /// <summary>Handles unmounted, in call order.</summary>
  internal List<ILayerHandle> Unmounted { get; } = [];

  /// <inheritdoc />
  public void MountLease(ILayerHandle handle) => Mounted.Add(handle);

  /// <inheritdoc />
  public void UnmountLease(ILayerHandle handle) => Unmounted.Add(handle);
}
