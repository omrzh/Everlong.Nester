using Avalonia.Controls;
using Everlong.Nester.Presentation;

namespace NesterApp.Pages.Posts;

[ViewFor<PostDetailChromeLayoutModel>]
public partial class PostDetailChromeLayout : UserControl, IBodyHolder, IPassThroughTransition
{
  public PostDetailChromeLayout()
  {
    InitializeComponent();
  }

  public IBodyPanel GetBodyPanel() => Body;
}
