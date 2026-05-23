using Everlong.Nester.ComponentModel;
using Everlong.Nester.Extensions.Properties;
using Everlong.Nester.Routing;
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Everlong.DI;
using Everlong.Nester.Notice;

namespace Everlong.Nester.Dialog;

/// <summary>
///   Describes an image source.
/// </summary>
public abstract record ImagePreviewSource
{
  /// <summary>Gets a human-readable description of the source for display in the UI.</summary>
  public abstract string DisplayText { get; }

  /// <summary>Creates a source backed by an in-memory byte array.</summary>
  public static ImagePreviewSource FromBytes(byte[] data)
  {
    return new BytesImagePreviewSource(data);
  }

  /// <summary>Creates a source backed by a local file path or a <c>file://</c> URI string.</summary>
  public static ImagePreviewSource FromFile(string path)
  {
    return new FileImagePreviewSource(path);
  }

  /// <summary>
  ///   Creates a source backed by a URI.
  ///   HTTP/HTTPS URIs require an <see cref="HttpClient" /> provided via
  ///   <see cref="ImagePreviewInput.HttpClient" />.
  /// </summary>
  public static ImagePreviewSource FromUri(Uri uri)
  {
    return new UriImagePreviewSource(uri);
  }
}

/// <summary>Image from an in-memory byte array.</summary>
public sealed record BytesImagePreviewSource(byte[] Data) : ImagePreviewSource
{
  /// <summary>
  ///   Gets a human-readable display text showing the byte array size.
  /// </summary>
  public override string DisplayText =>
    Lang.Dialog.ImagePreview.FormatBytesSourceDisplay(Data.Length);
}

/// <summary>Image from a local file path.</summary>
public sealed record FileImagePreviewSource(string Path) : ImagePreviewSource
{
  /// <summary>
  ///   Gets the file path as the display text.
  /// </summary>
  public override string DisplayText => Path;
}

/// <summary>Image from a URI — either a <c>file://</c> URI or an HTTP/HTTPS URL.</summary>
public sealed record UriImagePreviewSource(Uri Location) : ImagePreviewSource
{
  /// <summary>
  ///   Gets the URI as the display text.
  /// </summary>
  public override string DisplayText => Location.ToString();
}

/// <summary>
///   The initial data for an image preview dialog.
/// </summary>
public sealed class ImagePreviewInput
{
  /// <summary>The ordered list of image sources to display. Must not be empty.</summary>
  public IReadOnlyList<ImagePreviewSource> Sources { get; init; } = [];

  /// <summary>Zero-based index of the image to show first. Clamped to valid range at display time.</summary>
  public int StartIndex { get; init; }

  /// <summary>Optional dialog title shown in the top bar.</summary>
  public string? Title { get; init; }

  /// <summary>
  ///   Optional <see cref="HttpClient" /> used when loading HTTP/HTTPS sources.
  ///   The caller retains ownership — the session never disposes it.
  ///   Required when any source is an HTTP/HTTPS URI; the dialog shows an error if <see langword="null" /> in that case.
  /// </summary>
  public HttpClient? HttpClient { get; init; }

  /// <summary>Creates input for a single in-memory byte array image.</summary>
  public static ImagePreviewInput FromBytes(byte[] data, string? title = null)
  {
    return new ImagePreviewInput { Sources = [ImagePreviewSource.FromBytes(data)], Title = title };
  }

  /// <summary>Creates input for a single local file image.</summary>
  public static ImagePreviewInput FromFile(string path, string? title = null)
  {
    return new ImagePreviewInput { Sources = [ImagePreviewSource.FromFile(path)], Title = title };
  }

  /// <summary>
  ///   Creates input for a single HTTP/HTTPS or <c>file://</c> URI image.
  ///   <paramref name="httpClient" /> is required and must be pre-configured for the target host.
  /// </summary>
  public static ImagePreviewInput FromUri(Uri uri, HttpClient httpClient, string? title = null)
  {
    return new ImagePreviewInput
    { Sources = [ImagePreviewSource.FromUri(uri)], HttpClient = httpClient, Title = title };
  }

