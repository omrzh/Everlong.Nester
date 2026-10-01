using System.Windows.Controls;
using Everlong.Nester.Presentation;

namespace NesterApp.Pages.Settings;

[ViewFor<SettingsLayoutModel>]
public partial class SettingsLayout : UserControl, IBodyHolder, IPassThroughTransition
{
  public SettingsLayout()
  {
    InitializeComponent();
  }

  public IBodyPanel GetBodyPanel() => Body;
}
