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
  internal static async Task WaitForLoadedAsync()
  {
    var d = System.Windows.Application.Current?.Dispatcher;
    if (d is not null)
      await d.InvokeAsync(static () => { }, System.Windows.Threading.DispatcherPriority.Background);
  }
}
