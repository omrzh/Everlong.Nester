using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Everlong.Nester.Converters;

/// <summary>
/// Converts a stream containing image data to a WPF <see cref="BitmapImage"/>.
/// </summary>
/// <remarks>
/// The stream is read with caching enabled.  A seekable stream is rewound to position 0 before
/// reading.  The returned bitmap is frozen, so it is thread-safe and shareable across UI elements.
/// A null stream or invalid image data yields <see langword="null" />.
/// </remarks>
/// <seealso cref="BitmapImage"/>
public class StreamToBitmapConverter : IValueConverter
{
  /// <summary>
  /// Converts a stream to a <see cref="BitmapImage"/>.
  /// </summary>
  /// <param name="value">The source stream containing image data.</param>
  /// <param name="targetType">The type of the binding target property (<see cref="BitmapImage"/>).</param>
  /// <param name="parameter">Ignored.</param>
  /// <param name="culture">The culture to use in the converter.</param>
  /// <returns>
  /// A frozen <see cref="BitmapImage"/> created from the stream, or <c>null</c> if the stream is invalid
  /// or contains invalid image data.
  /// </returns>
  public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    if (value is Stream stream)
    {
      try
      {
        if (stream.CanSeek)
        {
          stream.Position = 0;
        }

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = stream;
        bitmap.EndInit();
        bitmap.Freeze();

        return bitmap;
      }
      catch
      {
        // Ignore invalid images
      }
    }

    return null;
  }

  /// <summary>
  /// Not supported. Throws <see cref="NotSupportedException"/>.
  /// </summary>
  /// <remarks>
  /// Stream to bitmap conversion is a one-way operation; converting a bitmap back to a stream is not supported.
  /// </remarks>
  /// <exception cref="NotSupportedException">ConvertBack is not supported for this converter.</exception>
  public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    throw new NotSupportedException();
  }
}
