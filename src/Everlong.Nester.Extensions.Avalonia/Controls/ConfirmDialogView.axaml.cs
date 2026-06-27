using Avalonia.Controls;
using Everlong.Nester.Presentation;

namespace Everlong.Nester.Controls;

/// <summary>
///   A view for displaying the confirm dialog.
/// </summary>
public partial class ConfirmDialogView : UserControl, ISceneTransition
{
  /// <summary>
  ///   Initializes a new instance of the <see cref="ConfirmDialogView" /> class.
  /// </summary>
  public ConfirmDialogView()
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
