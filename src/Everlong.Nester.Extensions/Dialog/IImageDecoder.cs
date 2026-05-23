namespace Everlong.Nester.Dialog;

/// <summary>
///   Decodes image sources into platform bitmap objects.
/// </summary>
public interface IImageDecoder
{
  /// <summary>
  ///   Decodes an image from the specified source.
  /// </summary>
  /// <param name="source">The image source descriptor.</param>
  /// <param name="httpClient">Required when <paramref name="source" /> is a web URI.</param>
  /// <param name="cancellationToken">Token to cancel the operation.</param>
  /// <returns>
  ///   An <see cref="ImageDecodeResult" /> containing the decoded image and an optional cleanup handle.
  ///   Dispose the result to release platform-specific bitmap resources.
  /// </returns>
  Task<ImageDecodeResult> DecodeAsync(ImagePreviewSource source,
                                      HttpClient? httpClient = null,
                                      CancellationToken cancellationToken = default);
}

/// <summary>
///   Holds a decoded image and an optional cleanup handle for platform resources.
/// </summary>
public sealed class ImageDecodeResult : IDisposable
{
  private readonly IDisposable? _cleanup;

  /// <summary>
  ///   Creates a new <see cref="ImageDecodeResult" />.
  /// </summary>
  /// <param name="image">The decoded image object.</param>
  /// <param name="cleanup">Optional disposable to clean up platform resources.</param>
  public ImageDecodeResult(object? image, IDisposable? cleanup = null)
  {
    Image = image;
    _cleanup = cleanup;
  }

  /// <summary>
  ///   Gets the decoded image object (platform-specific bitmap).
  /// </summary>
  public object? Image { get; }

  /// <summary>
  ///   Disposes platform resources associated with the decoded image.
  /// </summary>
  public void Dispose()
  {
    _cleanup?.Dispose();
  }
}
