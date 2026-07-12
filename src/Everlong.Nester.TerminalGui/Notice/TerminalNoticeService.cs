namespace Everlong.Nester.Notice;

/// <summary>
///   The minimal Terminal.Gui notice tenant — satisfies the notice surface
///   until the notice band renders entries; messages trace to the console.
/// </summary>
public sealed class TerminalNoticeService : INoticeService
{
  /// <inheritdoc />
  public void Toast(string message, ToastLevel level = ToastLevel.Information, TimeSpan? duration = null)
    => Trace(message);

  /// <inheritdoc />
  public void Show(string message, string? actionText = null, TimeSpan? duration = null)
    => Trace(actionText is null ? message : $"{message} [{actionText}]");

  /// <inheritdoc />
  public Task<SnackbarResult> ShowAsync(string message, string? actionText = null, TimeSpan? duration = null)
  {
    Show(message, actionText, duration);
    return Task.FromResult(SnackbarResult.TimedOut);
  }

  /// <inheritdoc />
  public void Notify(string title, string message, NotificationLevel level = NotificationLevel.Information,
                     TimeSpan? duration = null, string? actionText = null)
    => Trace($"{title}: {message}");

  /// <inheritdoc />
  public Task<NotificationResult> NotifyAsync(string title, string message,
                                              NotificationLevel level = NotificationLevel.Information,
                                              TimeSpan? duration = null, string? actionText = null)
  {
    Notify(title, message, level, duration, actionText);
    return Task.FromResult(NotificationResult.TimedOut);
  }

  private static void Trace(string message)
  {
    System.Diagnostics.Trace.WriteLine($"[notice] {message}");
  }
}
