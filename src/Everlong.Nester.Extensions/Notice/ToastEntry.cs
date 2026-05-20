using System.Windows.Input;

namespace Everlong.Nester.Notice;

/// <summary>
///   Represents a toast notification.
/// </summary>
public sealed class ToastEntry : NoticeEntryBase<ToastResult>
{
  /// <summary>
  ///   Creates a toast entry with its dismiss command wired.
  /// </summary>
  public ToastEntry()
  {
    DismissCommand = new NoticeCommand(Dismiss);
  }

  /// <summary>
  ///   The type of the toast.
  /// </summary>
  public ToastLevel Level
  {
    get;
    set => SetProperty(ref field, value);
  } = ToastLevel.Information;

  /// <summary>
  ///   The task that completes when the toast is dismissed.
  /// </summary>
  public Task<ToastResult> ShowTask => CompletionTask;

  /// <summary>
  ///   Command to dismiss the toast.
  /// </summary>
  public ICommand DismissCommand { get; }

  /// <summary>
  ///   Dismisses the toast.
  /// </summary>
  public void Dismiss()
  {
    Complete(ToastResult.Dismissed);
  }

  /// <summary>
  ///   Gets the result used when the toast times out.
  /// </summary>
  /// <returns>The timeout result.</returns>
  protected override ToastResult GetTimeoutResult()
  {
    return ToastResult.TimedOut;
  }

  /// <summary>
  ///   Gets the result used when the toast is dismissed.
  /// </summary>
  /// <returns>The dismiss result.</returns>
  protected override ToastResult GetDismissResult()
  {
    return ToastResult.Dismissed;
  }
}
