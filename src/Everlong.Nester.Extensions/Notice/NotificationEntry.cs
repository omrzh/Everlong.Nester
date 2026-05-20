using System.Windows.Input;

namespace Everlong.Nester.Notice;

/// <summary>
///   Represents a notification banner entry.
/// </summary>
public sealed class NotificationEntry : NoticeEntryBase<NotificationResult>
{
  private string? _title;
  private NotificationLevel _level = NotificationLevel.Information;
  private string? _actionText;
  private bool _isClosing;

  /// <summary>
  ///   Creates a notification entry with its dismiss and action commands wired.
  /// </summary>
  public NotificationEntry()
  {
    DismissCommand = new NoticeCommand(Dismiss);
    ActionCommand = new NoticeCommand(Action);
  }

  /// <summary>
  ///   The title of the notification.
  /// </summary>
  public string? Title
  {
    get => _title;
    set => SetProperty(ref _title, value);
  }

  /// <summary>
  ///   The type of the notification.
  /// </summary>
  public NotificationLevel Level
  {
    get => _level;
    set => SetProperty(ref _level, value);
  }

  /// <summary>
  ///   The text for the optional action button.
  /// </summary>
  public string? ActionText
  {
    get => _actionText;
    set => SetProperty(ref _actionText, value);
  }

  /// <summary>
  ///   Gets or sets a value indicating whether the notification is closing.
  /// </summary>
  public bool IsClosing
  {
    get => _isClosing;
    set => SetProperty(ref _isClosing, value);
  }

  /// <summary>
  ///   The task that completes when the notification is dismissed.
  /// </summary>
  public Task<NotificationResult> ShowTask => CompletionTask;

  /// <summary>
  ///   Command to dismiss the notification.
  /// </summary>
  public ICommand DismissCommand { get; }

  /// <summary>
  ///   Dismisses the notification.
  /// </summary>
  private void Dismiss()
  {
    Complete(NotificationResult.Dismissed);
  }

  /// <summary>
  ///   Dismisses the notification with the specified result.
  /// </summary>
  /// <param name="result">The completion result value.</param>
  public void Dismiss(NotificationResult result)
  {
    Complete(result);
  }

  /// <summary>
  ///   Command to invoke the action and return the action-invoked result.
  /// </summary>
  public ICommand ActionCommand { get; }

  /// <summary>
  ///   Invokes the notification action, settling the entry with the action-invoked result.
  /// </summary>
  public void Action()
  {
    Dismiss(NotificationResult.ActionInvoked);
  }

  /// <summary>
  ///   Gets the result used when the notification times out.
  /// </summary>
  /// <returns>The timeout result.</returns>
  protected override NotificationResult GetTimeoutResult()
  {
    return NotificationResult.TimedOut;
  }

  /// <summary>
  ///   Gets the result used when the notification is dismissed.
  /// </summary>
  /// <returns>The dismiss result.</returns>
  protected override NotificationResult GetDismissResult()
  {
    return NotificationResult.Dismissed;
  }
}
