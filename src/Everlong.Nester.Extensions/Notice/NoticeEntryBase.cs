using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Everlong.Nester.Notice;

/// <summary>
///   Abstraction for feedback items (Snackbar, Notification, Toast) that can be dismissed.
/// </summary>
public interface IDismissable
{
  /// <summary>
  ///   Dismisses the feedback item asynchronously.
  /// </summary>
  /// <returns>A task that completes when the item has been dismissed.</returns>
  Task DismissAsync();
}

/// <summary>
///   Abstract base class for feedback entries (Snackbar, Notification, Toast items).
///   Provides timer management, freeze-on-hover functionality, and completion tracking.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class NoticeEntryBase<TResult> : INotifyPropertyChanged, IPointerAware, IDismissable
{
  private readonly TaskCompletionSource<TResult> _completionSource = new();
  private bool _isTimerRunning;
  private TimeSpan _remainingDuration;
  private DateTime _startTime;
  private CancellationTokenSource? _timerCts;
  private ITimer? _timer;

  /// <inheritdoc />
  public event PropertyChangedEventHandler? PropertyChanged;

  /// <summary>
  ///   Gets or sets whether the timer should pause when the mouse hovers over the feedback entry.
  /// </summary>
  public bool FreezeOnHover { get; set; } = true;

  /// <summary>
  ///   Gets the icon to display — a geometry or image source.
  /// </summary>
  public object? Icon
  {
    get;
    set => SetProperty(ref field, value);
  }

  /// <summary>
  ///   Gets or sets how long the feedback message should be visible before auto-dismissing.
  /// </summary>
  public TimeSpan Duration
  {
    get;
    set => SetProperty(ref field, value);
  } = TimeSpan.FromSeconds(3);

  /// <summary>
  ///   Gets or sets the time source for the auto-dismiss timer.
  /// </summary>
  public TimeProvider TimeProvider { get; set; } = TimeProvider.System;

  /// <summary>
  ///   Gets or sets the message text to display.
  /// </summary>
  public string Message
  {
    get;
    set => SetProperty(ref field, value);
  } = string.Empty;

  /// <summary>
  ///   Gets a value indicating whether the timer is currently frozen (paused) due to pointer hover.
  /// </summary>
  public bool IsFrozen
  {
    get;
    private set => SetProperty(ref field, value);
  }

  /// <summary>
  ///   Gets the completion task that resolves when this feedback entry reaches a terminal state.
  /// </summary>
  protected Task<TResult> CompletionTask => _completionSource.Task;

  /// <summary>
  ///   Sets a backing field and raises <see cref="PropertyChanged" /> when the value changes.
  /// </summary>
  /// <returns><see langword="true" /> when the value changed; otherwise <see langword="false" />.</returns>
  protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
  {
    if (EqualityComparer<T>.Default.Equals(field, value))
    {
      return false;
    }

    field = value;
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    return true;
  }

  Task IDismissable.DismissAsync()
  {
    Complete(GetDismissResult());
    return Task.CompletedTask;
  }

  /// <summary>
  ///   Called when the pointer enters the feedback entry to pause the dismissal timer.
  /// </summary>
  public void OnPointerEnter()
  {
    PauseTimer();
  }

  /// <summary>
  ///   Called when the pointer leaves the feedback entry to resume the dismissal timer.
  /// </summary>
  public void OnPointerLeave()
  {
    ResumeTimer();
  }

  /// <summary>
  ///   Starts the auto-dismissal timer if not already running.
  /// </summary>
  public void StartTimer()
  {
    if (_isTimerRunning || CompletionTask.IsCompleted)
    {
      return;
    }

    _remainingDuration = Duration;
    RunTimer();
  }

  /// <summary>
  ///   Pauses the auto-dismissal timer; no-op when freeze-on-hover is
  ///   disabled.
  /// </summary>
  public void PauseTimer()
  {
    if (!FreezeOnHover || IsFrozen || !_isTimerRunning)
    {
      return;
    }

    if (_remainingDuration == Timeout.InfiniteTimeSpan)
    {
      IsFrozen = true;
      return;
    }

    TimeSpan elapsed = TimeProvider.GetUtcNow().UtcDateTime - _startTime;
    _remainingDuration -= elapsed;
    if (_remainingDuration < TimeSpan.Zero)
    {
      _remainingDuration = TimeSpan.Zero;
    }

    IsFrozen = true;
    _timerCts?.Cancel();
    _timerCts?.Dispose();
    _timerCts = null;
    _timer?.Dispose();
    _timer = null;
  }

  /// <summary>
  ///   Resumes the auto-dismissal timer.
  /// </summary>
  public void ResumeTimer()
  {
    if (!IsFrozen || CompletionTask.IsCompleted)
    {
      return;
    }

    IsFrozen = false;
    if (_remainingDuration == Timeout.InfiniteTimeSpan)
    {
      return;
    }

    RunTimer();
  }

  private void RunTimer()
  {
    if (_remainingDuration == Timeout.InfiniteTimeSpan)
    {
      // Never auto-dismisses — only an external Complete (dismiss or action)
      // ends the entry.
      _isTimerRunning = true;
      return;
    }

    if (_remainingDuration <= TimeSpan.Zero)
    {
      Complete(GetTimeoutResult());
      return;
    }

    _startTime = TimeProvider.GetUtcNow().UtcDateTime;
    _timerCts = new CancellationTokenSource();
    _isTimerRunning = true;

    _timer = TimeProvider.CreateTimer(static s =>
    {
      var entry = (NoticeEntryBase<TResult>)s!;
      entry._isTimerRunning = false;
      entry._timer = null;
      entry.Complete(entry.GetTimeoutResult());
    }, this, _remainingDuration, Timeout.InfiniteTimeSpan);
  }

  /// <summary>
  ///   Completes this feedback entry with the specified terminal result.
  /// </summary>
  /// <param name="result">The completion result value.</param>
  protected void Complete(TResult result)
  {
    _timerCts?.Cancel();
    _timerCts?.Dispose();
    _timerCts = null;
    _timer?.Dispose();
    _timer = null;
    _completionSource.TrySetResult(result);
  }

  /// <summary>
  ///   Gets the result value to use when the entry auto-dismisses due to timeout.
  /// </summary>
  protected abstract TResult GetTimeoutResult();

  /// <summary>
  ///   Gets the result value to use when the entry is manually dismissed.
  /// </summary>
  protected abstract TResult GetDismissResult();
}
