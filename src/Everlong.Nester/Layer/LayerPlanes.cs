namespace Everlong.Nester.Layer;

/// <summary>The closed z interval a plane reserves.</summary>
internal readonly record struct LayerRange(int Floor, int Ceiling)
{
  /// <summary>Whether <paramref name="z" /> lies in the range.</summary>
  internal bool Contains(int z) => z >= Floor && z <= Ceiling;
}

/// <summary>The reserved planes and their stacking behaviour.</summary>
internal static class LayerPlanes
{
  /// <summary>The z interval reserved for <paramref name="plane" />.</summary>
  internal static LayerRange Range(LayerPlane plane) => plane switch
  {
    LayerPlane.Ground => new LayerRange(100, 299),
    LayerPlane.Base => new LayerRange(1000, 1999),
    LayerPlane.Dock => new LayerRange(2000, 2999),
    LayerPlane.Overlay => new LayerRange(3000, 7999),
    LayerPlane.Notice => new LayerRange(8000, 8499),
    LayerPlane.Debug => new LayerRange(9000, 9499),
    LayerPlane.Ghost => new LayerRange(9990, 9999),
    _ => throw new ArgumentOutOfRangeException(nameof(plane), plane, "Unknown layer plane."),
  };

  /// <summary>Whether a grant in <paramref name="plane" /> stacks above the plane's highest live lease.</summary>
  internal static bool Stacks(LayerPlane plane)
    => plane is LayerPlane.Dock or LayerPlane.Overlay or LayerPlane.Debug;
}
