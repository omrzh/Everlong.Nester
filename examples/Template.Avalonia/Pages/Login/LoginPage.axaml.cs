using Avalonia.Controls;
using Everlong.Nester.Presentation;

namespace NesterApp.Pages.Login;

[ViewFor<LoginPageModel>]
public partial class LoginPage : UserControl
{
  public LoginPage()
  {
    InitializeComponent();
  }
}
