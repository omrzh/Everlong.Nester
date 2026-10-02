using System.ComponentModel;

namespace Everlong.Nester.Notice;

/// <summary>
///   The notice domain's engine — drives the three concurrent entry stacks
///   (toast / snackbar / notification) and their scene lifecycle.  It owns
///   the entry models, their timers and their results; the panel owns every
///   pixel.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class NoticeEngine : INoticeEngine
{
  private readonly List<INoticeEntry> _toasts = [];
  private readonly List<INoticeEntry> _snackbars = [];
  private readonly List<INoticeEntry> _banners = [];

  private INoticePanel? _panel;

  /// <inheritdoc />
  public void AttachPanel(INoticePanel panel) => _panel = panel;

  /// <inheritdoc />
  public void ShowToast(ToastEntry entry, NoticeServiceOptions options)
    => Present(entry, entry.ShowTask, NoticeChannel.Toast, _toasts, options.ToastMaxCount);

  /// <inheritdoc />
  public void ShowSnackbar(SnackbarEntry entry, NoticeServiceOptions options)
    => Present(entry, entry.ShowTask, NoticeChannel.Snackbar, _snackbars, options.SnackbarMaxCount);

  /// <inheritdoc />
  public void ShowNotification(NotificationEntry entry, NoticeServiceOptions options)
    => Present(entry, entry.ShowTask, NoticeChannel.Notification, _banners, options.NotificationMaxCount);

  /// <summary>
  ///   Starts an entry's scene — trimming the channel to make room first.
  ///   Fire-and-forget: the caller observes the entry through its
  ///   <c>ShowTask</c>, never through the scene.
  /// </summary>
  private void Present<TResult>(NoticeEntryBase<TResult> entry,
                                Task<TResult> showTask,
                                NoticeChannel channel,
                                List<INoticeEntry> active,
                                int maxCount)
  {
    // The oldest leaves when the channel is at its limit; its own scene runs
    // the exit — this only settles it.
    if (maxCount > 0 && active.Count >= maxCount)
      _ = active[0].DismissAsync();

    _ = PresentEntryAsync(entry, showTask, channel, active);
  }

  private async Task PresentEntryAsync<TResult>(NoticeEntryBase<TResult> entry,
                                                Task<TResult> showTask,
                                                NoticeChannel channel,
                                                List<INoticeEntry> active)
  {
    active.Add(entry);
    try
    {
      if (_panel is { } panel)
        await panel.PresentAsync(entry, channel, CancellationToken.None);

      // The timer starts only once the entry is visibly on screen.
      entry.StartTimer();
      await showTask;

      if (_panel is { } dismissing)
        await dismissing.DismissAsync(entry, channel, CancellationToken.None);
    }
    finally
    {
      active.Remove(entry);
    }
  }
}
