using Avalonia.Controls;

namespace Everlong.Nester.Presentation;

/// <summary>
///   A view for displaying the color picker dialog.
/// </summary>
public partial class ColorPickerDialogView : UserControl, ISceneTransition
{
  private readonly ISceneTransition _transition = new SlideFromBottomTransition();

  /// <summary>
  ///   Initializes a new instance of the <see cref="ColorPickerDialogView" /> class.
  /// </summary>
  public ColorPickerDialogView()
  {
    InitializeComponent();
  }

  Task ISceneTransition.AnimateEnterAsync(TransitionContext ctx, CancellationToken token) => _transition.AnimateEnterAsync(ctx, token);
  Task ISceneTransition.AnimateExitAsync(TransitionContext ctx, CancellationToken token) => _transition.AnimateExitAsync(ctx, token);
}
