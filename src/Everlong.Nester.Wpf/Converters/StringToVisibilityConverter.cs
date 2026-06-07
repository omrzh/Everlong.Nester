using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Everlong.Nester.Converters;

/// <summary>
/// Converts a string value to a <see cref="Visibility"/> enum value based on whether the string is null or empty.
/// </summary>
/// <remarks>
/// This converter is useful for conditionally displaying or hiding UI elements based on whether a bound string property
/// contains any meaningful content. Empty, null, or whitespace-only strings result in <see cref="Visibility.Collapsed"/>,
/// while non-empty strings result in <see cref="Visibility.Visible"/>.
/// 
/// Example XAML usage:
/// &lt;TextBlock Text="{Binding StatusMessage}" Visibility="{Binding StatusMessage, Converter={StaticResource StringToVisibilityConverter}}"/&gt;
/// </remarks>
/// <seealso cref="Visibility"/>
public sealed class StringToVisibilityConverter : IValueConverter
{
  /// <summary>
  /// Converts a string value to a <see cref="Visibility"/> value.
  /// </summary>
  /// <param name="value">The source string value.</param>
  /// <param name="targetType">The type of the binding target property (Visibility).</param>
  /// <param name="parameter">Ignored.</param>
  /// <param name="culture">The culture to use in the converter.</param>
  /// <returns>
  /// <see cref="Visibility.Visible"/> if the string is not null or empty; otherwise, <see cref="Visibility.Collapsed"/>.
  /// </returns>
  public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
    string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;

  /// <summary>
  /// Not supported. Throws <see cref="NotImplementedException"/>.
  /// </summary>
  /// <exception cref="NotImplementedException">ConvertBack is not implemented for this converter.</exception>
  public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
    throw new NotImplementedException();
}