  /// <summary>
  ///   Creates input from a string that is either a local file path or an HTTP/HTTPS URL.
  ///   For HTTP/HTTPS URLs, supply <paramref name="httpClient" />; for local paths it is ignored.
  /// </summary>
  public static ImagePreviewInput FromPathOrUrl(string pathOrUrl,
                                                HttpClient? httpClient = null,
                                                string? title = null)
  {
    return new ImagePreviewInput
    {
      Sources = [StringToSource(pathOrUrl)],
      HttpClient = httpClient,
      Title = title
    };
  }

  /// <summary>Creates input for a multi-image slideshow from local file paths.</summary>
  public static ImagePreviewInput FromFiles(IEnumerable<string> paths,
                                            int startIndex = 0,
                                            string? title = null)
  {
    ArgumentNullException.ThrowIfNull(paths);
    ImagePreviewSource[] sourceList = paths.Select(p => ImagePreviewSource.FromFile(p)).ToArray();
    if (sourceList.Length == 0)
    {
      throw new ArgumentException("paths cannot be empty", nameof(paths));
    }

    return new ImagePreviewInput { Sources = sourceList, StartIndex = startIndex, Title = title };
  }

  /// <summary>
  ///   Creates input for a multi-image slideshow from an explicit source list.
  ///   Supply <paramref name="httpClient" /> when any source is an HTTP/HTTPS URI.
  /// </summary>
  public static ImagePreviewInput FromSources(IEnumerable<ImagePreviewSource> sources,
                                              HttpClient? httpClient = null,
                                              int startIndex = 0,
                                              string? title = null)
  {
    ArgumentNullException.ThrowIfNull(sources);
    ImagePreviewSource[] sourceList = sources.ToArray();
    if (sourceList.Length == 0)
    {
      throw new ArgumentException("sources cannot be empty", nameof(sources));
    }

    return new ImagePreviewInput
    { Sources = sourceList, HttpClient = httpClient, StartIndex = startIndex, Title = title };
  }

  private static ImagePreviewSource StringToSource(string pathOrUrl)
  {
    return Uri.TryCreate(pathOrUrl, UriKind.Absolute, out Uri? uri)
           && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
             ? ImagePreviewSource.FromUri(uri)
             : ImagePreviewSource.FromFile(pathOrUrl);
  }
}

/// <summary>
///   A built-in dialog session that displays one or more images with navigation support.
///   To load images over HTTP/HTTPS, provide a pre-configured <see cref="HttpClient" /> via
///   <see cref="ImagePreviewInput.HttpClient" />.
/// </summary>
public partial class ImagePreviewDialogSession : DialogSessionBase<object?>, IInjectable, IReleasable
{
  private ImageDecodeResult? _currentDecodeResult;
  private bool _isReleased;
  private CancellationTokenSource? _loadCts;
  private INoticeService? _toastService;

  /// <summary>The heading shown in the preview's top bar.</summary>
  [ObservableProperty] public partial string? Title { get; set; }

  /// <summary>The ordered list of image sources to display.</summary>
  public IReadOnlyList<ImagePreviewSource> Sources { get; init; } = [];

  /// <summary>
  ///   Optional <see cref="HttpClient" /> for loading HTTP/HTTPS sources.
  ///   Ownership stays with the caller — this session never disposes the client.
  /// </summary>
  public HttpClient? HttpClient { get; init; }

  /// <summary>
  ///   The <see cref="IImageDecoder" /> decoding the current source.
  ///   Images cannot be displayed while <see langword="null" />.
  /// </summary>
  public IImageDecoder? Decoder { get; set; }

  [ObservableProperty]
  public partial object? CurrentImage { get; private set; }

