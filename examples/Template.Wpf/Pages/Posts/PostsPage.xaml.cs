using Everlong.Nester.Presentation;
using System.Windows.Controls;
namespace NesterApp.Pages.Posts;

[ViewFor<PostsPageModel>]
public partial class PostsPage : UserControl, ISceneTransition
{
  public PostsPage()
  {
    InitializeComponent();
  }

  public Task AnimateEnterAsync(TransitionContext context, CancellationToken token)
  {
    return context.EnterWithSlideAsync(SlideDirection.BottomToTop, 50, 100, token);
  }

  public Task AnimateExitAsync(TransitionContext context, CancellationToken token)
  {
    return context.ExitWithSlideAsync(SlideDirection.TopToBottom, 50, 100, token);
  }

}
