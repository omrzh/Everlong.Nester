using Avalonia.Controls;

namespace Everlong.Nester.Presentation;

/// <summary>
///   A view for displaying the alert dialog.
/// </summary>
public partial class AlertDialogView : UserControl, ISceneTransition
{
  /// <summary>
  ///   Initializes a new instance of the <see cref="AlertDialogView" /> class.
  /// </summary>
  public AlertDialogView()
  {
    InitializeComponent();
  }
  /// <inheritdoc />
  public Task AnimateEnterAsync(TransitionContext context, CancellationToken token)
  {
    const int durationMs = 200;
    return Task.WhenAll(
      this.SlideInAsync(SlideDirection.BottomToTop, 60, durationMs, token),
      this.FadeInAsync(durationMs, token));
  }

  /// <inheritdoc />
  public Task AnimateExitAsync(TransitionContext context, CancellationToken token)
  {
    const int durationMs = 200;
    return Task.WhenAll(
      this.SlideOutAsync(SlideDirection.BottomToTop, 60, durationMs, token),
      this.FadeOutAsync(durationMs, token));
  }
}
