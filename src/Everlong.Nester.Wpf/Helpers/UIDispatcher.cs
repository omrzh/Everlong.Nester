namespace Everlong.Nester.Helpers;

// ReSharper disable once InconsistentNaming
internal static class UIDispatcher
{
  /// <summary>
  ///   Asynchronously executes without waiting for the result (Post/BeginInvoke)
  /// </summary>
  public static void Post(Action action)
    => System.Windows.Application.Current?.Dispatcher.BeginInvoke(action);

  /// <summary>
  ///   Synchronously executes and blocks until the result is returned (Invoke)
  /// </summary>
  public static void Invoke(Action action)
    => System.Windows.Application.Current?.Dispatcher.Invoke(action);

  /// <summary>
  ///   Asynchronously executes and returns a Task
  /// </summary>
  public static Task InvokeAsync(Action action)
    => System.Windows.Application.Current?.Dispatcher.InvokeAsync(action).Task
       ?? Task.CompletedTask;

  /// <summary>
  ///   Asynchronously executes a function and returns its result (InvokeAsync)
  /// </summary>
  public static Task<T> InvokeAsync<T>(Func<T> function)
    => System.Windows.Application.Current?.Dispatcher.InvokeAsync(function).Task
       ?? Task.FromResult(default(T)!);

  /// <summary>
  ///   Asynchronously executes an async delegate on the UI thread.
  /// </summary>
  /// <param name="function">The async delegate to invoke.</param>
  /// <returns>A task that completes when the delegate finishes.</returns>
  public static Task InvokeAsync(Func<Task> function)
    => System.Windows.Application.Current?.Dispatcher.InvokeAsync(function).Task.Unwrap()
       ?? Task.CompletedTask;

  /// <summary>
  ///   Asynchronously executes an async delegate on the UI thread and returns its result.
  /// </summary>
  /// <param name="function">The async delegate to invoke.</param>
  /// <typeparam name="T">Result type.</typeparam>
  /// <returns>A task that completes with the delegate result.</returns>
  public static Task<T> InvokeAsync<T>(Func<Task<T>> function)
    => System.Windows.Application.Current?.Dispatcher.InvokeAsync(function).Task.Unwrap()
       ?? Task.FromResult(default(T)!);

  /// <summary>
  ///   Checks if the current thread is the UI thread (CheckAccess)
  /// </summary>
  public static bool CheckAccess()
    => System.Windows.Application.Current?.Dispatcher.CheckAccess() ?? false;

  /// <summary>
  ///   Verifies that the current thread is the UI thread (VerifyAccess)
  /// </summary>
  public static void VerifyAccess()
    => System.Windows.Application.Current?.Dispatcher.VerifyAccess();

  // ── Private helpers ──

  /// <summary>
  ///   Gives the dispatcher the pass that drains everything queued above
  ///   <see cref="System.Windows.Threading.DispatcherPriority.Background" />
  ///   — the layout and render work included.  It checks no condition: the
  ///   caller owns the predicate.
  /// </summary>
  internal static Task WaitForLoadedAsync()
  {
    var dispatcher = System.Windows.Application.Current?.Dispatcher;
    return dispatcher is null
             ? Task.CompletedTask
             : dispatcher.InvokeAsync(static () => { }, System.Windows.Threading.DispatcherPriority.Background).Task;
  }

  /// <summary>
  ///   Gives the dispatcher the pass that lays <paramref name="view" /> out,
  ///   and waits for the element's Loaded event when one pass was not enough.
  /// </summary>
  /// <remarks>
  ///   One pass realizes the layer the view was mounted into and measures the
  ///   cascade inside it; the event is the fallback for a cascade that takes
  ///   more than that.  Loaded is raised after layout, so both paths return a
  ///   laid-out element.  The post-pass guard tests arrangement alone on
  ///   purpose: an element the pass did lay out must never wait on an event
  ///   that a detached or already-consumed attachment may not raise.
  /// </remarks>
  internal static async Task WaitForLayoutAsync(PControl view, CancellationToken token)
  {
    if (view.IsLoaded && view.IsArrangeValid)
      return;

    await WaitForLoadedAsync();

    if (view.IsArrangeValid)
      return;

    await view.EnsureLoadedAsync(token);
  }
}
