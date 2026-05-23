using Everlong.Nester.ComponentModel;
using System.Globalization;
using System.Text;
using Everlong.Nester.Extensions.Properties;

using Everlong.Nester.Routing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Everlong.Nester.Dialog;

/// <summary>
///   Represents a point in the signature coordinate space.
/// </summary>
/// <param name="X">The X coordinate.</param>
/// <param name="Y">The Y coordinate.</param>
public readonly record struct SignaturePoint(double X, double Y);

/// <summary>
///   Represents the result payload produced by a signature pad dialog.
/// </summary>
/// <param name="ImageBytes">The rasterized PNG image bytes.</param>
/// <param name="SvgPathData">The SVG path data generated from captured strokes.</param>
/// <param name="PixelWidth">The exported image width in pixels.</param>
/// <param name="PixelHeight">The exported image height in pixels.</param>
/// <param name="StrokeCount">The number of captured strokes.</param>
public record SignNameResult(
  byte[]? ImageBytes,
  string SvgPathData,
  int PixelWidth,
  int PixelHeight,
  int StrokeCount);

/// <summary>
///   Dialog session for capturing a handwritten signature.
/// </summary>
public partial class SignaturePadDialogSession : DialogSessionBase<SignNameResult>
{
  private readonly List<List<SignaturePoint>> _strokes = [];

  private List<SignaturePoint>? _activeStroke;
  /// <summary>Creates a session with no captured strokes.</summary>
  public SignaturePadDialogSession() { }

  /// <summary>
  ///   Gets or sets the heading shown above the pad.
  /// </summary>
  [ObservableProperty] public partial string? Title { get; set; }

  /// <summary>
  ///   Gets or sets the instruction text shown above the pad.
  /// </summary>
  [ObservableProperty] public partial string? Message { get; set; }

  /// <summary>
  ///   Gets the number of strokes that have been captured in the signature.
  /// </summary>
  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(CanSubmit))]
  [NotifyPropertyChangedFor(nameof(StrokeCountText))]
  public partial int StrokeCount { get; private set; }

  /// <summary>Gets the caption under the pad, stating how many strokes were captured.</summary>
  public string StrokeCountText => Lang.Dialog.SignaturePad.FormatStrokeCount(StrokeCount);

  /// <summary>
  ///   Gets or sets the text displayed on the confirm button.
  /// </summary>
  [ObservableProperty] public required partial string ConfirmText { get; set; }

  /// <summary>
  ///   Gets or sets the text displayed on the clear/re-sign button.
  /// </summary>
  [ObservableProperty] public required partial string ClearText { get; set; }

  /// <summary>
  ///   Gets a value indicating whether the current signature can be submitted.
  /// </summary>
  public bool CanSubmit => StrokeCount > 0;

  /// <summary>
  ///   Starts a new stroke with the given starting point.
  /// </summary>
  /// <param name="point">The starting point of the stroke.</param>
  public void BeginStroke(SignaturePoint point)
  {
    _activeStroke = [point];
    _strokes.Add(_activeStroke);
    SyncStrokeCount();
  }

  /// <summary>
  ///   Appends a point to the current stroke.
  /// </summary>
  /// <param name="point">The next point of the stroke.</param>
  public void AppendStrokePoint(SignaturePoint point)
  {
    if (_activeStroke is null)
    {
      BeginStroke(point);
      return;
    }

    _activeStroke.Add(point);
  }

  /// <summary>
  ///   Ends the current stroke.
  /// </summary>
  public void EndStroke()
  {
    _activeStroke = null;
  }

  /// <summary>
  ///   Clears all captured strokes.
  /// </summary>
  [RelayCommand]
  public void Clear()
  {
    _activeStroke = null;
    _strokes.Clear();
    SyncStrokeCount();
  }

  /// <summary>
  ///   Cancels signature input and closes the dialog without a result.
  /// </summary>
  [RelayCommand]
  public void Cancel()
  {
    Close();
  }

  /// <summary>
  ///   Confirms signature input and closes the dialog with result data.
  /// </summary>
  /// <param name="imageBytes">The exported PNG image bytes.</param>
  /// <param name="width">The export width in pixels.</param>
  /// <param name="height">The export height in pixels.</param>
  public void Confirm(byte[]? imageBytes, int width, int height)
  {
    if (!CanSubmit)
    {
      return;
    }

    string svgPathData = BuildSvgPathData();
    SignNameResult result = new(imageBytes, svgPathData, width, height, StrokeCount);
    Close(result);
  }

  private void SyncStrokeCount()
  {
    StrokeCount = _strokes.Count;
  }

  private string BuildSvgPathData()
  {
    if (_strokes.Count == 0)
    {
      return string.Empty;
    }

    StringBuilder builder = new();
    for (int i = 0; i < _strokes.Count; i++)
    {
      List<SignaturePoint> stroke = _strokes[i];
      if (stroke.Count == 0)
      {
        continue;
      }

      AppendMove(builder, stroke[0]);
      if (stroke.Count == 1)
      {
        AppendLine(builder, stroke[0]);
      }
      else
      {
        for (int j = 1; j < stroke.Count; j++)
        {
          AppendLine(builder, stroke[j]);
        }
      }

      if (i < _strokes.Count - 1)
      {
        builder.Append(' ');
      }
    }

    return builder.ToString();
  }

  private static void AppendMove(StringBuilder builder, SignaturePoint point)
  {
    builder.Append('M');
    builder.Append(point.X.ToString("0.###", CultureInfo.InvariantCulture));
    builder.Append(' ');
    builder.Append(point.Y.ToString("0.###", CultureInfo.InvariantCulture));
  }

  private static void AppendLine(StringBuilder builder, SignaturePoint point)
  {
    builder.Append(" L");
    builder.Append(point.X.ToString("0.###", CultureInfo.InvariantCulture));
    builder.Append(' ');
    builder.Append(point.Y.ToString("0.###", CultureInfo.InvariantCulture));
  }
}

