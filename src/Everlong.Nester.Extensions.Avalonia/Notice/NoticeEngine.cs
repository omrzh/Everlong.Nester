using Everlong.Nester.Presentation;
// NOTE: Single-source file — the Extensions WPF project compiles this exact
// file via <Compile Include> in Everlong.Nester.Extensions.Wpf.csproj.
// Edit it here only; never create a WPF-side copy (the two builds would drift).
using System.Collections;
using System.Collections.ObjectModel;

namespace Everlong.Nester.Notice;

/// <summary>
///   The notice engine — the Air layer's private scene-execution engine.
///   Owns three concurrent entry stacks (toast / snackbar / banner), plays
///   each item as an independent scene (enter animation → timer → dismiss
///   animation), and completes awaitable notices.  No history, no modality.
///   The scene player is folded in — it owns no state.
/// </summary>
internal sealed class NoticeEngine : INoticeEngine
{
  private readonly IViewLocator<PControl> _viewLocator;
  private readonly ObservableCollection<PControl> _toastEntries = [];
  private readonly ObservableCollection<PControl> _snackbarEntries = [];
  private readonly ObservableCollection<PControl> _bannerEntries = [];

  internal NoticeEngine(IViewLocator<PControl> viewLocator)
  {
    _viewLocator = viewLocator;
  }

  /// <summary>Toast entry views, in display order.</summary>
  public IList<PControl> ToastEntries => _toastEntries;

  /// <summary>Snackbar entry views, in display order.</summary>
  public IList<PControl> SnackbarEntries => _snackbarEntries;

  /// <summary>Notification banner entry views, in display order.</summary>
  public IList<PControl> BannerEntries => _bannerEntries;

  IEnumerable INoticeEngine.ToastEntries => _toastEntries;
  IEnumerable INoticeEngine.SnackbarEntries => _snackbarEntries;
  IEnumerable INoticeEngine.BannerEntries => _bannerEntries;

  /// <summary>
  ///   Shows a toast entry: builds its view, plays the enter scene, starts
  ///   the dismissal timer, and plays the dismiss scene when the entry
  ///   completes.  Must run on the UI thread.
  /// </summary>
  public void ShowToast(ToastEntry entry, NoticeServiceOptions options)
  {
    entry.FreezeOnHover = options.ToastPauseOnHover;
    TrimToMax(_toastEntries, options.ToastMaxCount);
    Present(entry, entry.ShowTask, _toastEntries, options.ToastPosition);
  }

  /// <summary>
  ///   Shows a snackbar entry: builds its view, plays the enter scene, starts
  ///   the dismissal timer, and plays the dismiss scene when the entry
  ///   completes.  Must run on the UI thread.
  /// </summary>
  public void ShowSnackbar(SnackbarEntry entry, NoticeServiceOptions options)
  {
    entry.FreezeOnHover = options.SnackbarPauseOnHover;
    TrimToMax(_snackbarEntries, options.SnackbarMaxCount);
    Present(entry, entry.ShowTask, _snackbarEntries, options.SnackbarPosition);
  }

  /// <summary>
  ///   Shows a notification banner entry: builds its view, plays the enter
  ///   scene, starts the dismissal timer, and plays the dismiss scene when
  ///   the entry completes.  Must run on the UI thread.
  /// </summary>
  public void ShowNotification(NotificationEntry entry, NoticeServiceOptions options)
  {
    entry.FreezeOnHover = options.NotificationPauseOnHover;
    TrimToMax(_bannerEntries, options.NotificationMaxCount);
    Present(entry, entry.ShowTask, _bannerEntries, options.NotificationPosition);
  }

  /// <summary>
  ///   Dismisses the oldest entry of a stack when it already holds
  ///   <paramref name="maxCount" /> items, making room for the incoming one.
  /// </summary>
  private static void TrimToMax(ObservableCollection<PControl> stack, int maxCount)
  {
    if (maxCount <= 0 || stack.Count < maxCount)
    {
      return;
    }

    if (stack[0].DataContext is IDismissable dismissable)
    {
      // Completes the entry immediately; the dismiss scene removes the view.
      _ = dismissable.DismissAsync();
    }
    else
    {
      stack.RemoveAt(0);
    }
  }

