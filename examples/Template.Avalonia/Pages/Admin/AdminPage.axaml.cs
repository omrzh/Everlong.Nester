using Everlong.Nester.Presentation;
using Avalonia.Controls;
namespace NesterApp.Pages.Admin;

[ViewFor<AdminPageModel>]
public partial class AdminPage : UserControl, ISceneTransition
{
  public AdminPage() => InitializeComponent();

  public Task AnimateEnterAsync(TransitionContext context, CancellationToken token)
  {
    return context.EnterWithSlideAsync(SlideDirection.BottomToTop, 100, 100, token);
  }

  public Task AnimateExitAsync(TransitionContext context, CancellationToken token)
    => Task.CompletedTask;
}
