using System.ComponentModel;
using System.Windows.Controls;

namespace Everlong.Nester.Presentation;

/// <summary>
///   A view for displaying the choice dialog.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public partial class ChoiceDialogView : UserControl, ISceneTransition
{
  /// <summary>
  ///   Initializes a new instance of the <see cref="ChoiceDialogView" /> class.
  /// </summary>
  public ChoiceDialogView()
  {
    InitializeComponent();
  }
  /// <inheritdoc />
  public Task AnimateEnterAsync(TransitionContext context, CancellationToken token)
  {
    const int durationMs = 200;
    return Task.WhenAll(
      this.SlideInAsync(SlideDirection.BottomToTop, 40, durationMs, token),
      this.FadeInAsync(durationMs, token));
  }

  /// <inheritdoc />
  public Task AnimateExitAsync(TransitionContext context, CancellationToken token)
  {
    const int durationMs = 200;
    return Task.WhenAll(
      this.SlideOutAsync(SlideDirection.BottomToTop, 40, durationMs, token),
      this.FadeOutAsync(durationMs, token));
  }
}
