namespace Everlong.Nester.Primitives;

/// <summary>
///   Converts between <see cref="ValueColor" /> and the Avalonia <see cref="Color" />.
/// </summary>
public static class ValueColorExtensions
{
  /// <summary>
  ///   Converts a <see cref="ValueColor" /> to the Avalonia <see cref="Color" />.
  /// </summary>
  /// <param name="c">The color to convert.</param>
  /// <returns>An Avalonia <see cref="Color" /> with the same ARGB components.</returns>
  public static Color ToColor(this ValueColor c)
    => Color.FromArgb(c.A, c.R, c.G, c.B);

  /// <summary>
  ///   Converts an Avalonia <see cref="Color" /> to a <see cref="ValueColor" />.
  /// </summary>
  /// <param name="c">The Avalonia color to convert.</param>
  /// <returns>A <see cref="ValueColor" /> containing the same ARGB components.</returns>
  public static ValueColor ToValueColor(this Color c)
    => new(c.A, c.R, c.G, c.B);
}
