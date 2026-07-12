using Everlong.Nester.Threading;
using Terminal.Gui.App;

namespace Everlong.Nester.Hosting;

/// <summary>
///   Marshals work onto the Terminal.Gui main loop — the loop thread is the
///   main thread.  A post from the loop thread drains inline (single-threaded
///   program order); a post from any other thread bridges through
///   <c>IApplication.Invoke</c> and drains on the next iteration.
/// </summary>
internal sealed class TerminalMainDispatcher(IApplication app) : IMainDispatcher
{
  private readonly int _mainThreadId = Environment.CurrentManagedThreadId;
  private readonly Queue<Action> _queue = new();
  private bool _pumping;

  /// <inheritdoc />
  public bool CheckAccess() => Environment.CurrentManagedThreadId == _mainThreadId;

  /// <inheritdoc />
  public void Post(Action action, DispatchPriority priority = DispatchPriority.Normal)
  {
    lock (_queue)
    {
      _queue.Enqueue(action);
    }

    if (CheckAccess())
      Drain();
    else
      app.Invoke(_ => Drain());
  }

  /// <inheritdoc />
  public Task InvokeAsync(Action action, DispatchPriority priority = DispatchPriority.Normal)
  {
    if (CheckAccess())
    {
      try
      {
        action();
        return Task.CompletedTask;
      }
      catch (Exception e)
      {
        return Task.FromException(e);
      }
    }

    var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    Post(() =>
    {
      try
      {
        action();
        tcs.SetResult();
      }
      catch (Exception e)
      {
        tcs.SetException(e);
      }
    }, priority);
    return tcs.Task;
  }

  /// <inheritdoc />
  public Task InvokeAsync(Func<Task> callback, DispatchPriority priority = DispatchPriority.Normal)
  {
    if (CheckAccess())
    {
      try
      {
        return callback();
      }
      catch (Exception e)
      {
        return Task.FromException(e);
      }
    }

    var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    Post(async () =>
    {
      try
      {
        await callback().ConfigureAwait(false);
        tcs.SetResult();
      }
      catch (Exception e)
      {
        tcs.SetException(e);
      }
    }, priority);
    return tcs.Task;
  }

  /// <summary>Runs every queued action to empty — re-entrant posts join the same drain.</summary>
  private void Drain()
  {
    if (_pumping)
      return;
    _pumping = true;
    try
    {
      while (true)
      {
        Action next;
        lock (_queue)
        {
          if (_queue.Count == 0)
            return;
          next = _queue.Dequeue();
        }

        next();
      }
    }
    finally
    {
      _pumping = false;
    }
  }
}
