using Avalonia.Controls.Shapes;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Media;
using Everlong.Nester.Dialog;

namespace Everlong.Nester.Presentation;

/// <summary>
///   A view for displaying the signature pad dialog.
/// </summary>
public partial class SignaturePadDialogView : UserControl, ISceneTransition
{
  private readonly ISceneTransition _transition = new SlideFromBottomTransition();
  private bool _isDrawing;
  private Polyline? _activePolyline;

  /// <summary>
  ///   Initializes a new instance of the <see cref="SignaturePadDialogView" /> class.
  /// </summary>
  public SignaturePadDialogView()
  {
    InitializeComponent();
  }

  private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
  {
    if (!e.GetCurrentPoint(SignatureCanvas).Properties.IsLeftButtonPressed)
    {
      return;
    }

    if (_isDrawing)
    {
      EndStroke();
    }

    StartStroke(e.GetPosition(SignatureCanvas));
    e.Pointer.Capture(SignatureCanvas);
    e.Handled = true;
  }

  private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
  {
    if (!_isDrawing)
    {
      return;
    }

    ContinueStroke(e.GetPosition(SignatureCanvas));
    e.Handled = true;
  }

  private void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
  {
    if (!_isDrawing)
    {
      return;
    }

    ContinueStroke(e.GetPosition(SignatureCanvas));
    EndStroke();
    e.Pointer.Capture(null);
    e.Handled = true;
  }

  private void OnCanvasPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
  {
    if (_isDrawing)
    {
      EndStroke();
    }
  }

  private void OnClearClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
  {
    SignatureCanvas.Children.Clear();
    _isDrawing = false;
    _activePolyline = null;
  }

  private void OnConfirmClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
  {
    if (DataContext is not SignaturePadDialogSession session || !session.CanSubmit)
    {
      return;
    }

    var width = Math.Max(1, (int)Math.Ceiling(SignatureCanvas.Bounds.Width));
    var height = Math.Max(1, (int)Math.Ceiling(SignatureCanvas.Bounds.Height));
    var bytes = ExportCanvasAsPng(SignatureCanvas, width, height);
    session.Confirm(bytes, width, height);
  }

  private void StartStroke(Avalonia.Point point)
  {
    if (DataContext is not SignaturePadDialogSession session)
    {
      return;
    }

    // Explicitly initialize Points to a new owned collection.
    // Polyline.Points defaults to null; calling Add on null throws and silently kills drawing.
    _activePolyline = new Polyline
    {
      Stroke = Brushes.Black,
      StrokeThickness = 2.5,
      IsHitTestVisible = false,
      StrokeLineCap = PenLineCap.Round,
      StrokeJoin = PenLineJoin.Round,
      Points = [point]
    };

    SignatureCanvas.Children.Add(_activePolyline);
    session.BeginStroke(new SignaturePoint(point.X, point.Y));
    _isDrawing = true;
  }

  private void ContinueStroke(Avalonia.Point point)
  {
    if (!_isDrawing || _activePolyline is null || DataContext is not SignaturePadDialogSession session)
    {
      return;
    }

    _activePolyline.Points.Add(point);
    // AffectsGeometry only fires on property-reference change, not on collection mutation;
    // force geometry recalculation manually.
    _activePolyline.InvalidateMeasure();
    session.AppendStrokePoint(new SignaturePoint(point.X, point.Y));
  }

  private void EndStroke()
  {
    if (DataContext is SignaturePadDialogSession session)
    {
      session.EndStroke();
    }

    _isDrawing = false;
    _activePolyline = null;
  }

  private static byte[] ExportCanvasAsPng(Control target, int width, int height)
  {
    using var bitmap = new RenderTargetBitmap(new Avalonia.PixelSize(width, height));
    bitmap.Render(target);
    using var stream = new MemoryStream();
    bitmap.Save(stream, PngBitmapEncoderOptions.Default);
    return stream.ToArray();
  }
  Task ISceneTransition.AnimateEnterAsync(TransitionContext ctx, CancellationToken token) => _transition.AnimateEnterAsync(ctx, token);
  Task ISceneTransition.AnimateExitAsync(TransitionContext ctx, CancellationToken token) => _transition.AnimateExitAsync(ctx, token);
}
