using Everlong.Nester.Presentation;
using Avalonia.Controls;
namespace NesterApp.Pages.Profile;

[ViewFor<ProfilePageModel>]
public partial class ProfilePage : UserControl, ISceneTransition
{
  public ProfilePage() => InitializeComponent();

  public Task AnimateEnterAsync(TransitionContext context, CancellationToken token)
  {
    return context.EnterWithFadeAsync(167, token);
  }

  public Task AnimateExitAsync(TransitionContext context, CancellationToken token)
    => context.ExitWithFadeAsync(167, token);
}
