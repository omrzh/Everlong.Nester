using System.Windows.Controls;
using Everlong.Nester.Presentation;
namespace NesterApp.Dialogs;

[ViewFor<NestedConfirmDialogSession>]
public partial class NestedConfirmDialog : UserControl
{
  public NestedConfirmDialog()
  {
    InitializeComponent();
  }
}
