using Avalonia;
using Avalonia.Controls;
using Everlong.Nester.Presentation;

namespace NesterApp.Pages.Auth;

[ViewFor<AuthLayoutModel>]
public partial class AuthLayout : UserControl, IBodyHolder
{
  public AuthLayout() => InitializeComponent();

  public IBodyPanel GetBodyPanel() => Body;

  protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
  {
    base.OnAttachedToVisualTree(e);
  }
}
