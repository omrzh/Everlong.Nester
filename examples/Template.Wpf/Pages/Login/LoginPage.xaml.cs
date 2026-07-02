using System.Windows.Controls;
using Everlong.Nester.Presentation;

namespace NesterApp.Pages.Login;

[ViewFor<LoginPageModel>]
public partial class LoginPage : UserControl
{
  public LoginPage()
  {
    InitializeComponent();
    ListenToTextBox();
  }

  private void ListenToTextBox()
  {
    PwdBox.PasswordChanged += (s, e) =>
    {
      if (DataContext is LoginPageModel vm)
      {
        vm.Password = PwdBox.Password;
      }
    };
  }
}
