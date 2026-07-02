using Everlong.Nester.Presentation;
using System.Windows.Controls;
namespace NesterApp.Dialogs;

[ViewFor<MyConfirmDialogSession>]
public partial class MyConfirmDialog : UserControl, ISceneTransition
{
  public MyConfirmDialog()
  {
    InitializeComponent();
  }

  public Task AnimateEnterAsync(TransitionContext context, CancellationToken token)
  {
    context.RevealBefore(this);
    return this.ZoomInAsync(1.1d, 100, token);
  }

  public Task AnimateExitAsync(TransitionContext context, CancellationToken token)
  {
    return this.ZoomOutAsync(0.9d, 100, token);
  }
}
