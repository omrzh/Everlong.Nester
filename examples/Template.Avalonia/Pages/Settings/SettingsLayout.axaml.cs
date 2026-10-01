using Avalonia.Controls;
using Everlong.Nester.Presentation;

namespace NesterApp.Pages.Settings;

[ViewFor<SettingsLayoutModel>]
public partial class SettingsLayout : UserControl, IBodyHolder, ISceneTransition
{
  public SettingsLayout()
  {
    InitializeComponent();
  }

  public IBodyPanel GetBodyPanel() => Body;

  public Task AnimateEnterAsync(TransitionContext context, CancellationToken token)
    => this.PassThroughAsync(context, token);

  public Task AnimateExitAsync(TransitionContext context, CancellationToken token)
    => this.PassExitAsync(context, token);
}
