using System.Globalization;
using System.Windows.Data;
using System.Windows;
using Everlong.Nester.Helpers;

namespace Everlong.Nester.Converters;

/// <summary>
/// Converts a notification type to a Brush resource key lookup for styling.
/// </summary>
/// <remarks>
/// This converter maps notification types (such as Success, Error, Warning, or Info) to corresponding
/// brush resources defined in the application's resource dictionary. It uses <see cref="ResourceKeyHelper.GetBrushKey(object)"/>
/// to determine the appropriate resource key and then retrieves the brush from the current application resources.
/// 
/// Typical usage is in notification UI elements where the background, foreground, or accent colors
/// should vary based on the notification type.
/// 
/// Example XAML usage:
/// &lt;TextBlock Background="{Binding NotificationType, Converter={StaticResource TypeToBrushConverter}}"/&gt;
/// 
/// Expected resource keys in ResourceDictionary: "SuccessBrush", "ErrorBrush", "WarningBrush", "InfoBrush".
/// </remarks>
/// <seealso cref="Helpers.ResourceKeyHelper.GetBrushKey(object)"/>
public sealed class TypeToBrushConverter : IValueConverter
{
  /// <summary>
  /// Converts a notification type to a brush resource.
  /// </summary>
  /// <param name="value">The source notification type enum value.</param>
  /// <param name="targetType">The type of the binding target property (Brush).</param>
  /// <param name="parameter">Ignored.</param>
  /// <param name="culture">The culture to use in the converter.</param>
  /// <returns>
  /// The brush resource associated with the notification type, or <see cref="DependencyProperty.UnsetValue"/>
  /// if the type is not recognized or the resource key is not found in the application resources.
  /// </returns>
  public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    string? key = ResourceKeyHelper.GetBrushKey(value);

    return key != null
             ? Application.Current.TryFindResource(key)
             : DependencyProperty.UnsetValue; // Fallback or default
  }

  /// <summary>
  /// Not supported. Throws <see cref="NotImplementedException"/>.
  /// </summary>
  /// <exception cref="NotImplementedException">ConvertBack is not implemented for this converter.</exception>
  public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
    throw new NotImplementedException();
}