  /// <summary>
  ///   Gets or sets the zero-based index of the currently displayed image in the sources collection.
  /// </summary>
  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(CurrentSource))]
  [NotifyPropertyChangedFor(nameof(SourceText))]
  [NotifyPropertyChangedFor(nameof(IndexText))]
  [NotifyPropertyChangedFor(nameof(CanGoPrevious))]
  [NotifyPropertyChangedFor(nameof(CanGoNext))]
  [NotifyCanExecuteChangedFor(nameof(GoPreviousCommand))]
  [NotifyCanExecuteChangedFor(nameof(GoNextCommand))]
  public partial int CurrentIndex { get; set; }

  /// <summary>
  ///   Gets or sets whether an image is currently being loaded.
  /// </summary>
  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(StatusText))]
  [NotifyPropertyChangedFor(nameof(HasStatus))]
  public partial bool IsLoading { get; private set; } = true;

  /// <summary>
  ///   Gets or sets the error message displayed when image loading fails.
  /// </summary>
  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(StatusText))]
  [NotifyPropertyChangedFor(nameof(HasStatus))]
  public partial string ErrorText { get; private set; } = "";

  /// <summary>True when there is exactly one image (single-image mode); navigation is hidden.</summary>
  public bool IsSingleMode => Sources.Count <= 1;

  /// <summary>True when there are multiple images; navigation buttons and index counter are shown.</summary>
  public bool IsMultiMode => Sources.Count > 1;

  /// <summary>"N / M" counter shown in the top bar during multi-image mode.</summary>
  public string IndexText => IsMultiMode ? $"{CurrentIndex + 1} / {Sources.Count}" : "";

  /// <summary>The label of the step-back button shown in the top bar.</summary>
  public string PreviousText => Lang.Dialog.ImagePreview.PreviousText;

  /// <summary>The label of the step-forward button shown in the top bar.</summary>
  public string NextText => Lang.Dialog.ImagePreview.NextText;

  /// <summary>Text shown in the status overlay; non-empty when loading or an error has occurred.</summary>
  public string StatusText => IsLoading ? Lang.Dialog.ImagePreview.LoadingStatus : ErrorText;

  /// <summary><see langword="true" /> when the status overlay should be visible.</summary>
  public bool HasStatus => !string.IsNullOrWhiteSpace(StatusText);

  /// <summary>
  ///   The <see cref="ImagePreviewSource" /> currently being displayed, or <see langword="null" /> when the index is
  ///   out of range.
  /// </summary>
  public ImagePreviewSource? CurrentSource =>
    CurrentIndex >= 0 && CurrentIndex < Sources.Count ? Sources[CurrentIndex] : null;

  /// <summary><see langword="true" /> when the user can navigate to the previous image.</summary>
  public bool CanGoPrevious => CurrentIndex > 0;

  /// <summary><see langword="true" /> when the user can navigate to the next image.</summary>
  public bool CanGoNext => CurrentIndex >= 0 && CurrentIndex < Sources.Count - 1;

  /// <summary>Human-readable description of the current source, shown in the bottom bar.</summary>
  public string SourceText => CurrentSource?.DisplayText ?? "";

  /// <inheritdoc />
  public void Release()
  {
    if (_isReleased)
    {
      return;
    }

    _loadCts?.Cancel();
    _loadCts?.Dispose();
    _currentDecodeResult?.Dispose();
    _isReleased = true;
  }

  /// <inheritdoc />
  public virtual void Inject(IServiceProvider services)
  {
    _toastService = services.GetRequiredService<INoticeService>();
  }

  /// <summary>
  ///   Loads the current image when engaged — the arrival callback (the
  ///   session's initial <see cref="IsLoading" /> state already shows the
  ///   loading surface, so the reveal never flashes an empty frame).
  /// </summary>
  /// <param name="context">The arrival context — its lifetime token cancels the load.</param>
  public override Task OnArrivedAsync(IRoutingContext context)
  {
    base.OnArrivedAsync(context);
    return LoadCurrentImageAsync(context.Lifetime);
  }

  partial void OnCurrentIndexChanged(int value)
  {
    _ = LoadCurrentImageAsync();
  }

  private async Task LoadCurrentImageAsync(CancellationToken token = default)
  {
    _loadCts?.Cancel();
    _loadCts?.Dispose();
    CancellationTokenSource cts = new();
    _loadCts = cts;
    CancellationToken ct = cts.Token;

    IsLoading = true;
    ErrorText = "";
    CurrentImage = null;
    _currentDecodeResult?.Dispose();
    _currentDecodeResult = null;

    ImagePreviewSource? source = CurrentSource;
    if (source is null)
    {
      IsLoading = false;
      return;
    }

    IImageDecoder? decoder = Decoder;
    if (decoder is null)
    {
      ErrorText = Lang.Dialog.ImagePreview.LoadFailedError;
      _toastService?.Toast(Lang.Dialog.ImagePreview.LoadFailedError, ToastLevel.Error);
      IsLoading = false;
      return;
    }

    HttpClient? httpClient = null;
    if (source is UriImagePreviewSource { Location: { Scheme: var scheme } }
        && (scheme == Uri.UriSchemeHttp || scheme == Uri.UriSchemeHttps))
    {
      if (HttpClient is null)
      {
        ErrorText = Lang.Dialog.ImagePreview.NoHttpClientError;
        _toastService?.Toast(Lang.Dialog.ImagePreview.LoadFailedError, ToastLevel.Error);
        IsLoading = false;
        return;
      }

      httpClient = HttpClient;
    }

    try
    {
      ImageDecodeResult result = await decoder.DecodeAsync(source, httpClient, ct).ConfigureAwait(true);
      if (ct.IsCancellationRequested)
      {
        result.Dispose();
        return;
      }

      _currentDecodeResult = result;
      CurrentImage = result.Image;
    }
    catch (OperationCanceledException) { }
    catch
    {
      ErrorText = Lang.Dialog.ImagePreview.LoadFailedError;
      _toastService?.Toast(Lang.Dialog.ImagePreview.LoadFailedError, ToastLevel.Error);
    }
    finally
    {
      if (!ct.IsCancellationRequested)
      {
        IsLoading = false;
      }
    }
  }

  [RelayCommand(CanExecute = nameof(CanGoPrevious))]
  private void GoPrevious()
  {
    if (CanGoPrevious)
    {
      CurrentIndex--;
    }
  }

  [RelayCommand(CanExecute = nameof(CanGoNext))]
  private void GoNext()
  {
    if (CanGoNext)
    {
      CurrentIndex++;
    }
  }

  [RelayCommand]
  private void CloseDialog()
  {
    Close();
  }
}

