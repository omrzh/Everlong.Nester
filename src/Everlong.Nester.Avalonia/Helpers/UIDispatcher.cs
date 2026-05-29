using Avalonia.Threading;

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
}
