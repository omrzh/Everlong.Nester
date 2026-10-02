namespace Everlong.Nester.Primitives;

/// <summary>
///   Represents a rectangular region by its position and size.
///   Valid across all platforms; on non-windowed platforms, X and Y are zero.
/// </summary>
public readonly record struct Bounds(double X, double Y, double Width, double Height)
{
  /// <summary>
  ///   A static empty bounds instance with zeroed position and size.
  /// </summary>
  public static Bounds Empty => new(0, 0, 0, 0);

  /// <summary>
  ///   Whether the bounds are empty.
  /// </summary>
  public bool IsEmpty => Width <= 0 || Height <= 0;

  /// <inheritdoc />
  public override string ToString()
  {
    return $"({X}, {Y}) {Width}×{Height}";
  }
}
