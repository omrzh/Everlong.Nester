namespace Everlong.Nester.Notice;

/// <summary>
///   Aggregated configuration options for all feedback services (toast, snackbar, and notification panel).
/// </summary>
public sealed class NoticeServiceOptions
{
  // ── Hover ────────────────────────────────────────────────────────────────

  /// <summary>
  ///   When <see langword="true" />, the countdown timer pauses while the user hovers over a toast.
  ///   Default is <see langword="true" />.
  /// </summary>
  public bool ToastPauseOnHover { get; set; } = true;

  /// <summary>
  ///   When <see langword="true" />, the countdown timer pauses while the user hovers over a notification.
  ///   Default is <see langword="true" />.
  /// </summary>
  public bool NotificationPauseOnHover { get; set; } = true;

  /// <summary>
  ///   When <see langword="true" />, the countdown timer pauses while the user hovers over a snackbar.
  ///   Default is <see langword="true" />.
  /// </summary>
  public bool SnackbarPauseOnHover { get; set; } = true;

  // ── Default durations ────────────────────────────────────────────────────

  /// <summary>
  ///   Default visibility duration for toasts. Default is 3 seconds.
  /// </summary>
  public TimeSpan ToastDuration { get; set; } = TimeSpan.FromSeconds(3);

  /// <summary>
  ///   Default visibility duration for snackbars without an action button. Default is 4 seconds.
  /// </summary>
  public TimeSpan SnackbarDuration { get; set; } = TimeSpan.FromSeconds(4);

  /// <summary>
  ///   Default visibility duration for snackbars with an action button. Default is 6 seconds.
  /// </summary>
  public TimeSpan SnackbarWithActionDuration { get; set; } = TimeSpan.FromSeconds(6);

  /// <summary>
  ///   Default visibility duration for notification banners. Default is 3 seconds.
  /// </summary>
  public TimeSpan NotificationDuration { get; set; } = TimeSpan.FromSeconds(3);

  // ── Concurrency limits ───────────────────────────────────────────────────

  /// <summary>
  ///   Maximum concurrently visible toasts; the oldest is dismissed when a new one exceeds the limit.
  ///   Default is 4.
  /// </summary>
  public int ToastMaxCount { get; set; } = 4;

  /// <summary>
  ///   Maximum concurrently visible snackbars; the oldest is dismissed when a new one exceeds the limit.
  ///   Default is 1.
  /// </summary>
  public int SnackbarMaxCount { get; set; } = 1;

  /// <summary>
  ///   Maximum concurrently visible notification banners; the oldest is dismissed when a new one exceeds the limit.
  ///   Default is 3.
  /// </summary>
  public int NotificationMaxCount { get; set; } = 3;

  // ── Placement ────────────────────────────────────────────────────────────

  /// <summary>
  ///   Anchor position of the toast stack. Default is <see cref="NoticePosition.TopCenter" />.
  /// </summary>
  public NoticePosition ToastPosition { get; set; } = NoticePosition.TopCenter;

  /// <summary>
  ///   Anchor position of the snackbar stack. Default is <see cref="NoticePosition.BottomCenter" />.
  /// </summary>
  public NoticePosition SnackbarPosition { get; set; } = NoticePosition.BottomCenter;

  /// <summary>
  ///   Anchor position of the notification banner stack. Default is <see cref="NoticePosition.TopRight" />.
  /// </summary>
  public NoticePosition NotificationPosition { get; set; } = NoticePosition.TopRight;

  /// <summary>
  ///   Spacing between the toast stack and the shell edge. Default is 16 on all sides.
  /// </summary>
  public Primitives.Thickness ToastMargin { get; set; } = new(16);

  /// <summary>
  ///   Spacing between the snackbar stack and the shell edge. Default is 16 on all sides.
  /// </summary>
  public Primitives.Thickness SnackbarMargin { get; set; } = new(16);

  /// <summary>
  ///   Spacing between the notification banner stack and the shell edge. Default is 16 on all sides.
  /// </summary>
  public Primitives.Thickness NotificationMargin { get; set; } = new(16);
}
