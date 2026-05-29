using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace Everlong.Nester.Converters;

/// <summary>
/// Calculates the responsive maximum size for a dialog based on user-defined size and parent container constraints.
/// </summary>
/// <remarks>
/// This multi-value converter implements a priority-based sizing strategy:
/// 1. If a user-defined maximum size is provided, it is respected.
/// 2. Otherwise, the maximum size is calculated as a percentage of the parent container size (default 90%, or as specified by the converter parameter).
/// 
/// This ensures dialogs remain responsive and do not exceed reasonable bounds relative to their container.
/// 
/// Example usage in XAML:
/// <code>
/// &lt;MultiBinding Converter="{StaticResource ResponsiveMaxSizeConverter}" ConverterParameter="0.85"&gt;
///   &lt;Binding Path="DialogMaxWidth" /&gt;
///   &lt;Binding Path="ParentWindowWidth" /&gt;
/// &lt;/MultiBinding&gt;
/// </code>
/// 
/// This would use the DialogMaxWidth if set; otherwise, 85% of ParentWindowWidth.
/// </remarks>
public sealed class ResponsiveMaxSizeConverter : IMultiValueConverter
{
  /// <summary>
  /// Converts multiple input values to a responsive maximum size for a dialog.
  /// </summary>
  /// <param name="values">
  /// A list of binding values. Expected structure:
  /// - Index 0: User-defined maximum size (<see cref="double"/>?). If null or missing, falls back to calculated value.
  /// - Index 1: Parent container size (<see cref="double"/>), the fallback basis when no user size is provided.
  /// </param>
  /// <param name="targetType">The type of the binding target property — <see cref="double"/>.</param>
  /// <param name="parameter">
  /// The ratio (as percentage) of parent size to use when calculating responsive size.
  /// Can be a <see cref="double"/> (<c>0.9</c>) or a string representation (<c>"0.85"</c>).
  /// If null or unparseable, defaults to 0.9 (90%).
  /// </param>
  /// <param name="culture">The culture for parsing the parameter.</param>
  /// <returns>
  /// If user-defined size is provided, returns it as a <see cref="double"/>.
  /// If parent size is available, returns the calculated responsive size (parent × ratio).
  /// Otherwise, returns <see cref="AvaloniaProperty.UnsetValue"/>.
  /// </returns>
  public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
  {
    // Value 0: User Defined Max Size (double?)
    // Value 1: Parent Container Size (double)

    switch (values.Count)
    {
      // 1. If User has defined a size, respect it.
      case > 0 when values[0] is double userMax:
        return userMax;
      // 2. Fallback to Percentage of Parent (Safety Net)
      case > 1 when values[1] is double parentSize:
        {
          double ratio = parameter switch
          {
            double r => r,
            string s when double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double pr) => pr,
            _ => 0.9
          };

          return parentSize * ratio;
        }
      default:
        return AvaloniaProperty.UnsetValue;
    }
  }
}
