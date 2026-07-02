using System.Windows.Controls;
using Everlong.Nester.Presentation;

namespace NesterApp.Pages.Profile;

[ViewFor<ProfilePageModel>]
public partial class ProfilePage : UserControl
{
  public ProfilePage()
  {
    InitializeComponent();
  }
}
