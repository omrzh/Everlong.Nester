using Everlong.Nester.Presentation;
using System.Windows.Controls;
namespace NesterApp.Pages.Settings;

[ViewFor<SettingsPageModel>]
public partial class SettingsPage : UserControl, ISceneTransition
{
  public SettingsPage()
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
