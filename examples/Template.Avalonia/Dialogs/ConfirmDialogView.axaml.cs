using Avalonia.Controls;
using Everlong.Nester.Presentation;

namespace NesterApp.Dialogs;

[ViewFor<MyConfirmDialogSession>]
public partial class ConfirmDialogView : UserControl
{
  public ConfirmDialogView()
  {
    InitializeComponent();
  }
}
