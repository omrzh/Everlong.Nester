using System.Globalization;
using System.Windows;
using System.Windows.Data;


namespace Everlong.Nester.Converters;

/// <summary>
/// Selects the responsive maximum size for a dialog from a user-defined constraint and the parent
/// container's size.
/// </summary>
/// <remarks>
/// Priority: <c>values[0]</c> — the user-defined maximum — wins when present; otherwise the parent
/// size scaled by the converter parameter (default 0.9).
/// </remarks>
/// <seealso cref="IMultiValueConverter"/>
public sealed class ResponsiveMaxSizeConverter : IMultiValueConverter
{
  /// <summary>
  /// Converts multiple size values to a responsive maximum size.
  /// </summary>
  /// <param name="values">
  /// An array of size values where:
  /// - values[0] is the user-defined maximum size (nullable double), or missing for default behavior.
  /// - values[1] is the parent container size (double) used as the basis for percentage calculation.
  /// </param>
  /// <param name="targetType">The type of the binding target property (double).</param>
  /// <param name="parameter">
  /// The percentage ratio for the safety net calculation (double or string representation of double).
  /// If not provided, defaults to 0.9 (90% of parent size).
  /// </param>
  /// <param name="culture">The culture to use in the converter.</param>
  /// <returns>
  /// The user-defined maximum size if available, otherwise the percentage-based safety net value.
  /// Returns <see cref="DependencyProperty.UnsetValue"/> if neither value is available.
  /// </returns>
  public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
  {
    // Value 0: User Defined Max Size (double?)
    // Value 1: Parent Container Size (double)

    // 1. If User has defined a size, respect it.
    if (values.Length > 0 && values[0] is double userMax)
    {
      return userMax;
    }

    // 2. Fallback to Percentage of Parent (Safety Net)
    if (values.Length > 1 && values[1] is double parentSize)
    {
      double ratio = 0.9; // Default: 90%

      if (parameter is double r)
        ratio = r;
      else if (parameter is string s &&
               double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double pr))
        ratio = pr;

      // No user constraint applies — the parent size scaled by the ratio
      // (90% by default) bounds the dialog.
      return parentSize * ratio;
    }

    return DependencyProperty.UnsetValue;
  }

  /// <summary>
  /// Not supported. Throws <see cref="NotImplementedException"/>.
  /// </summary>
  /// <exception cref="NotImplementedException">ConvertBack is not implemented for multi-value converters.</exception>
  public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
  {
    throw new NotImplementedException();
  }
}
