using System.Windows.Input;

namespace Everlong.Nester.Notice;

/// <summary>
///   An always-enabled <see cref="ICommand" /> that invokes a delegate.
/// </summary>
internal sealed class NoticeCommand(Action execute) : ICommand
{
  /// <inheritdoc />
  event EventHandler? ICommand.CanExecuteChanged
  {
    // The command is always enabled, so the contract event never fires.
    add { }
    remove { }
  }

  /// <inheritdoc />
  public bool CanExecute(object? parameter) => true;

  /// <inheritdoc />
  public void Execute(object? parameter)
  {
    execute();
  }
}
