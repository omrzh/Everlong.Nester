using System.Globalization;
using System.Windows.Data;
using System.Windows;
using Everlong.Nester.Helpers;

namespace Everlong.Nester.Converters;

/// <summary>
/// Converts a notification type to a Geometry (Icon) resource.
/// </summary>
/// <remarks>
/// This converter maps notification types (such as Success, Error, Warning, or Info) to corresponding
/// icon geometry resources defined in the application's resource dictionary. It uses <see cref="Helpers.ResourceKeyHelper.GetIconKey(object)"/>
/// to determine the appropriate resource key and then retrieves the geometry from the current application resources.
/// 
/// Typical usage is in notification UI elements (like toast popups, snackbars, or alert dialogs) where
/// the displayed icon should visually represent the notification type.
/// 
/// Example XAML usage:
/// &lt;Path Data="{Binding NotificationType, Converter={StaticResource TypeToIconConverter}}"/&gt;
/// 
/// Expected resource keys in ResourceDictionary: "SuccessIconGeometry", "ErrorIconGeometry", "WarningIconGeometry", "InfoIconGeometry".
/// </remarks>
/// <seealso cref="Helpers.ResourceKeyHelper.GetIconKey(object)"/>
public sealed class TypeToIconConverter : IValueConverter
{
  /// <summary>
  /// Converts a notification type to an icon (geometry) resource.
  /// </summary>
  /// <param name="value">The source notification type enum value.</param>
  /// <param name="targetType">The type of the binding target property (Geometry).</param>
  /// <param name="parameter">Ignored.</param>
  /// <param name="culture">The culture to use in the converter.</param>
  /// <returns>
  /// The geometry resource associated with the notification type, or <see cref="DependencyProperty.UnsetValue"/>
  /// if the type is not recognized or the resource key is not found in the application resources.
  /// </returns>
  public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    string? key = ResourceKeyHelper.GetIconKey(value);
    return key != null ? Application.Current.TryFindResource(key) : DependencyProperty.UnsetValue;
    // Fallback or default
  }

  /// <summary>
  /// Not supported. Throws <see cref="NotImplementedException"/>.
  /// </summary>
  /// <exception cref="NotImplementedException">ConvertBack is not implemented for this converter.</exception>
  public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
    throw new NotImplementedException();
}
