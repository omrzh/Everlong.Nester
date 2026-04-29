namespace Everlong.Nester.Primitives;

/// <summary>
///   ARGB color channel values.
/// </summary>
public readonly record struct ValueColor(byte A, byte R, byte G, byte B)
{
  /// <summary>
  ///   Converts the ARGB color channels to a hexadecimal string.
  /// </summary>
  /// <returns>A string in <c>#AARRGGBB</c> format.</returns>
  public string ToHexArgb()
  {
    return $"#{A:X2}{R:X2}{G:X2}{B:X2}";
  }
}
