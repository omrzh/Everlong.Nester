using Avalonia.Controls;

namespace Everlong.Nester.Presentation;

/// <summary>
///   A view for displaying a wait dialog.
/// </summary>
public partial class WaitDialogView : UserControl, ISceneTransition
{
  private readonly ISceneTransition _transition = new SlideFromBottomTransition();

  /// <summary>
  ///   Initializes a new instance of the <see cref="WaitDialogView" /> class.
  /// </summary>
  public WaitDialogView()
  {
    InitializeComponent();
  }
  Task ISceneTransition.AnimateEnterAsync(TransitionContext ctx, CancellationToken token) => _transition.AnimateEnterAsync(ctx, token);
  Task ISceneTransition.AnimateExitAsync(TransitionContext ctx, CancellationToken token) => _transition.AnimateExitAsync(ctx, token);
}
