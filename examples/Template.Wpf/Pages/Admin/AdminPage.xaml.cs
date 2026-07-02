using System.Windows;
using System.Windows.Controls;
using Everlong.Nester.Presentation;

namespace NesterApp.Pages.Admin;

[ViewFor<AdminPageModel>]
public partial class AdminPage : UserControl
{
  public AdminPage()
  {
    HierarchicalDataTemplate template = new(typeof(AdminPage))
    {
      VisualTree = new FrameworkElementFactory(typeof(TextBlock))
    };
    InitializeComponent();
  }
}
