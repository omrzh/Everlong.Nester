namespace Everlong.Nester.Notice;

/// <summary>
///   Represents the severity level of a toast notification.
/// </summary>
public enum ToastLevel
{
  /// <summary>
  ///   Indicates a successful operation.
  /// </summary>
  Success,

  /// <summary>
  ///   Indicates informational feedback.
  /// </summary>
  Information,

  /// <summary>
  ///   Indicates a warning condition.
  /// </summary>
  Warning,

  /// <summary>
  ///   Indicates an error condition.
  /// </summary>
  Error
}

/// <summary>
///   Represents the terminal state of a toast entry.
/// </summary>
public enum ToastResult
{
  /// <summary>
  ///   The toast timed out naturally.
  /// </summary>
  TimedOut,

  /// <summary>
  ///   The toast was dismissed.
  /// </summary>
  Dismissed
}

/// <summary>
///   Represents the result of a snackbar interaction.
/// </summary>
public enum SnackbarResult
{
  /// <summary>
  ///   The snackbar timed out naturally.
  /// </summary>
  TimedOut,

  /// <summary>
  ///   The snackbar action was invoked.
  /// </summary>
  ActionInvoked,

  /// <summary>
  ///   The snackbar was dismissed.
  /// </summary>
  Dismissed
}

/// <summary>
///   Represents the severity level of a notification banner.
/// </summary>
public enum NotificationLevel
{
  /// <summary>
  ///   Indicates a successful operation.
  /// </summary>
  Success,

  /// <summary>
  ///   Indicates informational feedback.
  /// </summary>
  Information,

  /// <summary>
  ///   Indicates a warning condition.
  /// </summary>
  Warning,

  /// <summary>
  ///   Indicates an error condition.
  /// </summary>
  Error
}

/// <summary>
///   Represents the result of a notification banner interaction.
/// </summary>
public enum NotificationResult
{
  /// <summary>
  ///   The banner timed out naturally.
  /// </summary>
  TimedOut,

  /// <summary>
  ///   The banner action was invoked.
  /// </summary>
  ActionInvoked,

  /// <summary>
  ///   The banner was dismissed.
  /// </summary>
  Dismissed
}

/// <summary>
///   Anchor position of a notice stack within the shell.
/// </summary>
public enum NoticePosition
{
  /// <summary>
  ///   Top-left corner.
  /// </summary>
  TopLeft,

  /// <summary>
  ///   Top edge, centered.
  /// </summary>
  TopCenter,

  /// <summary>
  ///   Top-right corner.
  /// </summary>
  TopRight,

  /// <summary>
  ///   Bottom-left corner.
  /// </summary>
  BottomLeft,

  /// <summary>
  ///   Bottom edge, centered.
  /// </summary>
  BottomCenter,

  /// <summary>
  ///   Bottom-right corner.
  /// </summary>
  BottomRight
}
