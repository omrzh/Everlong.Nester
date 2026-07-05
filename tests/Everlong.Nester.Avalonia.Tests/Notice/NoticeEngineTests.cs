using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Everlong.Nester.Notice;
using Everlong.Nester.Presentation;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Everlong.Nester.Tests.Notice;

using static Everlong.Nester.Tests.AsyncTestHelpers;

public class NoticeEngineTests
{
  private sealed class FakeNoticeView : Control, ISceneTransition
  {
    public List<string> Calls { get; } = [];

    public Task AnimateEnterAsync(TransitionContext context, CancellationToken token)
    {
      Calls.Add(nameof(AnimateEnterAsync));
      return Task.CompletedTask;
    }

    public Task AnimateExitAsync(TransitionContext context, CancellationToken token)
    {
      Calls.Add(nameof(AnimateExitAsync));
      return Task.CompletedTask;
    }
  }

  private sealed class PlainView : Control
  {
    public List<string> Calls { get; } = [];
  }

  private sealed class FakeViewLocator : IViewLocator
  {
    public required Func<object?, Control?> Factory { get; init; }

    public bool Match(object? data) => true;

    public Control? Build(object? data) => Factory(data);
  }

  private static NoticeServiceOptions Options => new();

  private static ToastEntry ToastEntry(TimeSpan duration)
    => new() { Message = "hello", Duration = duration };

  [AvaloniaFact]
  public async Task Toast_PlaysFullSceneLifecycle_AndAutoDismisses()
  {
    var time = new FakeTimeProvider();
    var entry = ToastEntry(TimeSpan.FromMilliseconds(100));
    entry.TimeProvider = time;
    var view = new FakeNoticeView();
    var locator = new FakeViewLocator { Factory = _ => view };
    var engine = new NoticeEngine(locator);

    engine.ShowToast(entry, Options);

    // The enter scene runs synchronously up to the animation — the view is
    // attached before Show returns.
    Assert.Single(engine.ToastEntries);
    Assert.Same(view, engine.ToastEntries[0]);

    time.Advance(TimeSpan.FromMilliseconds(100));
    await WaitUntilAsync(() => engine.ToastEntries.Count == 0);

    Assert.Equal(
    [
      nameof(FakeNoticeView.AnimateEnterAsync),
      nameof(FakeNoticeView.AnimateExitAsync)
    ], view.Calls);
  }

  [AvaloniaFact]
  public async Task Toast_WithoutTransitionDirector_SkipsAnimations()
  {
    var view = new PlainView();
    var locator = new FakeViewLocator { Factory = _ => view };
    var engine = new NoticeEngine(locator);

    engine.ShowToast(ToastEntry(TimeSpan.FromMilliseconds(100)), Options);

    await WaitUntilAsync(() => engine.ToastEntries.Count == 0);

    Assert.Empty(view.Calls);
  }

  [AvaloniaFact]
  public async Task Snackbar_And_Notification_UseTheirOwnStacks()
  {
    var locator = new FakeViewLocator { Factory = _ => new Control() };
    var engine = new NoticeEngine(locator);

    engine.ShowSnackbar(new SnackbarEntry { Message = "snack", Duration = TimeSpan.FromMilliseconds(50) },
                        Options);
    engine.ShowNotification(new NotificationEntry { Title = "T", Message = "banner", Duration = TimeSpan.FromMilliseconds(50) },
                            Options);

    Assert.Single(engine.SnackbarEntries);
    Assert.Single(engine.BannerEntries);
    Assert.Empty(engine.ToastEntries);

    await WaitUntilAsync(() => engine.SnackbarEntries.Count == 0 && engine.BannerEntries.Count == 0);
  }

  [AvaloniaFact]
  public async Task ConcurrentToasts_AreIndependentScenes()
  {
    var time = new FakeTimeProvider();
    var shortEntry = ToastEntry(TimeSpan.FromMilliseconds(100));
    shortEntry.TimeProvider = time;
    var longEntry = ToastEntry(TimeSpan.FromMilliseconds(300));
    longEntry.TimeProvider = time;
    var view1 = new FakeNoticeView();
    var view2 = new FakeNoticeView();
    int built = 0;
    var locator = new FakeViewLocator { Factory = _ => built++ == 0 ? view1 : view2 };
    var engine = new NoticeEngine(locator);

    engine.ShowToast(shortEntry, Options);
    engine.ShowToast(longEntry, Options);

    Assert.Equal(2, engine.ToastEntries.Count);

    // The short toast leaves while the long one stays.
    time.Advance(TimeSpan.FromMilliseconds(100));
    await WaitUntilAsync(() => engine.ToastEntries.Count == 1);
    Assert.Same(view2, engine.ToastEntries[0]);

    time.Advance(TimeSpan.FromMilliseconds(200));
    await WaitUntilAsync(() => engine.ToastEntries.Count == 0);
  }

  [AvaloniaFact]
  public async Task SnackbarEntry_CompletesWithTimeoutResult()
  {
    var entry = new SnackbarEntry { Message = "hello", Duration = TimeSpan.FromMilliseconds(100) };
    var locator = new FakeViewLocator { Factory = _ => new Control() };
    var engine = new NoticeEngine(locator);

    engine.ShowSnackbar(entry, Options);

    SnackbarResult result = await entry.ShowTask.WaitAsync(TimeSpan.FromSeconds(2));
    Assert.Equal(SnackbarResult.TimedOut, result);
  }

  [AvaloniaFact]
  public async Task Toast_ExceedingMaxCount_DismissesOldest()
  {
    var options = new NoticeServiceOptions { ToastMaxCount = 1 };
    var first = ToastEntry(TimeSpan.FromSeconds(30));
    var second = ToastEntry(TimeSpan.FromSeconds(30));
    var locator = new FakeViewLocator { Factory = _ => new Control() };
    var engine = new NoticeEngine(locator);

    engine.ShowToast(first, options);
    Assert.Single(engine.ToastEntries);

    // The second toast exceeds the limit — the first is dismissed and
    // leaves via its dismiss scene.
    engine.ShowToast(second, options);
    await WaitUntilAsync(() => engine.ToastEntries.Count == 1);
    Assert.Same(second, engine.ToastEntries[0].DataContext);
  }

  [AvaloniaFact]
  public async Task Toast_InfiniteDuration_DoesNotAutoDismiss()
  {
    var time = new FakeTimeProvider();
    var entry = ToastEntry(Timeout.InfiniteTimeSpan);
    entry.TimeProvider = time;
    var locator = new FakeViewLocator { Factory = _ => new Control() };
    var engine = new NoticeEngine(locator);

    engine.ShowToast(entry, Options);

    time.Advance(TimeSpan.FromSeconds(5));
    Assert.Single(engine.ToastEntries);
  }
}
