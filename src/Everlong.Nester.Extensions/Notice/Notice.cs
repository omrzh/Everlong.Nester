namespace Everlong.Nester.Notice;

/// <summary>
///   Service for displaying toast notifications.
/// </summary>
public interface IToastService
{
  /// <summary>
  ///   Shows a toast notification that auto-dismisses after <paramref name="duration" />.
  /// </summary>
  /// <param name="message">The message to display.</param>
  /// <param name="level">The severity level.</param>
  /// <param name="duration">How long the toast stays visible; <see langword="null" /> uses the default.</param>
  void Toast(string message, ToastLevel level = ToastLevel.Information, TimeSpan? duration = null);
}

/// <summary>
///   Service for displaying snackbar notifications.
/// </summary>
public interface ISnackbarService
{
  /// <summary>
  ///   Shows a snackbar notification that auto-dismisses after <paramref name="duration" />.
  /// </summary>
  /// <param name="message">The message to display.</param>
  /// <param name="actionText">The action button text; <see langword="null" /> shows no action button.</param>
  /// <param name="duration">How long the snackbar stays visible; <see langword="null" /> uses the default.</param>
  void Show(string message, string? actionText = null, TimeSpan? duration = null);

  /// <summary>
  ///   Shows a snackbar notification and returns a task that completes with the
  ///   interaction result when the snackbar is dismissed, times out, or its
  ///   action is invoked.
  /// </summary>
  /// <param name="message">The message to display.</param>
  /// <param name="actionText">The action button text; <see langword="null" /> shows no action button.</param>
  /// <param name="duration">How long the snackbar stays visible; <see langword="null" /> uses the default.</param>
  /// <returns>A task that completes with the <see cref="SnackbarResult" />.</returns>
  Task<SnackbarResult> ShowAsync(string message, string? actionText = null, TimeSpan? duration = null);
}

/// <summary>
///   Service for displaying notification banners.
/// </summary>
public interface INotificationService
{
  /// <summary>
  ///   Shows a notification banner that auto-dismisses after <paramref name="duration" />.
  /// </summary>
  /// <param name="title">The banner title.</param>
  /// <param name="message">The message to display.</param>
  /// <param name="level">The severity level.</param>
  /// <param name="duration">How long the banner stays visible; <see langword="null" /> uses the default.</param>
  /// <param name="actionText">The action button text; <see langword="null" /> shows no action button.</param>
  void Notify(string title, string message, NotificationLevel level = NotificationLevel.Information,
              TimeSpan? duration = null, string? actionText = null);

  /// <summary>
  ///   Shows a notification banner and returns a task that completes with the
  ///   interaction result when the banner is dismissed, times out, or its
  ///   action is invoked.
  /// </summary>
  /// <param name="title">The banner title.</param>
  /// <param name="message">The message to display.</param>
  /// <param name="level">The severity level.</param>
  /// <param name="duration">How long the banner stays visible; <see langword="null" /> uses the default.</param>
  /// <param name="actionText">The action button text; <see langword="null" /> shows no action button.</param>
  /// <returns>A task that completes with the <see cref="NotificationResult" />.</returns>
  Task<NotificationResult> NotifyAsync(string title, string message, NotificationLevel level = NotificationLevel.Information,
                                       TimeSpan? duration = null, string? actionText = null);
}
