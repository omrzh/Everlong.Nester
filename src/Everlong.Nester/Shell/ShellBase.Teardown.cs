using Everlong.Nester.Hosting;
using Everlong.Nester.Layer;

namespace Everlong.Nester.Shell;

partial class ShellBase
{
  private int _disposed;

  /// <summary>
  ///   Ends the shell imperatively — the addressed close: teardown
  ///   (<see cref="DisposeAsync" />) followed by the platform's presentation
  ///   end.  It consults no handler: no Director or page can refuse a close
  ///   the caller itself decided.  A user close affordance dispatches
  ///   <see cref="CloseIntent" /> through the chain instead.
  /// </summary>
  /// <remarks>
  ///   Idempotent and re-entrant: <see cref="DisposeAsync" />'s guard makes a
  ///   second call no-op, and ending an already-ended presentation is
  ///   best-effort.  NO LOCK — by design: the disposal guard is set
  ///   synchronously before the first await, so a re-entrant entrant (the
  ///   window-close translation, a concurrent dispatch) no-ops at the entry;
  ///   an async lock would deadlock the UI thread.
  /// </remarks>
  public async ValueTask CloseAsync()
  {
    await DisposeAsync();
    await EndPresentationAsync();
  }

  /// <summary>
  ///   Platform hook: ends the host presentation after teardown — the desktop
  ///   window's close, the terminal session's stop.  Default is a no-op, the
  ///   correct answer where <see cref="DisposeAsync" /> already detaches the
  ///   surface (single-view).
  /// </summary>
  protected virtual ValueTask EndPresentationAsync() => ValueTask.CompletedTask;

  /// <summary>
  ///   Destroys the shell — the single teardown entry: flips the lifecycle to
  ///   <see cref="ShellLifecycle.Disposed"/>, stops the shell's token,
  ///   evicts the layer leases (<see cref="ILayerTenant.OnEvictedAsync"/>
  ///   cascade), unbinds the activation agent (disposing a declared one),
  ///   then the window container (scope + root — the shell owns both) and
  ///   deregisters from the app-exit teardown cascade.  Idempotent and
  ///   re-entrant.  Platform shells may override to detach their surface
  ///   (single-view MainView).
  /// </summary>
  public virtual async ValueTask DisposeAsync()
  {
    if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
      return;

    _lifetime.Advance(ShellLifecycle.Disposed);

    // The startup signal settles with a fault: a shell disposed before its
    // startup flow settled never completes startup — waiters must not hang.
    _lifetime.FaultStartup(new InvalidOperationException("The shell was disposed before its startup flow settled."));

    // Stop signal first — but a tenant callback may throw during
    // cancellation: a broken observer must not abort the teardown cascade
    // (the dispose guard is already set, so a failed cascade is not
    // retryable).
    try
    {
      await _lifetime.BeginTeardownAsync();
    }
    catch
    {
      // ignore — cancellation is best-effort; the cascade below owns teardown
    }

    await EvictAllAsync();
    await ReleaseAgentAsync();
    await DisposeContainerAsync();

    // Teardown complete — deregister from the app-exit cascade: a shell
    // that closed at runtime leaves the registry here (the exit cascade
    // walks only the live set).  Idempotent — the cascade's own sweep
    // clears any leftovers.
    var lifetime = AppLifetime.Current;
    if (lifetime is not null)
    {
      await lifetime.UntrackAsync(this);
    }

    // Teardown complete — the Stopped signal fires: IShellLifetime.Stopped
    // observers (late teardown watchers) unblock now.  The CTS stays
    // undisposed on purpose: IShellLifetime.Stopping must stay readable after
    // disposal (window-owned tasks observe it late), and a plain CTS has
    // no resources worth reclaiming.
    try
    {
      await _lifetime.CompleteTeardownAsync();
    }
    catch
    {
      // best-effort — a broken observer must not fail the teardown
    }
  }

  /// <summary>Releases the window container — the shell's own provider (scope + root).  Idempotent.</summary>
  /// <remarks>Never touches a default scope (.NET 10 throws on an empty one).</remarks>
  internal async ValueTask DisposeContainerAsync()
  {
    var scope = ShellServiceScope;
    if (scope is null)
      return;

    ShellServiceScope = null;

    try
    {
      if (scope is IAsyncDisposable asyncScope)
        await asyncScope.DisposeAsync();
      else
        scope.Dispose();
    }
    finally
    {
      // The root is released even when the scope disposal failed — the
      // shell owns both, and neither is retryable (the dispose guard is
      // already set).
      var root = RootProvider;
      RootProvider = null;
      if (root is not null)
      {
        if (root is IAsyncDisposable asyncRoot)
          await asyncRoot.DisposeAsync();
        else
          (root as IDisposable)?.Dispose();
      }
    }
  }
}
