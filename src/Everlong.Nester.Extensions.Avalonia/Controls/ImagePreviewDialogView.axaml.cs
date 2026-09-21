using Avalonia.Controls;
using Everlong.Nester.Dialog;
using Everlong.Nester.Presentation;
using Everlong.Nester.Routing;

namespace Everlong.Nester.Controls;

/// <summary>
///   A view for displaying the image preview dialog.
/// </summary>
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
  /// <remarks>Installs the Avalonia image decoder on the session when it has none.</remarks>
  public Task OnArrivingAsync(IRoutingContext context)
  {
    if (DataContext is ImagePreviewDialogSession session)
    {
      session.Decoder ??= new AvaloniaImageDecoder();
    }

    return Task.CompletedTask;
  }

  /// <inheritdoc />
  public Task AnimateEnterAsync(TransitionContext context, CancellationToken token)
  {
    // FlyingCanvas covers the entire content area — fix Chrome size now so
    // images never resize the dialog.
    if (context.FlyingCanvas is { } plane)
    {
      ChromeBorder.Width = Math.Clamp(plane.Bounds.Width * 0.85, 800, 1280);
      ChromeBorder.Height = Math.Clamp(plane.Bounds.Height * 0.78, 600, 900);
    }

    return Task.CompletedTask;
  }

  /// <inheritdoc />
  public Task AnimateExitAsync(TransitionContext context, CancellationToken token) => Task.CompletedTask;
}
