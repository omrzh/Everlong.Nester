using Everlong.Nester.Presentation;
using System.Windows.Controls;
namespace NesterApp.Pages.Labs;

[ViewFor<InteractionLabPageModel>]
public partial class InteractionLabPage : UserControl, ISceneTransition
{
  public InteractionLabPage()
  {
    InitializeComponent();
  }

  public Task AnimateEnterAsync(TransitionContext context, CancellationToken token)
  {
    return context.EnterWithSlideAsync(SlideDirection.RightToLeft, 100, 100, token);
  }

  public Task AnimateExitAsync(TransitionContext context, CancellationToken token)
  {
    return context.ExitWithSlideAsync(SlideDirection.RightToLeft, 100, 100, token);
  }
}
