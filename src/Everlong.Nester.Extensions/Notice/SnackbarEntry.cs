using System.Windows.Input;

namespace Everlong.Nester.Notice;

/// <summary>
///   Represents a snackbar notification session.
/// </summary>
public sealed class SnackbarEntry : NoticeEntryBase<SnackbarResult>
{
  private string? _actionText;

  /// <summary>
  ///   Creates a snackbar entry with its action command wired.
  /// </summary>
  public SnackbarEntry()
  {
    ActionCommand = new NoticeCommand(Action);
  }

  /// <summary>
  ///   The text for the action button.
  /// </summary>
  public string? ActionText
  {
    get => _actionText;
    set => SetProperty(ref _actionText, value);
  }

  /// <summary>
  ///   The task that completes when the snackbar is dismissed.
  /// </summary>
  public Task<SnackbarResult> ShowTask => CompletionTask;

  /// <summary>
  ///   Command to invoke the snackbar action and return the action-invoked result.
  /// </summary>
  public ICommand ActionCommand { get; }

  /// <summary>
  ///   Invokes the snackbar action, settling the entry with the action-invoked result.
  /// </summary>
  private void Action()
  {
    Dismiss(SnackbarResult.ActionInvoked);
  }

  /// <summary>
  ///   Dismisses the snackbar.
  /// </summary>
  public void Dismiss(SnackbarResult result = SnackbarResult.Dismissed)
  {
    Complete(result);
  }

  /// <summary>
  ///   Gets the result used when the snackbar times out.
  /// </summary>
  /// <returns>The timeout result.</returns>
  protected override SnackbarResult GetTimeoutResult()
  {
    return SnackbarResult.TimedOut;
  }

  /// <summary>
  ///   Gets the result used when the snackbar is dismissed.
  /// </summary>
  /// <returns>The dismiss result.</returns>
  protected override SnackbarResult GetDismissResult()
  {
    return SnackbarResult.Dismissed;
  }
}
