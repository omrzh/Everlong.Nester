using Everlong.Nester.Presentation;
using Avalonia.Controls;
namespace NesterApp.Pages.Landing;

[ViewFor<LandingPageModel>]
public partial class LandingPage : UserControl, ISceneTransition
{
  public LandingPage() => InitializeComponent();

  public Task AnimateEnterAsync(TransitionContext context, CancellationToken token)
  {
    return context.EnterWithSlideAsync(SlideDirection.BottomToTop, 50, 100, token);
  }

  public Task AnimateExitAsync(TransitionContext context, CancellationToken token)
    => context.ExitWithSlideAsync(SlideDirection.TopToBottom, 50, 100, token);
}
