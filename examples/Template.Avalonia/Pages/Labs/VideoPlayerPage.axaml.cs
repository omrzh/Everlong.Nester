using Everlong.Nester.Presentation;
using Avalonia.Controls;
namespace NesterApp.Pages.Labs;

// No [ViewFor<VideoPlayerViewModel>] here — the mapping is declared centrally on ViewLocator
// with [Mapping<VideoPlayerViewModel, VideoPlayerPage>] in ViewLocators.cs.
public partial class VideoPlayerPage : UserControl, ISceneTransition
{
  public VideoPlayerPage()
  {
    InitializeComponent();
  }

  public Task AnimateEnterAsync(TransitionContext context, CancellationToken token)
  {
    return context.EnterWithSlideAsync(SlideDirection.BottomToTop, 50, 100, token);
  }

  public Task AnimateExitAsync(TransitionContext context, CancellationToken token)
    => context.ExitWithSlideAsync(SlideDirection.BottomToTop, 50, 100, token);
}
