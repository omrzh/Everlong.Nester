namespace Everlong.Nester.Primitives;

/// <summary>
///   A four-edge thickness — uniform, symmetric or per-edge spacing.
/// </summary>
public readonly struct Thickness(double left, double top, double right, double bottom) : IEquatable<Thickness>
{
  /// <summary>Gets the thickness on the left edge.</summary>
  public double Left { get; } = left;

  /// <summary>Gets the thickness on the top edge.</summary>
  public double Top { get; } = top;

  /// <summary>Gets the thickness on the right edge.</summary>
  public double Right { get; } = right;

  /// <summary>Gets the thickness on the bottom edge.</summary>
  public double Bottom { get; } = bottom;

  /// <summary>A thickness of zero on every edge.</summary>
  public static Thickness Zero => default;

  /// <summary>Creates a uniform thickness on every edge.</summary>
  public Thickness(double uniform)
    : this(uniform, uniform, uniform, uniform)
  {
  }

  /// <summary>Creates a thickness with symmetric horizontal and vertical values.</summary>
  public Thickness(double horizontal, double vertical)
    : this(horizontal, vertical, horizontal, vertical)
  {
  }

  /// <inheritdoc />
  public bool Equals(Thickness other)
    => Left.Equals(other.Left)
       && Top.Equals(other.Top)
       && Right.Equals(other.Right)
       && Bottom.Equals(other.Bottom);

  /// <inheritdoc />
  public override bool Equals(object? obj) => obj is Thickness other && Equals(other);

  /// <inheritdoc />
  public override int GetHashCode() => HashCode.Combine(Left, Top, Right, Bottom);

  /// <summary>Compares two thicknesses for equality.</summary>
  public static bool operator ==(Thickness left, Thickness right) => left.Equals(right);

  /// <summary>Compares two thicknesses for inequality.</summary>
  public static bool operator !=(Thickness left, Thickness right) => !left.Equals(right);

  /// <inheritdoc />
  public override string ToString() => $"{{Left={Left}, Top={Top}, Right={Right}, Bottom={Bottom}}}";
}
