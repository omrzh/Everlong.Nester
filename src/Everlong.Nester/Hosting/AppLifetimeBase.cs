using System.ComponentModel;
using Everlong.Nester.Diagnostics;
using Everlong.Nester.Shell;
using Everlong.Nester.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Everlong.Nester.Hosting;

/// <summary>
///   Platform-neutral host of the application lifetime: the container root,
///   shell creation and tracking, the shutdown cascade and the global error
///   hooks.  Activation is the broker's surface — the host never touches
///   activation intents.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class AppLifetimeBase : IAppLifetime
{
  /// <summary>Live shells in the app-exit teardown cascade — each shell untracks itself when its teardown completes.</summary>
  protected readonly List<IShell> Shells = [];

  /// <summary>Serializes registry mutations (track / untrack / cascade snapshot) — never held across an await.</summary>
  private readonly SemaphoreSlim _registryGate = new(1, 1);

  /// <inheritdoc />
  public IMainDispatcher Dispatcher
  {
    get => field!;
    protected set
    {
      field = value;
      MainDispatcher.BindInstance(value);
    }
  }

  /// <inheritdoc />
  public IErrorHandler? ErrorHandler { get; protected set; }

  /// <inheritdoc />
  public IServiceProvider? Services { get; protected set; }

  private int _disposeState;

  /// <inheritdoc />
  public IShell? MainShell { get; private set; }

  /// <summary>The host's stop signal — canceled when the teardown cascade begins.</summary>
  private readonly CancellationTokenSource _stoppingCts = new();

  /// <summary>The host's stopped signal — canceled when the teardown cascade completes.</summary>
  private readonly CancellationTokenSource _stoppedCts = new();

  private ILogger? FrameworkLogger
    => MainShell?.Services?.GetService<ILoggerFactory>()?.CreateLogger("Everlong.Nester.AppLifetime");

  /// <inheritdoc />
  public CancellationToken Stopping => _stoppingCts.Token;

  /// <inheritdoc />
  public CancellationToken Stopped => _stoppedCts.Token;

  /// <summary>
  ///   Common host initialization, invoked by platform constructors after
  ///   their platform-specific wiring: the global error hooks (AppDomain /
  ///   TaskScheduler).
  /// </summary>
  protected void InitializeHost(IErrorHandler? errorHandler, ErrorHandlingOptions errorHandling)
  {
    if (errorHandler is not null)
    {
      ErrorHandler = errorHandler;
      BindGlobalErrorHooks(errorHandling);
    }
  }

  private bool TryBeginDispose()
  {
    return Interlocked.CompareExchange(ref _disposeState, 1, 0) == 0;
  }

  /// <summary>
  ///   Tracks a shell for the app-exit teardown cascade.
  /// </summary>
  /// <remarks>
  ///   Idempotent: tracking a shell that was already released is a no-op.
  ///   Throws when the lifetime is already shutting down — a shell
  ///   registered after the cascade began would never be released.
  /// </remarks>
  public void Track(IShell shell)
  {
    ArgumentNullException.ThrowIfNull(shell);
    _registryGate.Wait();
    try
    {
      if (Volatile.Read(ref _disposeState) != 0)
      {
        throw new InvalidOperationException(
          "Cannot track a shell — the application lifetime is already shutting down.");
      }

      if (!Shells.Contains(shell))
      {
        Shells.Add(shell);
      }
    }
    finally
    {
      _registryGate.Release();
    }
  }

  /// <inheritdoc />
  public async ValueTask UntrackAsync(IShell shell)
  {
    ArgumentNullException.ThrowIfNull(shell);
    await _registryGate.WaitAsync();
    try
    {
      Shells.Remove(shell);
    }
    finally
    {
      _registryGate.Release();
    }
  }

  /// <summary>Removes a shell from the teardown registry (synchronous, gate-protected).</summary>
  protected void Untrack(IShell shell)
  {
    _registryGate.Wait();
    try
    {
      Shells.Remove(shell);
    }
    finally
    {
      _registryGate.Release();
    }
  }

  /// <summary>
  ///   Promotes the shell as the application's main shell and tracks it for
  ///   the app-exit teardown cascade.
  /// </summary>
  /// <remarks>
  ///   Pure declaration: presentation is driven by <see cref="IShell.Start" />.
  ///   Only an ACTIVE shell (assembly + pipeline activation done) may be
  ///   promoted — activation intents reach the main shell through its
  ///   pipeline, so a promoted shell always answers them.
  /// </remarks>
  public void SetMainShell(IShell shell)
  {
    ArgumentNullException.ThrowIfNull(shell);
    if (shell.Lifetime.Lifecycle != ShellLifecycle.Started)
    {
      throw new InvalidOperationException(
        "The shell is not active — SetMainShell requires a started shell (Start() completed its activation).");
    }

    if (ReferenceEquals(shell, MainShell))
    {
      return; // No-op
    }

    _registryGate.Wait();
    try
    {
      if (Volatile.Read(ref _disposeState) != 0)
      {
        throw new InvalidOperationException(
          "Cannot promote a shell — the application lifetime is already shutting down.");
      }

      MainShell = shell;
      if (!Shells.Contains(shell))
      {
        Shells.Add(shell);
      }

      // Hygiene: drop already-disposed shells from the tracking list (their
      // teardown is idempotent, so this is purely cosmetic).
      Shells.RemoveAll(s => s.Lifetime.Lifecycle == ShellLifecycle.Disposed);
    }
    finally
    {
      _registryGate.Release();
    }

    if (IsSingleView)
    {
      // Single-view: the previous shell (if any) is disposed and detached by
      // the platform implementation — only one MainView can exist at a time.
      ReplaceSingleViewShell(shell);
    }
  }

  /// <summary>Whether the platform presents a single view (one MainView) instead of multiple windows.</summary>
  public abstract bool IsSingleView { get; }

  /// <summary>Platform hook: disposes the previous single-view shell, if any.</summary>
  protected abstract void ReplaceSingleViewShell(IShell shellContext);

  /// <summary>Performs the platform-specific shutdown step.</summary>
  protected abstract void ShutdownCore(int exitCode);

  /// <summary>Releases every owned resource: shells, container root, dispatcher, broker and error handler.  Idempotent.</summary>
  public ValueTask DisposeAsync() => DisposeManagedResources();

  /// <summary>Releases every owned resource: shells, container root, dispatcher, broker and error handler.  Idempotent.</summary>
  protected async ValueTask DisposeManagedResources()
  {
    if (!TryBeginDispose())
    {
      return;
    }

    // Stop signal first — a broken observer must not abort the cascade (the
    // dispose guard is already set, so a failed cascade is not retryable).
    // Both CTSs stay undisposed on purpose: the signals must stay readable
    // after disposal (late observers), and a plain CTS has no resources
    // worth reclaiming.
    try
    {
      await _stoppingCts.CancelAsync();
    }
    catch
    {
      // ignore — cancellation is best-effort; the cascade below owns teardown
    }

    // Snapshot the live set under the registry gate — a shell's own
    // teardown may be settling on another context and untracking itself
    // concurrently (the gate is never held across an await).
    await _registryGate.WaitAsync();
    List<IShell> snapshot;
    try
    {
      snapshot = Shells.ToList();
    }
    finally
    {
      _registryGate.Release();
    }

    // Dispose all tracked shells (layers + window containers — each shell
    // owns its provider; each shell untracks itself as its teardown
    // completes, so the registry drains as the cascade walks the snapshot).
    foreach (var shell in snapshot)
    {
      try
      {
        await DisposeShellAsync(shell);
      }
      catch (Exception ex) { FrameworkLogger?.LogError(ex, "Error disposing shell"); }
    }

    // Leftovers: shells whose teardown never reached its own untrack (a
    // best-effort catch swallowed a mid-teardown failure).
    await _registryGate.WaitAsync();
    try
    {
      Shells.Clear();
    }
    finally
    {
      _registryGate.Release();
    }

    // Dispose the app lifetime's own components — each is owned here.  The
    // process-level container goes AFTER every window scope (the windows'
    // bridged references must die first); the dispatcher goes last (the
    // other components may still use it).
    await DisposeComponentAsync(Services);
    await DisposeComponentAsync(ErrorHandler);
    await DisposeComponentAsync(Dispatcher);

    // Teardown complete — the stopped signal fires last, after every owned
    // component was released.
    try
    {
      await _stoppedCts.CancelAsync();
    }
    catch
    {
      // best-effort — a broken observer must not fail the teardown
    }
  }

  /// <summary>Releases one owned component (async-aware, best-effort).</summary>
  private static async ValueTask DisposeComponentAsync(object? component)
  {
    if (component is null)
    {
      return;
    }

    try
    {
      switch (component)
      {
        case IAsyncDisposable asyncDisposable:
          await asyncDisposable.DisposeAsync();
          break;
        case IDisposable disposable:
          disposable.Dispose();
          break;
      }
    }
    catch (Exception)
    {
      // best-effort: a failing component must not break the teardown cascade
    }
  }

  /// <summary>
  ///   Releases one tracked shell (async-aware, best-effort).
  /// </summary>
  private async ValueTask DisposeShellAsync(IShell shell)
  {
    try
    {
      if (shell is IAsyncDisposable asyncDisposable)
        await asyncDisposable.DisposeAsync();
    }
    catch (Exception ex)
    {
      FrameworkLogger?.LogError(ex, "Error disposing shell");
    }
  }

  // ── Global error hooks ─────────────────────────

  private bool _rethrowUnhandledExceptions;
  private bool _markTaskSchedulerExceptionsAsObserved = true;

  private void BindGlobalErrorHooks(ErrorHandlingOptions options)
  {
    _rethrowUnhandledExceptions = options.RethrowUnhandledExceptions;
    _markTaskSchedulerExceptionsAsObserved = options.MarkTaskSchedulerExceptionsAsObserved;
    AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
    TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
  }

  private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
  {
    var exception = e.ExceptionObject as Exception
                    ?? new InvalidOperationException("Unhandled exception object is not Exception.");
    ReportAppError(exception);
  }

  private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
  {
    ReportAppError(e.Exception);
    if (_markTaskSchedulerExceptionsAsObserved)
    {
      e.SetObserved();
    }
  }

  /// <summary>Routes a host-level error to the configured handler (fallback: log/rethrow per options).</summary>
  protected void ReportAppError(Exception exception)
  {
    var handler = ErrorHandler;
    if (handler == null)
    {
      RethrowIfConfigured(exception);
      FrameworkLogger?.LogCritical(exception, "Host error without registered handler");
      return;
    }

    try
    {
      var handled = handler.HandleError(exception);
      if (!handled)
      {
        RethrowIfConfigured(exception);
      }
    }
    catch (Exception e)
    {
      Console.WriteLine("Critical error in host error handler: " + e.Message);
      FrameworkLogger?.LogCritical(e, "Critical error in host error handler");
    }
  }

  private void RethrowIfConfigured(Exception exception)
  {
    if (!_rethrowUnhandledExceptions)
    {
      return;
    }

    // Surface through the dispatcher's unhandled-exception path — always
    // post, never throw synchronously: callers wrap this in try/catch, so a
    // synchronous rethrow (the previous CheckAccess fast path) was swallowed
    // — the "rethrow" never rethrew.
    Dispatcher.SurfaceUnhandled(exception);
  }
}
