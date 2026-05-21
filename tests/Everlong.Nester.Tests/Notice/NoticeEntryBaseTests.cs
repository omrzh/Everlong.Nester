using Everlong.Nester.Notice;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Everlong.Nester.Tests.Notice;

/// <summary>
///   Timer semantics of <see cref="NoticeEntryBase{TResult}" />: start/pause/
///   resume, freeze-on-hover, infinite duration, and manual dismissal — all
///   driven by a fake clock instead of wall time.
/// </summary>
public class NoticeEntryBaseTests
{
  private static SnackbarEntry Entry(TimeSpan duration, FakeTimeProvider time)
    => new() { Message = "hello", Duration = duration, TimeProvider = time };

  [Fact]
  public async Task StartTimer_WhenDurationElapses_CompletesWithTimeoutResult()
  {
    var time = new FakeTimeProvider();
    var entry = Entry(TimeSpan.FromMilliseconds(100), time);

    entry.StartTimer();
    Assert.False(entry.ShowTask.IsCompleted);

    time.Advance(TimeSpan.FromMilliseconds(100));

    SnackbarResult result = await entry.ShowTask.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
    Assert.Equal(SnackbarResult.TimedOut, result);
  }

  [Fact]
  public async Task StartTimer_InfiniteDuration_NeverAutoDismisses()
  {
    var time = new FakeTimeProvider();
    var entry = Entry(Timeout.InfiniteTimeSpan, time);

    entry.StartTimer();
    time.Advance(TimeSpan.FromDays(1));

    Assert.False(entry.ShowTask.IsCompleted);
  }

  [Fact]
  public async Task PauseTimer_ResumeTimer_PreservesRemainingDuration()
  {
    var time = new FakeTimeProvider();
    var entry = Entry(TimeSpan.FromSeconds(1), time);

    entry.StartTimer();
    time.Advance(TimeSpan.FromMilliseconds(400));
    entry.PauseTimer();
    Assert.True(entry.IsFrozen);

    // Frozen time must not count towards the remaining duration.
    time.Advance(TimeSpan.FromSeconds(5));
    Assert.False(entry.ShowTask.IsCompleted);

    entry.ResumeTimer();
    Assert.False(entry.IsFrozen);

    // Only the remaining 600 ms are left after the 400 ms that already ran.
    time.Advance(TimeSpan.FromMilliseconds(599));
    Assert.False(entry.ShowTask.IsCompleted);
    time.Advance(TimeSpan.FromMilliseconds(1));

    Assert.Equal(SnackbarResult.TimedOut, await entry.ShowTask.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));
  }

  [Fact]
  public async Task PauseTimer_WhenFreezeOnHoverDisabled_IsNoop()
  {
    var time = new FakeTimeProvider();
    var entry = Entry(TimeSpan.FromMilliseconds(100), time);
    entry.FreezeOnHover = false;

    entry.StartTimer();
    entry.PauseTimer();

    Assert.False(entry.IsFrozen);
    time.Advance(TimeSpan.FromMilliseconds(100));

    Assert.Equal(SnackbarResult.TimedOut, await entry.ShowTask.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));
  }

  [Fact]
  public async Task PauseTimer_InfiniteDuration_SetsFrozenWithoutElapsedBookkeeping()
  {
    var time = new FakeTimeProvider();
    var entry = Entry(Timeout.InfiniteTimeSpan, time);

    entry.StartTimer();
    entry.PauseTimer();
    Assert.True(entry.IsFrozen);

    time.Advance(TimeSpan.FromDays(1));
    Assert.False(entry.ShowTask.IsCompleted);

    entry.ResumeTimer();
    Assert.False(entry.IsFrozen);
    time.Advance(TimeSpan.FromDays(1));
    Assert.False(entry.ShowTask.IsCompleted);
  }

  [Fact]
  public async Task StartTimer_WhenAlreadyRunning_IsNoop()
  {
    var time = new FakeTimeProvider();
    var entry = Entry(TimeSpan.FromMilliseconds(100), time);

    entry.StartTimer();
    entry.StartTimer();
    entry.StartTimer();

    time.Advance(TimeSpan.FromMilliseconds(100));

    Assert.Equal(SnackbarResult.TimedOut, await entry.ShowTask.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));
  }

  [Fact]
  public async Task DismissAsync_CompletesWithDismissResult_AndStopsTimer()
  {
    var time = new FakeTimeProvider();
    var entry = Entry(TimeSpan.FromSeconds(30), time);
    var dismissable = (IDismissable)entry;

    entry.StartTimer();
    await dismissable.DismissAsync();

    Assert.Equal(SnackbarResult.Dismissed, await entry.ShowTask.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));

    // The cancelled timer must not fire afterwards.
    time.Advance(TimeSpan.FromSeconds(30));
    Assert.Equal(SnackbarResult.Dismissed, await entry.ShowTask);
  }
}
