namespace Everlong.Nester.Threading;

/// <summary>
///   Dispatches work to the main thread.
/// </summary>
public interface IMainDispatcher
{
  /// <summary>Posts a fire-and-forget action to the main thread.</summary>
  void Post(Action action, DispatchPriority priority = DispatchPriority.Normal);

  /// <summary>Asynchronously invokes an action on the main thread.</summary>
  Task InvokeAsync(Action action, DispatchPriority priority = DispatchPriority.Normal);

  /// <summary>Asynchronously invokes an async callback on the main thread.</summary>
  Task InvokeAsync(Func<Task> callback, DispatchPriority priority = DispatchPriority.Normal);

  /// <summary>
  ///   Returns <see langword="true" /> if the calling thread is the main thread.
  /// </summary>
  bool CheckAccess();
}
