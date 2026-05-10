namespace Everlong.Nester.Routing;

/// <summary>
///   The router's run supervision — the active transaction, the convergence
///   runs in flight and the idle signal.
/// </summary>
/// <remarks>
///   Every lifetime token a transfer hands out is cancelled exactly once: the
///   end of its convergence cancels it — a run that never started ends there
///   too — a transaction that leaves the pipe without committing cancels its
///   own, a committed one is cancelled by the next landing, which takes the
///   active slot, and the router's teardown cancels the active one.  A
///   cancellation callback is user code — it reports through the error channel
///   and decides no navigation.
/// </remarks>
internal sealed class RunSupervision
{
  private readonly object _gate = new();
  private readonly Action<IReadOnlyList<Exception>> _report;

  private TransactionContext? _active;
  private int _converging;
  private TaskCompletionSource _idle = new(TaskCreationOptions.RunContinuationsAsynchronously);

  /// <param name="report">The error channel a cancellation callback's failure reports through.</param>
  internal RunSupervision(Action<IReadOnlyList<Exception>> report) => _report = report;

  /// <summary>Hands a committed transaction over as the active one — the previous active transaction's lifetime token is cancelled.</summary>
  internal void Supersede(TransactionContext context)
  {
    TransactionContext? previous;
    lock (_gate)
    {
      previous = _active;
      _active = context;
    }

    if (previous is not null)
      Cancel(previous);
  }

  /// <summary>Settles a transaction that left the pipe — one that never committed cancels the token it handed out.</summary>
  internal void Retire(TransactionContext context)
  {
    if (context.IsCommitted)
      return;

    Cancel(context);
    lock (_gate)
    {
      if (ReferenceEquals(_active, context))
        _active = null;
    }
  }

  /// <summary>Ends the transfer of a convergence that never started — a newer transaction was already queued, so the run's observation channel and the transfer's lifetime close here.</summary>
  internal void Skip(IConvergenceContext run) => End(run);

  /// <summary>Ends the active transfer — the router is going away, so the lifetime token it handed out is cancelled.</summary>
  internal void EndActive()
  {
    TransactionContext? active;
    lock (_gate)
    {
      active = _active;
      _active = null;
    }

    if (active is not null)
      Cancel(active);
  }

  /// <summary>Opens a convergence run — the run counts in flight.</summary>
  internal void Open()
  {
    lock (_gate)
      _converging++;
  }

  /// <summary>Closes a convergence run — ends its transfer and, on the last run, the idle signal.</summary>
  internal void Close(IConvergenceContext run)
  {
    End(run);
    lock (_gate)
    {
      _converging--;
      if (_converging != 0)
        return;

      _idle.TrySetResult();
      _idle = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }
  }

  /// <summary>Completes when no convergence run is in flight.</summary>
  internal Task Idle
  {
    get
    {
      lock (_gate)
        return _converging == 0 ? Task.CompletedTask : _idle.Task;
    }
  }

  /// <summary>Ends a run — its observation channel and the transfer's lifetime token; a throwing cancellation callback reports instead of deciding the outcome.</summary>
  private void End(IConvergenceContext run) => Isolate(run.End);

  /// <summary>Cancels a transaction's lifetime token — a throwing callback reports instead of deciding the navigation's outcome.</summary>
  private void Cancel(TransactionContext context) => Isolate(context.CancelAbort);

  /// <summary>Runs an ending — a throwing cancellation callback is user code: it reports through the error channel.</summary>
  private void Isolate(Action ending)
  {
    try
    {
      ending();
    }
    catch (AggregateException aggregate)
    {
      _report(aggregate.InnerExceptions);
    }
    catch (Exception e)
    {
      _report([e]);
    }
  }
}
