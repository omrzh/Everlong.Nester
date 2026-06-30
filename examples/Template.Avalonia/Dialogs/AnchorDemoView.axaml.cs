using Everlong.Nester.Presentation;
using Avalonia.Controls;
namespace NesterApp.Dialogs;

[ViewFor<AnchorDemoSession>]
public partial class AnchorDemoView : UserControl, ISceneTransition
{
  private readonly ISceneTransition _transition = new SlideFromBottomTransition();

  public AnchorDemoView()
  {
    InitializeComponent();
  }

  Task ISceneTransition.AnimateEnterAsync(TransitionContext ctx, CancellationToken token)
    => _transition.AnimateEnterAsync(ctx, token);

  Task ISceneTransition.AnimateExitAsync(TransitionContext ctx, CancellationToken token)
    => _transition.AnimateExitAsync(ctx, token);
}
