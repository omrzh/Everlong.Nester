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

  // ── Panel ───────────────────────────────────────────────────────────────

  /// <summary>
  ///   The panel model the mounted panel lays out by — its visual options,
  ///   not the service's: the service hands the model through and reads
  ///   nothing from it.
  /// </summary>
  public NoticePanelModel Panel { get; set; } = new();
}
