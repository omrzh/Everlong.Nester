using System.Globalization;
using System.Windows.Data;

namespace Everlong.Nester.Converters;

/// <summary>
/// Converts numeric indices to boolean values based on comparison with a target index.
/// </summary>
/// <remarks>
/// This converter supports both single-value and multi-value binding. In single-value mode,
/// it compares the input index against a target index provided via the converter parameter.
/// In multi-value mode, it compares an active index against a tag index from multiple binding sources.
/// 
/// Useful for data templates and UI states where you need to conditionally show/hide items or apply
/// styles based on their position in a list or collection.
/// 
/// Example (single-value): {Binding Path=CurrentItemIndex, Converter={StaticResource IndexToBoolConverter}, ConverterParameter=0}
/// Example (multi-value): {MultiBinding Converter={StaticResource IndexToBoolConverter}, ConverterParameter=0}
/// </remarks>
public class IndexToBoolConverter : IValueConverter, IMultiValueConverter
{
  /// <summary>
  /// Converts a single index value to a boolean by comparing it with a target index.
  /// </summary>
  /// <param name="value">The source index value (int).</param>
  /// <param name="targetType">The type of the binding target property (bool).</param>
  /// <param name="parameter">The target index to compare against (int or string representation of int).</param>
  /// <param name="culture">The culture to use in the converter.</param>
  /// <returns>
  /// <c>true</c> if the source index equals the target index; otherwise, <c>false</c>.
  /// </returns>
  public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    if (value is int index && parameter is string paramStr && int.TryParse(paramStr, out int targetIndex))
    {
      return index == targetIndex;
    }

    if (value is int idx && parameter is int targetIdx)
    {
      return idx == targetIdx;
    }

    return false;
  }

  /// <summary>
  /// Not supported. Throws <see cref="NotImplementedException"/>.
  /// </summary>
  /// <exception cref="NotImplementedException">ConvertBack is not implemented for this converter.</exception>
  public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    throw new NotImplementedException();
  }

  /// <summary>
  /// Converts multiple index values (active index and tag index) to a boolean by comparing them.
  /// </summary>
  /// <param name="values">
  /// An array of index values where:
  /// - values[0] is the active index (int)
  /// - values[1] is the tag index (int or string representation of int)
  /// </param>
  /// <param name="targetType">The type of the binding target property (bool).</param>
  /// <param name="parameter">Ignored.</param>
  /// <param name="culture">The culture to use in the converter.</param>
  /// <returns>
  /// <c>true</c> if the active index equals the tag index; otherwise, <c>false</c>.
  /// Returns <c>false</c> if the input array doesn't have exactly 2 values.
  /// </returns>
  public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
  {
    if (values.Length == 2)
    {
      var activeIndex = values[0];
      var tagIndex = values[1];

      if (activeIndex is int ai && tagIndex is int ti)
        return ai == ti;
      if (activeIndex is int ai2 && tagIndex is string ts && int.TryParse(ts, out int ti2))
        return ai2 == ti2;
    }

    return false;
  }

  /// <summary>
  /// Not supported. Throws <see cref="NotImplementedException"/>.
  /// </summary>
  /// <exception cref="NotImplementedException">ConvertBack is not implemented for this converter.</exception>
  public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
  {
    throw new NotImplementedException();
  }
}
