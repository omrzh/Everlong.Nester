using System.Runtime.ExceptionServices;

namespace Everlong.Nester.Threading;

/// <summary>
///   Extension methods for <see cref="IMainDispatcher" />.
/// </summary>
public static class MainDispatcherExtensions
{
  extension(IMainDispatcher dispatcher)
  {
    /// <summary>
    ///   Synchronously invokes an action on the main thread, blocking the caller.
    /// </summary>
    /// <remarks>Runs the delegate inline when already on the main thread; otherwise dispatches and blocks until completion.</remarks>
    public void Invoke(Action action,
                       DispatchPriority priority = DispatchPriority.Normal)
    {
      if (dispatcher.CheckAccess())
      {
        action();
        return;
      }

      dispatcher.InvokeAsync(action, priority).GetAwaiter().GetResult();
    }

    /// <summary>
    ///   Asynchronously invokes a function on the main thread and returns its result.
    /// </summary>
    public Task<T> InvokeAsync<T>(Func<T> callback,
                                  DispatchPriority priority = DispatchPriority.Normal)
    {
      if (dispatcher.CheckAccess())
      {
        return Task.FromResult(callback());
      }

      TaskCompletionSource<T> tcs = new();
      dispatcher.Post(() =>
      {
        try
        { tcs.SetResult(callback()); }
        catch (Exception ex) { tcs.SetException(ex); }
      }, priority);
      return tcs.Task;
    }

    /// <summary>
    ///   Asynchronously invokes an async function on the main thread and returns its result.
    /// </summary>
    public async Task<T> InvokeAsync<T>(Func<Task<T>> callback,
                                        DispatchPriority priority = DispatchPriority.Normal)
    {
      if (dispatcher.CheckAccess())
      {
        return await callback().ConfigureAwait(false);
      }

      TaskCompletionSource<T> tcs = new();
      await dispatcher.InvokeAsync(async () =>
      {
        try
        { tcs.SetResult(await callback().ConfigureAwait(false)); }
        catch (Exception ex) { tcs.SetException(ex); }
      }, priority).ConfigureAwait(false);
      return await tcs.Task.ConfigureAwait(false);
    }

    /// <summary>
    ///   Throws <see cref="InvalidOperationException" /> if the calling thread is not the main thread.
    /// </summary>
    public void VerifyAccess()
    {
      if (!dispatcher.CheckAccess())
      {
        throw new InvalidOperationException("The current thread is not the main thread.");
      }
    }

    /// <summary>
    ///   Surfaces an exception to this dispatcher's unhandled-exception path.
    /// </summary>
    /// <remarks>
    ///   The exception is observed, so it never surfaces as an unobserved
    ///   task exception.
    /// </remarks>
    public void SurfaceUnhandled(Exception exception)
    {
      _ = dispatcher.InvokeAsync(() => ExceptionDispatchInfo.Capture(exception).Throw())
                      .ContinueWith(_ => { }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
    }
  }
}