/// <summary>
///   Extension methods for showing image preview dialogs using the <see cref="IRouter" />.
/// </summary>
public static partial class DialogSessionExtensions
{
  extension(IRouter router)
  {
    /// <summary>
    ///   Shows a full-screen image preview dialog described by <paramref name="input" />.
    ///   Use the <see cref="ImagePreviewInput" /> factory methods to build the input:
    ///   <see cref="ImagePreviewInput.FromBytes" />, <see cref="ImagePreviewInput.FromFile" />,
    ///   <see cref="ImagePreviewInput.FromUri" />, <see cref="ImagePreviewInput.FromPathOrUrl" />,
    ///   <see cref="ImagePreviewInput.FromFiles" />, or <see cref="ImagePreviewInput.FromSources" />.
    /// </summary>
    /// <param name="input">All initial data for the dialog: sources, title, HTTP client, start index.</param>
    /// <returns>A task that completes when the user closes the image preview dialog.</returns>
    public async Task PreviewAsync(ImagePreviewInput input)
    {
      ArgumentNullException.ThrowIfNull(input);
      IReadOnlyList<ImagePreviewSource> sourceList = input.Sources.Count > 0
                                                       ? input.Sources
                                                       : throw new ArgumentException(
                                                           "ImagePreviewInput.Sources cannot be empty", nameof(input));

      ImagePreviewDialogSession session = new()
      {
        Title = input.Title ?? Lang.Dialog.ImagePreview.Title,
        Sources = sourceList,
        CurrentIndex = Math.Clamp(input.StartIndex, 0, sourceList.Count - 1),
        HttpClient = input.HttpClient
      };

      await router.ShowAsync(session);
    }
  }
}
