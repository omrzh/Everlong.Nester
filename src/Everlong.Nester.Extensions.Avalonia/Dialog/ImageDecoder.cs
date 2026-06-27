using Avalonia.Media.Imaging;

namespace Everlong.Nester.Dialog;

internal sealed class AvaloniaImageDecoder : IImageDecoder
{
  public async Task<ImageDecodeResult> DecodeAsync(ImagePreviewSource source,
                                                    HttpClient? httpClient = null,
                                                    CancellationToken cancellationToken = default)
  {
    var bytes = await LoadBytesAsync(source, httpClient, cancellationToken).ConfigureAwait(true);
    if (bytes is not { Length: > 0 })
    {
      return new ImageDecodeResult(null);
    }

    using var stream = new MemoryStream(bytes, false);
    var bitmap = new Bitmap(stream);
    return new ImageDecodeResult(bitmap, bitmap);
  }

  private static async Task<byte[]?> LoadBytesAsync(ImagePreviewSource source,
                                                     HttpClient? httpClient,
                                                     CancellationToken ct)
  {
    switch (source)
    {
      case BytesImagePreviewSource { Data: var data }:
        return data;
      case FileImagePreviewSource { Path: var path }:
        {
          var fullPath = Path.GetFullPath(path);
          return File.Exists(fullPath)
            ? await File.ReadAllBytesAsync(fullPath, ct).ConfigureAwait(true)
            : null;
        }
      case UriImagePreviewSource { Location: { IsFile: true } uri }:
        return await File.ReadAllBytesAsync(uri.LocalPath, ct).ConfigureAwait(true);
      case UriImagePreviewSource { Location: var uri }:
        return await httpClient!.GetByteArrayAsync(uri, ct).ConfigureAwait(true);
      default:
        return null;
    }
  }
}
