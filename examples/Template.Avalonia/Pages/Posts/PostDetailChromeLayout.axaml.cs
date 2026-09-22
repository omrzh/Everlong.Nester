using Avalonia.Controls;
using Everlong.Nester.Presentation;

namespace NesterApp.Pages.Posts;

[ViewFor<PostDetailChromeLayoutModel>]
public partial class PostDetailChromeLayout : UserControl, ILayoutControl
{
  public PostDetailChromeLayout()
  {
    InitializeComponent();
  }

  public ILayoutBody GetLayoutBody() => Body;
}
