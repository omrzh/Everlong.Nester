using Everlong.Nester.Layer;

namespace Everlong.Nester.Tests.Shell;

/// <summary>Shared tenant identity for tests that cut slices without a specific operator.</summary>
internal sealed class LayerTestTenant : ILayerTenant
{
  public static readonly LayerTestTenant Instance = new();

  /// <summary>The content the test pushes into the leased slot at acquire.</summary>
  public object Content { get; set; } = new object();

  /// <summary>Counts eviction notices received via <see cref="OnEvictedAsync"/>.</summary>
  public int EvictedCalls { get; private set; }

  public ValueTask OnEvictedAsync(ILayerLease lease)
  {
    EvictedCalls++;
    return ValueTask.CompletedTask;
  }
}
