using Everlong.Nester.Presentation;
using System.Windows.Controls;
namespace NesterApp.Dialogs;

[ViewFor<AnchorDemoSession>]
public partial class AnchorDemoDialog : UserControl, ISceneTransition
{
  public AnchorDemoDialog()
  {
    InitializeComponent();
  }

  public Task AnimateEnterAsync(TransitionContext ctx, CancellationToken token)
  {
    ctx.RevealBefore(this);
    return this.SlideInAsync(SlideDirection.BottomToTop, 100, 100, token);
  }

  public Task AnimateExitAsync(TransitionContext ctx, CancellationToken token)
  {
    return this.SlideOutAsync(SlideDirection.TopToBottom, 100, 100, token);
  }
}
