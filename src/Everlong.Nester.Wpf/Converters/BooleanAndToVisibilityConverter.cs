using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Everlong.Nester.Converters;

/// <summary>
///   A multi-value converter that yields <see cref="Visibility.Visible" />
///   only when every input is <see langword="true" />.
/// </summary>
public sealed class BooleanAndToVisibilityConverter : IMultiValueConverter
{
  /// <summary>Yields <see cref="Visibility.Collapsed" /> unless every input is <see langword="true" />.</summary>
  public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
  {
    foreach (object? value in values)
    {
      if (value is not bool boolValue || !boolValue)
      {
        return Visibility.Collapsed;
      }
    }

    return Visibility.Visible;
  }

  /// <summary>Not supported — the converter is one-way.</summary>
  /// <exception cref="NotSupportedException">Always thrown.</exception>
  public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
  {
    throw new NotSupportedException();
  }
}
