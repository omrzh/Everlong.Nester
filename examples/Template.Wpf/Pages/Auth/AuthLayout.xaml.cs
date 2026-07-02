using System.Windows.Controls;
using Everlong.Nester.Presentation;

namespace NesterApp.Pages.Auth;

[ViewFor<AuthLayoutModel>]
public partial class AuthLayout : UserControl, ILayoutControl
{
  public AuthLayout()
  {
    InitializeComponent();
  }

  public ILayoutBody GetLayoutBody()
  {
    return Body;
  }
}
