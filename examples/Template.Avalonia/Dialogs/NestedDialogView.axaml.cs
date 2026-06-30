using Avalonia.Controls;
using Everlong.Nester.Presentation;

namespace NesterApp.Dialogs;

[ViewFor<NestedConfirmDialogSession>]
public partial class NestedDialogView : UserControl
{
  public NestedDialogView()
  {
    InitializeComponent();
  }
}
