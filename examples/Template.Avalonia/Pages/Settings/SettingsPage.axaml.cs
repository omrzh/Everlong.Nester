using Everlong.Nester.Presentation;
using Avalonia.Controls;
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
    var d = 100;
    return Task.WhenAll(
      context.ExitWithSlideAsync(SlideDirection.TopToBottom, 50, d, token),
      context.ExitWithFadeAsync(d, token));
  }
}