/// <summary>
///   Options for <see cref="SignaturePadDialogSession" />.
/// </summary>
public sealed class SignaturePadOptions : DialogOptions
{
  /// <summary>Gets or sets the override for the confirm button text.</summary>
  public string ConfirmText { get; init; } = Lang.Dialog.SignaturePad.ConfirmText;

  /// <summary>Gets or sets the override for the clear/re-sign button text.</summary>
  public string ClearText { get; init; } = Lang.Dialog.SignaturePad.ClearText;
}

/// <summary>
///   Extension methods for showing a signature pad dialog using the <see cref="IRouter" />.
/// </summary>
public static partial class DialogSessionExtensions
{
  extension(IRouter router)
  {
    /// <summary>
    ///   Shows a signature pad dialog allowing the user to draw a handwritten signature and returns the result.
    /// </summary>
    /// <param name="title">The title displayed in the dialog.</param>
    /// <param name="message">The instruction text displayed above the signature area.</param>
    /// <param name="sessionOptions">Optional string overrides for button labels.</param>
    /// <returns>
    ///   A task that resolves to <see cref="SignNameResult" /> when confirmed;
    ///   returns <see langword="null" /> when canceled.
    /// </returns>
    public Task<SignNameResult?> SignHandwrittenNameAsync(string? title = null,
                                                          string? message = null,
                                                          SignaturePadOptions? sessionOptions = null)
    {
      sessionOptions ??= new SignaturePadOptions();
      SignaturePadDialogSession session = new()
      {
        Title = title ?? Lang.Dialog.SignaturePad.Title,
        Message = message,
        ConfirmText = sessionOptions.ConfirmText,
        ClearText = sessionOptions.ClearText
      };

      return router.ShowAsync<SignNameResult?>(session, sessionOptions.ToDimmer());
    }
  }
}
