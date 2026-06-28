using System.ComponentModel;
using System.Windows.Controls;
using Everlong.Nester.Dialog;
using Everlong.Nester.Presentation;
using Everlong.Nester.Routing;

namespace Everlong.Nester.Controls;

/// <summary>
///   A view for displaying the image preview dialog.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public partial class ImagePreviewDialogView : UserControl, IArriving, ISceneTransition
{
  /// <summary>
  ///   Initializes a new instance of the <see cref="ImagePreviewDialogView" /> class.
  /// </summary>
  public ImagePreviewDialogView()
  {
    InitializeComponent();
  }

  /// <inheritdoc />
  /// <remarks>Installs the WPF image decoder on the session when it has none.</remarks>
  public Task OnArrivingAsync(IRoutingContext context)
  {
    if (DataContext is ImagePreviewDialogSession session)
    {
      session.Decoder ??= new WpfImageDecoder();
    }

    return Task.CompletedTask;
  }

  /// <inheritdoc />
  public Task AnimateEnterAsync(TransitionContext context, CancellationToken token)
  {
    // FlyingCanvas covers the entire content area — fix Chrome size now so
    // images never resize the dialog.
    ChromeBorder.Width = Math.Clamp(context.FlyingCanvas.ActualWidth * 0.85, 800, 1280);
    ChromeBorder.Height = Math.Clamp(context.FlyingCanvas.ActualHeight * 0.78, 600, 900);
    context.FlyingCanvas.UpdateLayout();

    // WPF's Border.ClipToBounds does NOT clip to CornerRadius — the top bar
    // Grid (at VerticalAlignment="Top") bleeds into rounded corners without
    // an explicit geometry clip.
    double cr = ChromeBorder.CornerRadius.TopLeft;
    ChromeBorder.Clip = new System.Windows.Media.RectangleGeometry(
      new System.Windows.Rect(0, 0, ChromeBorder.ActualWidth, ChromeBorder.ActualHeight), cr, cr);

    return Task.CompletedTask;
  }

  /// <inheritdoc />
  public Task AnimateExitAsync(TransitionContext context, CancellationToken token) => Task.CompletedTask;
}
