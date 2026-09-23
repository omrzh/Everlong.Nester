using System.Windows.Controls;

namespace Everlong.Nester.Presentation;

/// <summary>
///   A view for displaying the IPv4 compose dialog.
/// </summary>
public partial class Ipv4ComposeDialogView : UserControl, ISceneTransition
{
  /// <summary>
  ///   Initializes a new instance of the <see cref="Ipv4ComposeDialogView" /> class.
  /// </summary>
  public Ipv4ComposeDialogView()
  {
    InitializeComponent();
  }
  private readonly ISceneTransition _transition = new SlideFromBottomTransition();

  Task ISceneTransition.AnimateEnterAsync(TransitionContext ctx, CancellationToken token)
      => _transition.AnimateEnterAsync(ctx, token);
  Task ISceneTransition.AnimateExitAsync(TransitionContext ctx, CancellationToken token)
      => _transition.AnimateExitAsync(ctx, token);
}
