using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Everlong.Nester.Helpers;

// ReSharper disable once InconsistentNaming
internal static class UIDispatcher
{
  /// <summary>
  ///   Asynchronously executes without waiting for the result (Post/BeginInvoke)
  /// </summary>
  public static void Post(Action action) => Dispatcher.UIThread.Post(action);

  /// <summary>
  ///   Synchronously executes and blocks until the result is returned (Invoke)
  /// </summary>
  public static void Invoke(Action action) => Dispatcher.UIThread.Invoke(action);

  /// <summary>
  ///   Asynchronously executes and returns a Task
  /// </summary>
  public static Task InvokeAsync(Action action) => Dispatcher.UIThread.InvokeAsync(action).GetTask();

  /// <summary>
  ///   Asynchronously executes a function and returns its result (InvokeAsync)
  /// </summary>
  public static Task<T> InvokeAsync<T>(Func<T> function) => Dispatcher.UIThread.InvokeAsync(function).GetTask();

  /// <summary>
  ///   Asynchronously executes an async delegate on the UI thread.
  /// </summary>
  /// <param name="function">The async delegate to invoke.</param>
  /// <returns>A task that completes when the delegate finishes.</returns>
  public static Task InvokeAsync(Func<Task> function) => Dispatcher.UIThread.InvokeAsync(function);

  /// <summary>
  ///   Asynchronously executes an async delegate on the UI thread and returns its result.
  /// </summary>
  /// <param name="function">The async delegate to invoke.</param>
  /// <typeparam name="T">Result type.</typeparam>
  /// <returns>A task that completes with the delegate result.</returns>
  public static Task<T> InvokeAsync<T>(Func<Task<T>> function) => Dispatcher.UIThread.InvokeAsync(function);

  /// <summary>
  ///   Checks if the current thread is the UI thread (CheckAccess)
  /// </summary>
  public static bool CheckAccess() => Dispatcher.UIThread.CheckAccess();

  /// <summary>
  ///   Verifies that the current thread is the UI thread (VerifyAccess)
  /// </summary>
  public static void VerifyAccess() => Dispatcher.UIThread.VerifyAccess();

  // ── Private helpers ──
  internal static async Task WaitForLoadedAsync()
  {
    await Dispatcher.UIThread.InvokeAsync(static () => { }, DispatcherPriority.Background);
  }

  /// <summary>The dispatcher turns <see cref="WaitForLayoutAsync" /> gives back before giving up.</summary>
  private const int LayoutTurnBudget = 8;

  /// <summary>
  ///   Yields to the dispatcher until <paramref name="view" /> is attached to
  ///   the visual tree and measured.  Bounded: a view that never joins the
  ///   tree costs the turn budget and no more.
  /// </summary>
  internal static async Task WaitForLayoutAsync(PControl view, CancellationToken token)
  {
    for (var turn = 0; turn < LayoutTurnBudget && !view.IsAttachedToVisualTree(); turn++)
    {
      if (token.IsCancellationRequested)
        return;

      await Dispatcher.UIThread.InvokeAsync(static () => { }, DispatcherPriority.Background);
    }

    // Attached is not measured: a view added to an attached parent joins the
    // tree at once and is measured by the pass this turn gives back.
    if (view.IsAttachedToVisualTree() && view.Bounds is not { Width: > 0, Height: > 0 })
      await Dispatcher.UIThread.InvokeAsync(static () => { }, DispatcherPriority.Background);
  }
}
