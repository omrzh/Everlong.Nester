using Everlong.Nester.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NesterApp.Dialogs;

public partial class NestedConfirmDialogSession : DialogSessionBase<bool>
{
  public string? Message { get; set; }

  [RelayCommand] private void Ok() => Close(true);
}
