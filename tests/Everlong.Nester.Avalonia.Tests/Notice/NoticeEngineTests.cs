using Everlong.Nester.Notice;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Everlong.Nester.Tests.Notice;

using static Everlong.Nester.Tests.AsyncTestHelpers;

/// <summary>
///   The notice engine's scene lifecycle: an entry is presented in its
///   channel, timed, and dismissed; the channel limit trims the oldest.
/// </summary>
public class NoticeEngineTests
{
  /// <summary>A panel that records the engine's calls instead of drawing.</summary>
  private sealed class RecordingPanel : INoticePanel
  {
    public List<(INoticeEntry Entry, NoticeChannel Channel)> Presented { get; } = [];

    public List<(INoticeEntry Entry, NoticeChannel Channel)> Dismissed { get; } = [];

    public void AttachModel(NoticePanelModel model)
    {
    }

    public Task PresentAsync(INoticeEntry entry, NoticeChannel channel, CancellationToken token)
    {
      Presented.Add((entry, channel));
      return Task.CompletedTask;
    }

    public Task DismissAsync(INoticeEntry entry, NoticeChannel channel, CancellationToken token)
    {
      Dismissed.Add((entry, channel));
      return Task.CompletedTask;
    }
  }

  private static (NoticeEngine Engine, RecordingPanel Panel) Engine()
  {
    var engine = new NoticeEngine();
    var panel = new RecordingPanel();
    engine.AttachPanel(panel);
    return (engine, panel);
  }

  private static NoticeServiceOptions Options => new();

  private static ToastEntry Toast(TimeSpan duration, TimeProvider time)
    => new() { Message = "hello", Duration = duration, TimeProvider = time };

  [Fact]
  public async Task Toast_PlaysFullSceneLifecycle_AndAutoDismisses()
  {
    var time = new FakeTimeProvider();
    var entry = Toast(TimeSpan.FromMilliseconds(100), time);
    var (engine, panel) = Engine();

    engine.ShowToast(entry, Options);

    Assert.Single(panel.Presented);
    Assert.Same(entry, panel.Presented[0].Entry);
    Assert.Equal(NoticeChannel.Toast, panel.Presented[0].Channel);

    time.Advance(TimeSpan.FromMilliseconds(100));
    await WaitUntilAsync(() => panel.Dismissed.Count == 1);

    Assert.Same(entry, panel.Dismissed[0].Entry);
    Assert.Equal(NoticeChannel.Toast, panel.Dismissed[0].Channel);
  }

  [Fact]
  public async Task Snackbar_And_Notification_UseTheirOwnChannels()
  {
    var time = new FakeTimeProvider();
    var snack = new SnackbarEntry { Message = "snack", Duration = TimeSpan.FromMilliseconds(50), TimeProvider = time };
    var banner = new NotificationEntry { Title = "T", Message = "b", Duration = TimeSpan.FromMilliseconds(50), TimeProvider = time };
    var (engine, panel) = Engine();

    engine.ShowSnackbar(snack, Options);
    engine.ShowNotification(banner, Options);

    Assert.Contains(panel.Presented, p => p.Channel == NoticeChannel.Snackbar && ReferenceEquals(p.Entry, snack));
    Assert.Contains(panel.Presented, p => p.Channel == NoticeChannel.Notification && ReferenceEquals(p.Entry, banner));
    Assert.DoesNotContain(panel.Presented, p => p.Channel == NoticeChannel.Toast);

    time.Advance(TimeSpan.FromMilliseconds(50));
    await WaitUntilAsync(() => panel.Dismissed.Count == 2);
  }

  [Fact]
  public async Task ConcurrentToasts_AreIndependentScenes()
  {
    var time = new FakeTimeProvider();
    var shortEntry = Toast(TimeSpan.FromMilliseconds(100), time);
    var longEntry = Toast(TimeSpan.FromMilliseconds(300), time);
    var (engine, panel) = Engine();

    engine.ShowToast(shortEntry, Options);
    engine.ShowToast(longEntry, Options);

    Assert.Equal(2, panel.Presented.Count);

    // The short toast leaves while the long one stays.
    time.Advance(TimeSpan.FromMilliseconds(100));
    await WaitUntilAsync(() => panel.Dismissed.Count == 1);
    Assert.Same(shortEntry, panel.Dismissed[0].Entry);

    time.Advance(TimeSpan.FromMilliseconds(200));
    await WaitUntilAsync(() => panel.Dismissed.Count == 2);
  }

  [Fact]
  public async Task Toast_ExceedingMaxCount_DismissesOldest()
  {
    var options = new NoticeServiceOptions { ToastMaxCount = 1 };
    var time = new FakeTimeProvider();
    var first = Toast(Timeout.InfiniteTimeSpan, time);
    var second = Toast(Timeout.InfiniteTimeSpan, time);
    var (engine, panel) = Engine();

    engine.ShowToast(first, options);
    engine.ShowToast(second, options);

    await WaitUntilAsync(() => panel.Dismissed.Count == 1);
    Assert.Same(first, panel.Dismissed[0].Entry);
  }

  [Fact]
  public async Task SnackbarEntry_CompletesWithTimeoutResult()
  {
    var time = new FakeTimeProvider();
    var entry = new SnackbarEntry { Message = "hello", Duration = TimeSpan.FromMilliseconds(100), TimeProvider = time };
    var (engine, _) = Engine();

    engine.ShowSnackbar(entry, Options);

    time.Advance(TimeSpan.FromMilliseconds(100));
    SnackbarResult result = await entry.ShowTask.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
    Assert.Equal(SnackbarResult.TimedOut, result);
  }

  [Fact]
  public void Toast_InfiniteDuration_DoesNotAutoDismiss()
  {
    var time = new FakeTimeProvider();
    var entry = Toast(Timeout.InfiniteTimeSpan, time);
    var (engine, panel) = Engine();

    engine.ShowToast(entry, Options);

    time.Advance(TimeSpan.FromSeconds(5));
    Assert.Single(panel.Presented);
    Assert.Empty(panel.Dismissed);
  }
}
