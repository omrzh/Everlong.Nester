using Avalonia;
using Avalonia.Controls;
using Everlong.Nester.Presentation;

namespace NesterApp.Pages.Auth;

[ViewFor<AuthLayoutModel>]
public partial class AuthLayout : UserControl, ILayoutControl
{
  public AuthLayout() => InitializeComponent();

  public ILayoutBody GetLayoutBody() => Body;

  protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
  {
    base.OnAttachedToVisualTree(e);


  }
}
