using Everlong.Nester.Dialog;
using Everlong.Nester.Presentation;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace NesterApp.Dialogs;

/// <summary>
///   The confirm-dialog view — a centered box presenting a
///   <see cref="ConfirmDialogSession" /> (or any session carrying the same
///   confirm/cancel commands).
/// </summary>
public sealed class ConfirmDialogView : NesterView
{
  private readonly Label _titleLabel;
  private readonly Label _messageLabel;
  private readonly Button _confirmButton;
  private readonly Button _cancelButton;

  public ConfirmDialogView()
  {
    StretchToBody = false;
    FloatingSize = new TerminalSurfaceSize(60, 11);
    Width = 60;
    Height = 11;
    X = Pos.Center();
    Y = Pos.Center();
    BorderStyle = LineStyle.Single;

    _titleLabel = new Label { X = 2, Y = 0 };
    _messageLabel = new Label { X = 2, Y = 2, Width = 56 };
    _confirmButton = new Button { X = Pos.Center() - 8, Y = 7 };
    _cancelButton = new Button { X = Pos.Center() + 8, Y = 7 };

    Add(_titleLabel, _messageLabel, _confirmButton, _cancelButton);

    _confirmButton.Accepted += (_, _) => Invoke(vm => vm.ConfirmCommand.Execute(null));
    _cancelButton.Accepted += (_, _) => Invoke(vm => vm.CancelCommand.Execute(null));
  }

  private void Invoke(Action<ConfirmDialogSession> action)
  {
    if (DataContext is ConfirmDialogSession vm)
      action(vm);
  }

  protected override void OnDataContextChanged()
  {
    if (DataContext is not ConfirmDialogSession vm)
      return;

    _titleLabel.Text = vm.Title ?? string.Empty;
    _messageLabel.Text = vm.Message ?? string.Empty;
    _confirmButton.Text = vm.ConfirmText;
    _cancelButton.Text = vm.CancelText;
    _confirmButton.Enabled = vm.IsConfirmEnabled;
    _confirmButton.SetFocus();
  }
}