  private void Present<TResult>(NoticeEntryBase<TResult> entry,
                                Task<TResult> showTask,
                                ObservableCollection<PControl> stack,
                                NoticePosition position)
  {
    PControl view = _viewLocator.Build(entry) ?? new PTextBlock
    {
      Text = "Cannot resolve notice view"
    };
    view.DataContext = entry;
    _ = PresentEntryAsync(entry, view, showTask, stack, position);
  }

  private async Task PresentEntryAsync<TResult>(NoticeEntryBase<TResult> entry,
                                                PControl view,
                                                Task<TResult> showTask,
                                                ObservableCollection<PControl> stack,
                                                NoticePosition position)
  {
    try
    {
      // The timer starts only once the item is visibly on screen.
      await PlayEnterAsync(view, stack.Add, position);
      entry.StartTimer();
      await showTask;
      await PlayDismissAsync(view, v => stack.Remove(v), position);
    }
    catch
    {
      // Fire-and-forget safety net: an unexpected failure must never leave
      // the item stranded on screen — force it out of the stack.
      stack.Remove(view);
    }
  }

  /// <summary>
  ///   Attaches the view and runs the enter animation (the view's own
  ///   <see cref="ISceneTransition" />, or the default director anchored at
  ///   <paramref name="position" /> when the view implements none).
  ///   The item is laid out at <c>Opacity = 0</c> during the animation and
  ///   restored afterwards, matching the page pipeline's director contract.
  ///   A broken or preempted animation never blocks the item from appearing.
  /// </summary>
  private async Task PlayEnterAsync(PControl view,
                                    Action<PControl> attach,
                                    NoticePosition position,
                                    CancellationToken token = default)
  {
    ArgumentNullException.ThrowIfNull(view);
    ArgumentNullException.ThrowIfNull(attach);

    attach(view);
    view.Opacity = 0;
    view.IsHitTestVisible = false;

    try
    {
      if (view is ISceneTransition director)
        await director.AnimateEnterAsync(new TransitionContext(null, TransitionKind.Enter) { ArrivingChain = [view] }, token);
      else
        await DefaultNoticeDirector.Instance.AnimateEnterAsync(new TransitionContext(null, TransitionKind.Enter) { ArrivingChain = [view] }, position, token);
    }
    catch (OperationCanceledException)
    {
      // Preempted — the item still lands visibly.
    }
    catch (Exception)
    {
      // A broken director must not block the item from appearing.
    }
    finally
    {
      view.Opacity = 1;
      view.IsHitTestVisible = true;
    }

  }

  /// <summary>
  ///   Runs the exit animation (the view's own <see cref="ISceneTransition" />,
  ///   or the default director anchored at <paramref name="position" /> when
  ///   the view implements none) then detaches the view.
  ///   Detach always runs, even when the animation fails or is preempted — a
  ///   broken director must not leak the item on screen.
  /// </summary>
  private async Task PlayDismissAsync(PControl view,
                                      Action<PControl> detach,
                                      NoticePosition position,
                                      CancellationToken token = default)
  {
    ArgumentNullException.ThrowIfNull(view);
    ArgumentNullException.ThrowIfNull(detach);

    try
    {
      if (view is ISceneTransition director)
        await director.AnimateExitAsync(new TransitionContext(null, TransitionKind.Dismiss) { DepartingChain = [view] }, token);
      else
        await DefaultNoticeDirector.Instance.AnimateExitAsync(new TransitionContext(null, TransitionKind.Dismiss) { DepartingChain = [view] }, position, token);
    }
    catch (OperationCanceledException)
    {
      // Preempted — dismissal still completes.
    }
    catch (Exception)
    {
      // A broken director must not leak the item on screen.
    }
    finally
    {

      detach(view);
    }
  }
}
