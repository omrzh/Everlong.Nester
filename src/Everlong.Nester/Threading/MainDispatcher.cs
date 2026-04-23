using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Everlong.Nester.Threading;

/// <summary>
///   Static facade over the <see cref="IMainDispatcher" />.
/// </summary>
/// <remarks>
///   <see cref="Post"/>, <see cref="InvokeAsync(Action, DispatchPriority)"/>, and
///   <see cref="CheckAccess"/> require a bound instance — host-required call
///   sites; <see cref="TryGet"/> and <see cref="TryPost"/> degrade to the
///   caller's thread when nothing is bound.
/// </remarks>
public static class MainDispatcher
{
  /// <summary>
  ///   Posts a fire-and-forget action to the main thread.
  /// </summary>
  /// <remarks>
  ///   Throws when no dispatcher is bound — host-required call sites;
  ///   degradable call sites use <see cref="TryPost"/>.
  /// </remarks>
  public static void Post(Action action, DispatchPriority priority = DispatchPriority.Normal)
  {
    Require().Post(action, priority);
  }

  /// <summary>
  ///   Posts <paramref name="action"/> to the main thread, or runs it inline
  ///   when no dispatcher is bound or the caller is already on the main
  ///   thread.
  /// </summary>
  /// <returns>
  ///   <see langword="true"/> when the action was posted (deferred);
  ///   <see langword="false"/> when it ran inline.
  /// </returns>
  public static bool TryPost(Action action, DispatchPriority priority = DispatchPriority.Normal)
  {
    if (TryGet(out var dispatcher) && !dispatcher.CheckAccess())
    {
      dispatcher.Post(action, priority);
      return true;
    }

    action();
    return false;
  }

  /// <summary>
  ///   Asynchronously invokes an action on the main thread.
  /// </summary>
  /// <remarks>Throws when no dispatcher is bound — host-required call sites.</remarks>
  public static Task InvokeAsync(Action action, DispatchPriority priority = DispatchPriority.Normal)
  {
    return Require().InvokeAsync(action, priority);
  }

  /// <summary>
  ///   Asynchronously invokes an async callback on the main thread.
  /// </summary>
  /// <remarks>Throws when no dispatcher is bound — host-required call sites.</remarks>
  public static Task InvokeAsync(Func<Task> callback, DispatchPriority priority = DispatchPriority.Normal)
  {
    return Require().InvokeAsync(callback, priority);
  }

  /// <summary>
  ///   Returns <see langword="true" /> if the calling thread is the main thread.
  /// </summary>
  /// <remarks>Throws when no dispatcher is bound — host-required call sites.</remarks>
  public static bool CheckAccess()
  {
    return Require().CheckAccess();
  }

  private static IMainDispatcher? _instance;

  /// <summary>
  ///   Binds the process-wide main-thread dispatcher instance.
  /// </summary>
  /// <remarks>
  ///   Idempotent — rebinding the same instance is a no-op; binding a different
  ///   instance throws <see cref="InvalidOperationException"/>.
  /// </remarks>
  public static void BindInstance(IMainDispatcher dispatcher)
  {
    ArgumentNullException.ThrowIfNull(dispatcher);
    if (_instance != null && !ReferenceEquals(_instance, dispatcher))
    {
      throw new InvalidOperationException("A different dispatcher is already bound to MainDispatcher.");
    }

    if (ReferenceEquals(_instance, dispatcher))
    {
      return; // idempotent
    }

    _instance = dispatcher;
  }

  /// <summary>
  ///   Clears the bound instance — test isolation.
  /// </summary>
  /// <remarks>
  ///   After a reset, binding-requiring entries throw until
  ///   <see cref="BindInstance"/> binds again.
  /// </remarks>
  [EditorBrowsable(EditorBrowsableState.Never)]
  public static void ResetForTesting()
  {
    _instance = null;
  }

  private static IMainDispatcher Require()
  {
    return _instance ?? throw new InvalidOperationException(
             "MainDispatcher is not initialized. Call BindInstance before using MainDispatcher.");
  }

  /// <summary>
  ///   Tries to get the bound main-thread dispatcher instance.
  /// </summary>
  public static bool TryGet([NotNullWhen(true)] out IMainDispatcher? dispatcher)
  {
    dispatcher = _instance;
    return dispatcher != null;
  }

}
