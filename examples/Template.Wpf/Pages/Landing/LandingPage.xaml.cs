using Everlong.Nester.Presentation;
using System.Windows.Controls;
namespace NesterApp.Pages.Landing;

[ViewFor<LandingPageModel>]
public partial class LandingPage : UserControl, ISceneTransition
{
  public LandingPage()
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
