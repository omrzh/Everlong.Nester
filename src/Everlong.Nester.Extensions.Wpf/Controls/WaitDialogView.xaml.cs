using System.ComponentModel;
using System.Windows.Controls;
using Everlong.Nester.Presentation;

namespace Everlong.Nester.Controls;

/// <summary>
///   A view for displaying a wait dialog.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public partial class WaitDialogView : UserControl, ISceneTransition
{
  /// <summary>
  ///   Initializes a new instance of the <see cref="WaitDialogView" /> class.
  /// </summary>
  public WaitDialogView()
  {
    InitializeComponent();
  }
  private readonly ISceneTransition _transition = new SlideFromBottomTransition();

  Task ISceneTransition.AnimateEnterAsync(TransitionContext ctx, CancellationToken token)
      => _transition.AnimateEnterAsync(ctx, token);
  Task ISceneTransition.AnimateExitAsync(TransitionContext ctx, CancellationToken token)
      => _transition.AnimateExitAsync(ctx, token);
}
