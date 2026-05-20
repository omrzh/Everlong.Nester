using System.ComponentModel;
using Everlong.Nester.Layer;
using Everlong.Nester.Threading;

namespace Everlong.Nester.Notice;

/// <summary>
///   The notice tenant base: rents the notice band and hands every show
///   call to the <see cref="INoticeEngine" />.
/// </summary>
/// <remarks>Creates the base service over the given shell surface and engine.</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class NoticeServiceBase(ILayerBroker broker, INoticeEngine engine, NoticeServiceOptions options)
  : INoticeService, ILayerTenant
{
  private object? _noticeHost;
  private bool _evicted;

  /// <summary>The entry engine — feeds the three concurrent stacks.</summary>
  protected INoticeEngine Engine => engine;

  /// <summary>The notice options.</summary>
  protected NoticeServiceOptions Options => options;

  /// <summary>Shows a toast.</summary>
  public void Toast(string message, ToastLevel level = ToastLevel.Information, TimeSpan? duration = null)
  {
    var entry = new ToastEntry
    {
      Message = message,
      Level = level,
      Duration = duration ?? options.ToastDuration
    };
    ShowCore(() => engine.ShowToast(entry, options));
  }

  /// <summary>Shows a snackbar.</summary>
  public void Show(string message, string? actionText = null, TimeSpan? duration = null)
  {
    var entry = new SnackbarEntry
    {
      Message = message,
      ActionText = actionText,
      Duration = duration ?? (actionText is null ? options.SnackbarDuration : options.SnackbarWithActionDuration)
    };
    ShowCore(() => engine.ShowSnackbar(entry, options));
  }

  /// <summary>Shows a snackbar and completes with its result.</summary>
  public Task<SnackbarResult> ShowAsync(string message, string? actionText = null, TimeSpan? duration = null)
  {
    var entry = new SnackbarEntry
    {
      Message = message,
      ActionText = actionText,
      Duration = duration ?? (actionText is null ? options.SnackbarDuration : options.SnackbarWithActionDuration)
    };
    ShowCore(() => engine.ShowSnackbar(entry, options));
    return entry.ShowTask;
  }

  /// <summary>Shows a notification.</summary>
  public void Notify(string title,
                     string message,
                     NotificationLevel level = NotificationLevel.Information,
                     TimeSpan? duration = null,
                     string? actionText = null)
  {
    var entry = new NotificationEntry
    {
      Title = title,
      Message = message,
      Level = level,
      Duration = duration ?? options.NotificationDuration,
      ActionText = actionText
    };
    ShowCore(() => engine.ShowNotification(entry, options));
  }

  /// <summary>Shows a notification and completes with its result.</summary>
  public Task<NotificationResult> NotifyAsync(string title,
                                              string message,
                                              NotificationLevel level = NotificationLevel.Information,
                                              TimeSpan? duration = null,
                                              string? actionText = null)
  {
    var entry = new NotificationEntry
    {
      Title = title,
      Message = message,
      Level = level,
      Duration = duration ?? options.NotificationDuration,
      ActionText = actionText
    };
    ShowCore(() => engine.ShowNotification(entry, options));
    return entry.ShowTask;
  }

  private void ShowCore(Action show)
  {
    // The engine manipulates the visual tree: TryPost defers to the bound
    // dispatcher; without one it runs inline.
    MainDispatcher.TryPost(() =>
    {
      // The band lease was reclaimed: the shell is gone and a late notice
      // has nowhere to land — dropped rather than faulting the caller.
      if (_evicted)
        return;
      EnsureHosts();
      show();
    });
  }

  /// <summary>Rents the notice band and installs the three entry stacks into the host.</summary>
  private void EnsureHosts()
  {
    if (_noticeHost is not null)
      return;

    _noticeHost = CreateHost();
    broker.Acquire(this, _noticeHost, KnownLayers.Notice, LayerPolicy.Floor);
  }

  /// <summary>
  ///   Platform hook: builds the host grid with the three entry stacks
  ///   anchored by their position options.
  /// </summary>
  protected abstract object CreateHost();

  /// <summary>The notice lease was reclaimed — the host is dropped and later shows are dropped with it.</summary>
  public ValueTask OnEvictedAsync(ILayerLease lease)
  {
    _evicted = true;
    _noticeHost = null;
    return ValueTask.CompletedTask;
  }
}
