namespace Everlong.Nester.Notice;

/// <summary>
///   The three notice channels — the independent entry stacks a panel
///   presents.
/// </summary>
public enum NoticeChannel
{
  /// <summary>Transient feedback without an action.</summary>
  Toast,

  /// <summary>Feedback carrying an optional action.</summary>
  Snackbar,

  /// <summary>A banner carrying an optional action.</summary>
  Notification,
}
