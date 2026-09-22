using System.Windows.Controls;
using Everlong.Nester.Presentation;

namespace NesterApp.Pages.Auth;

[ViewFor<AuthLayoutModel>]
public partial class AuthLayout : UserControl, IBodyHolder
{
  public AuthLayout()
  {
    InitializeComponent();
  }

  public IBodyPanel GetBodyPanel()
  {
    return Body;
  }
}
