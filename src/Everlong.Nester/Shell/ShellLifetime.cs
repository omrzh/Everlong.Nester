namespace Everlong.Nester.Shell;

/// <summary>
///   The shell's lifetime: the lifecycle state, the startup completion signal
///   and the teardown signals.
/// </summary>
internal sealed class ShellLifetime : IShellLifetime
{
  /// <summary>The stop signal — canceled when teardown begins.</summary>
  private readonly CancellationTokenSource _stoppingCts = new();

  /// <summary>The stopped signal — canceled when the teardown cascade completes.</summary>
  private readonly CancellationTokenSource _stoppedCts = new();

  /// <summary>The startup signal — settles when the startup flow settles (result or fault).</summary>
  private readonly TaskCompletionSource _startupTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

  private ShellLifecycle _lifecycle;

  /// <inheritdoc />
  public ShellLifecycle Lifecycle => _lifecycle;

  /// <inheritdoc />
  public Task Startup => _startupTcs.Task;

  /// <inheritdoc />
  public CancellationToken Stopping => _stoppingCts.Token;

  /// <inheritdoc />
  public CancellationToken Stopped => _stoppedCts.Token;

  /// <summary>
  ///   Advances the lifecycle to <paramref name="targetState"/>, enforcing the
  ///   single forward walk <see cref="ShellLifecycle.Created"/> →
  ///   <see cref="ShellLifecycle.Assembling"/> → <see cref="ShellLifecycle.Assembled"/> →
  ///   <see cref="ShellLifecycle.Started"/> → <see cref="ShellLifecycle.Disposed"/>:
  ///   each step must be the next state; <see cref="ShellLifecycle.Disposed"/> is
  ///   legal from any state (idempotent); re-entering
  ///   <see cref="ShellLifecycle.Assembling"/> is allowed (idempotent).
  /// </summary>
  /// <param name="targetState">The destination lifecycle state.</param>
  /// <exception cref="InvalidOperationException">
  ///   Thrown when the state transition is invalid or out of order.
  /// </exception>
  internal void Advance(ShellLifecycle targetState)
  {
    if (targetState == ShellLifecycle.Disposed)
    {
      _lifecycle = targetState; // any → Disposed (idempotent teardown)
      return;
    }

    if (targetState == _lifecycle && targetState == ShellLifecycle.Assembling)
      return; // idempotent re-entry: ctor assembly / retry after a failed Start

    if (targetState != _lifecycle + 1)
    {
      throw new InvalidOperationException(
        $"Cannot transition lifecycle from {_lifecycle} to {targetState} — the shell walks " +
        $"{ShellLifecycle.Created} → {ShellLifecycle.Assembling} → {ShellLifecycle.Assembled} → " +
        $"{ShellLifecycle.Started} → {ShellLifecycle.Disposed} exactly once.");
    }

    _lifecycle = targetState;
  }

  /// <summary>Completes the startup signal — the startup flow settled.</summary>
  internal void CompleteStartup() => _startupTcs.TrySetResult();

  /// <summary>
  ///   Faults the startup signal and observes the fault (waiters never hang;
  ///   no <see cref="TaskScheduler.UnobservedTaskException"/> for non-waiters).
  ///   Idempotent — the signal settles once.
  /// </summary>
  internal void FaultStartup(Exception cause)
  {
    _startupTcs.TrySetException(cause);
    _ = _startupTcs.Task.ContinueWith(_ => { }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted,
                                      TaskScheduler.Default);
  }

  /// <summary>Cancels the stopping signal — teardown has begun.</summary>
  internal Task BeginTeardownAsync() => _stoppingCts.CancelAsync();

  /// <summary>Cancels the stopped signal — the teardown cascade has completed.</summary>
  internal Task CompleteTeardownAsync() => _stoppedCts.CancelAsync();
}
