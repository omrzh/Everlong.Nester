using System.ComponentModel;
using System.IO;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using Everlong.Nester.Dialog;

namespace Everlong.Nester.Presentation;

/// <summary>
///   A view for displaying the signature pad dialog.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public partial class SignaturePadDialogView : UserControl, ISceneTransition
{
  /// <summary>
  ///   Initializes a new instance of the <see cref="SignaturePadDialogView" /> class.
  /// </summary>
  public SignaturePadDialogView()
  {
    InitializeComponent();
    SignatureCanvas.DefaultDrawingAttributes = new DrawingAttributes
    {
      Color = Colors.Black,
      Width = 2.5,
      Height = 2.5,
      FitToCurve = false,
      IgnorePressure = false
    };
  }

  private void OnClearClick(object sender, RoutedEventArgs e)
  {
    SignatureCanvas.Strokes.Clear();
    if (DataContext is SignaturePadDialogSession session)
    {
      session.Clear();
    }
  }

  private void OnStrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
  {
    if (DataContext is not SignaturePadDialogSession session)
    {
      return;
    }

    var points = e.Stroke.StylusPoints;
    if (points.Count == 0)
    {
      return;
    }

    session.BeginStroke(new SignaturePoint(points[0].X, points[0].Y));
    for (var i = 1; i < points.Count; i++)
    {
      session.AppendStrokePoint(new SignaturePoint(points[i].X, points[i].Y));
    }

    session.EndStroke();
  }

  private void OnConfirmClick(object sender, RoutedEventArgs e)
  {
    if (DataContext is not SignaturePadDialogSession session || !session.CanSubmit)
    {
      return;
    }

    var width = Math.Max(1, (int)Math.Ceiling(SignatureCanvas.ActualWidth));
    var height = Math.Max(1, (int)Math.Ceiling(SignatureCanvas.ActualHeight));
    var bytes = ExportInkCanvasAsPng(SignatureCanvas, width, height);
    session.Confirm(bytes, width, height);
  }

  private static byte[] ExportInkCanvasAsPng(InkCanvas target, int width, int height)
  {
    var visual = new DrawingVisual();
    using (var context = visual.RenderOpen())
    {
      context.DrawRectangle(Brushes.White, null, new PRect(0, 0, width, height));
      context.DrawRectangle(new VisualBrush(target), null, new PRect(0, 0, width, height));
    }

    var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
    bitmap.Render(visual);

    var encoder = new PngBitmapEncoder();
    encoder.Frames.Add(BitmapFrame.Create(bitmap));

    using var stream = new MemoryStream();
    encoder.Save(stream);
    return stream.ToArray();
  }

  private readonly ISceneTransition _transition = new SlideFromBottomTransition();
  Task ISceneTransition.AnimateEnterAsync(TransitionContext ctx, CancellationToken token)
        => _transition.AnimateEnterAsync(ctx, token);
  Task ISceneTransition.AnimateExitAsync(TransitionContext ctx, CancellationToken token)
      => _transition.AnimateExitAsync(ctx, token);
}
