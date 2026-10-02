using Everlong.Nester.Layer;
using Xunit;

namespace Everlong.Nester.Tests.Layer;

/// <summary>
///   The plane table's structural contract: the planes are ordered, their z
///   intervals are disjoint, and the ghost plane stays above them all.
/// </summary>
public class LayerPlanesTests
{
  private static readonly LayerPlane[] Ordered =
  [
    LayerPlane.Ground,
    LayerPlane.Base,
    LayerPlane.Dock,
    LayerPlane.Overlay,
    LayerPlane.Notice,
    LayerPlane.Debug,
    LayerPlane.Ghost,
  ];

  [Fact]
  public void Planes_AreOrdered_AndDisjoint()
  {
    for (int i = 1; i < Ordered.Length; i++)
    {
      LayerRange lower = LayerPlanes.Range(Ordered[i - 1]);
      LayerRange upper = LayerPlanes.Range(Ordered[i]);
      Assert.True(lower.Floor < upper.Floor, $"{Ordered[i - 1]} must sit below {Ordered[i]}");
      Assert.True(lower.Ceiling < upper.Floor,
        $"{lower.Floor}..{lower.Ceiling} overlaps {upper.Floor}..{upper.Ceiling}");
    }
  }

  [Fact]
  public void Ghost_IsTheTopPlane()
  {
    foreach (LayerPlane plane in Ordered)
    {
      if (plane is LayerPlane.Ghost)
        continue;
      Assert.True(LayerPlanes.Range(plane).Ceiling < LayerPlanes.Range(LayerPlane.Ghost).Floor);
    }
  }

  [Fact]
  public void NonStackingPlanes_AreKnown()
  {
    Assert.False(LayerPlanes.Stacks(LayerPlane.Ground));
    Assert.False(LayerPlanes.Stacks(LayerPlane.Base));
    Assert.True(LayerPlanes.Stacks(LayerPlane.Dock));
    Assert.True(LayerPlanes.Stacks(LayerPlane.Overlay));
    Assert.False(LayerPlanes.Stacks(LayerPlane.Notice));
    Assert.True(LayerPlanes.Stacks(LayerPlane.Debug));
    Assert.False(LayerPlanes.Stacks(LayerPlane.Ghost));
  }
}
