using Avalonia.Data.Converters;
using System.Globalization;

namespace Everlong.Nester.Converters;

/// <summary>
/// Converts a numeric index to a boolean value based on comparison with a target index.
/// </summary>
/// <remarks>
/// This converter is useful for conditional rendering in data templates or item containers
/// where you need to determine if the current item index matches a specific value.
///
/// Example usage in XAML:
/// <code>
/// &lt;CheckBox IsChecked="{Binding Index, Converter={StaticResource IndexToBoolConverter}, ConverterParameter=0}" /&gt;
/// </code>
///
/// This would set the CheckBox to checked only when the index equals 0.
/// The converter parameter can be passed as either a string or int representation of the target index.
/// </remarks>
public class IndexToBoolConverter : IValueConverter
{
  /// <summary>
  /// The shared instance.
  /// </summary>
  public static readonly IndexToBoolConverter Instance = new();

  /// <summary>
  /// Converts a numeric index to a boolean value by comparing it with the target index parameter.
  /// </summary>
  /// <param name="value">The numeric index to convert. Should be an <see cref="int"/>.</param>
  /// <param name="targetType">The type of the binding target property — <see cref="bool"/>.</param>
  /// <param name="parameter">The target index to compare against. Can be a string or int representation.</param>
  /// <param name="culture">Ignored.</param>
  /// <returns>
  /// <c>true</c> if the value equals the target index parameter; otherwise <c>false</c>.
  /// Returns <c>false</c> if value is not an integer or parameter cannot be parsed.
  /// </returns>
  public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    if (value is int index && parameter is string paramStr && int.TryParse(paramStr, out int targetIndex))
    {
      return index == targetIndex;
    }

    // Also handle int parameter if binding engine passes it directly (though XAML usually passes string)
    if (value is int idx && parameter is int targetIdx)
    {
      return idx == targetIdx;
    }

    return false;
  }

  /// <summary>
  /// This converter does not support reverse conversion.
  /// </summary>
  public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    throw new NotImplementedException();
  }
}
