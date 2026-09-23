using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Everlong.Nester.Activation;
using Everlong.Nester.Presentation;
using Everlong.Nester.Diagnostics;
using Everlong.Nester.Hosting;
using Everlong.Nester.Intent;
using Everlong.Nester.Routing;
using Everlong.Nester.Shell;
using Everlong.Nester.Threading;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Everlong.Nester.Tests.Hosting;
using PlatformControl = Avalonia.Controls.Control;

namespace Everlong.Nester.Tests.Shell;

/// <summary>
///   Error-path regression tests: unhandled errors escalate to the terminal
///   hook (<see cref="ShellBase.OnUnhandledError"/> — default rethrow),
///   startup errors route through the UI dispatcher, and teardown survives
///   broken cancellation observers.
/// </summary>
[Collection("RealShell")]
public sealed class ShellErrorPathTests
{
  /// <summary>Director that handles nothing — the error chain reaches the fallback.</summary>
  private sealed class ErrorPassthrough : IShellDirector
  {
    public ValueTask HandleAsync(IntentContext context, IntentDelegate next) => next(context);
    public void OnAssembled(IShell shell) { }
    public bool HandleError(Exception exception) => false;
  }

  /// <summary>Director whose intent handling throws (the startup activation passes) and whose error handler declines — the dispatch faults.</summary>
  private sealed class ThrowingDirector : IShellDirector
  {

    public ValueTask HandleAsync(IntentContext context, IntentDelegate next)
      => context.Intent is ShellActivationIntent
        ? next(context)
        : throw new InvalidOperationException("director boom");

    public void OnAssembled(IShell shell) { }
    public bool HandleError(Exception exception) => false;
  }

  /// <summary>Director whose intent handling faults during the startup dispatch — the error is routed to its own error handler (the dispatch absorbs it).</summary>
  private sealed class FaultingDirector : IShellDirector
  {
    public Exception? ObservedError { get; private set; }

    public ValueTask HandleAsync(IntentContext context, IntentDelegate next)
      => throw new InvalidOperationException("startup boom");

    public void OnAssembled(IShell shell) { }
    public bool HandleError(Exception exception)
    {
      ObservedError = exception;
      return true;
    }
  }

  /// <summary>Host view (the default presentation chain awaits the director coroutine — the shell's OnStarted default).</summary>
  private sealed class ChainHost : ContentControl, IAvaloniaShellHost
  {
    private readonly ContentLayer _contentLayer = new();

    public void HostShell(IAvaloniaShell shell, PlatformControl stage) => _contentLayer.Content = stage;
  }

  /// <summary>Singleton disposal probe — records whether the window container was released.</summary>
  private sealed class ReleaseProbe(Action onRelease) : IDisposable
  {
    public void Dispose() => onRelease();
  }

  /// <summary>Dispatcher that records posted work instead of running it — deterministic UI-thread simulation.</summary>
  private sealed class RecordingDispatcher(bool onMainThread) : IMainDispatcher
  {
    public List<Func<Task>> Posted { get; } = [];

    public bool CheckAccess() => onMainThread;

    public void Post(Action action, DispatchPriority priority = DispatchPriority.Normal)
      => Posted.Add(() =>
      {
        action();
        return Task.CompletedTask;
      });

    public Task InvokeAsync(Action action, DispatchPriority priority = DispatchPriority.Normal)
    {
      Posted.Add(() =>
      {
        action();
        return Task.CompletedTask;
      });
      return Task.CompletedTask;
    }

    public Task InvokeAsync(Func<Task> callback, DispatchPriority priority = DispatchPriority.Normal)
    {
      Posted.Add(callback);
      return Task.CompletedTask;
    }
  }

  /// <summary>Minimal host — only the dispatcher matters for the error path.</summary>
  private sealed class FakeLifetime(IMainDispatcher dispatcher) : IAppLifetime
  {
    public IShell? MainShell => null;
    public IMainDispatcher Dispatcher { get; } = dispatcher;
    public IErrorHandler? ErrorHandler => null;
    public IServiceProvider? Services => null;
    public CancellationToken Stopping => CancellationToken.None;
    public CancellationToken Stopped => CancellationToken.None;
    public bool IsSingleView => false;
    public void Track(IShell shell) { }
    public ValueTask UntrackAsync(IShell shell) => ValueTask.CompletedTask;
    public void SetMainShell(IShell context) => throw new NotSupportedException();
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
  }

  private static IDisposable BindFacade(IMainDispatcher dispatcher)
  {
    AppLifetime.BindImpl(new FakeLifetime(dispatcher));
    MainDispatcher.BindInstance(dispatcher); // single channel: the shell's UI paths consult MainDispatcher
    return new ResetFacade();
  }

  private sealed class ResetFacade : IDisposable
  {
    public void Dispose()
    {
      AppLifetime.ResetForTesting();
      MainDispatcher.ResetForTesting();
    }
  }


  /// <summary>Shell that records the terminal escalation instead of rethrowing — the unassembled error path.</summary>
  private sealed class HookRecordingShell : AvaloniaShell
  {
    public Exception? Unhandled { get; private set; }

    protected override IServiceProvider InitializeServices() => null!; // unassembled on purpose

    protected override void OnUnhandledError(Exception exception) => Unhandled = exception;
  }

  // ── Startup error routing (P6): a faulting Director's dispatch error is absorbed by its own handler ──

  [AvaloniaFact]
  public async Task FaultedStartupDispatch_RoutesErrorToDirectorHandler()
  {
    // The startup dispatch (empty ShellActivationIntent) faults inside the
    // Director's intent handling; the per-handler isolation contract
    // reports it to the Director's error handler — the Startup signal still
    // completes (the dispatch absorbs the error; waiters never hang).
    using var facade = BindFacade(new RecordingDispatcher(onMainThread: false));
    IShell shell = TestHost.CreateShell<FaultingDirector>(rootView: new ChainHost());
    shell.Start();

    await shell.Lifetime.Startup;

    var director = (FaultingDirector)((AvaloniaShell)shell).Director!;
    Assert.IsType<InvalidOperationException>(director.ObservedError);
  }

  // ── Teardown robustness (P3) ──

  [AvaloniaFact]
  public async Task DisposeAsync_ThrowingCancelCallback_StillReleasesContainer()
  {
    // A broken cancellation observer must not abort the teardown cascade
    // (the dispose guard is already set — a failed cascade is not retryable).
    bool released = false;
    IShell shell = TestHost.CreateShell<ErrorPassthrough>(s =>
                                                            s.AddSingleton(_ =>
                                                                             new ReleaseProbe(() => released =
                                                                               true))); // factory form: the container owns and disposes it
    shell.Start();
    await shell.Lifetime.Startup;

    _ = shell.Services!.GetRequiredService<ReleaseProbe>(); // instantiate — the container disposes what it created

    ((AvaloniaShell)shell).Lifetime.Stopping.Register(() => throw new InvalidOperationException("observer boom"));

    await ((IAsyncDisposable)shell).DisposeAsync();

    Assert.True(released);
    Assert.Equal(ShellLifecycle.Disposed, ((AvaloniaShell)shell).Lifetime.Lifecycle);
    // Lifetime.Stopping stays readable after disposal (window-owned tasks observe it late).
    Assert.True(((AvaloniaShell)shell).Lifetime.Stopping.IsCancellationRequested);
  }

  // ── Error-path guard (P4/P5) ──

  [AvaloniaFact]
  public void ReportError_BeforeAssembly_EscalatesToTerminalHook()
  {
    // No Director, no host handler — the error escalates straight to the
    // terminal hook; the error path never NREs on an unassembled shell
    // (no Director, no container).
    var shell = new HookRecordingShell();

    var error = new InvalidOperationException("early");
    shell.ReportError(error);

    Assert.Same(error, shell.Unhandled);
  }

  // ── Intent chain: a non-guarding Director answers through its error handler ──

  [AvaloniaFact]
  public async Task DispatchIntent_ThrowingDirector_DeclinedByHandleError_FaultsTheDispatch()
  {
    // The Director did not guard its handling and its error handler
    // declines — the exception faults the dispatch caller instead of
    // continuing to the host.
    using var facade = BindFacade(new RecordingDispatcher(onMainThread: true));
    IShell shell = TestHost.CreateShell<ThrowingDirector>(rootView: new ChainHost());
    shell.Start();
    await shell.Lifetime.Startup;

    await Assert.ThrowsAsync<InvalidOperationException>(
      () => shell.DispatchIntent(null, new BackIntent()).AsTask());
  }

  [AvaloniaFact]
  public async Task DispatchIntent_ThrowingDirector_AcceptedByHandleError_SettlesAsPass()
  {
    // The Director's error handler accepts the exception — the dispatch
    // settles as Pass: no continuation, no fault.
    using var facade = BindFacade(new RecordingDispatcher(onMainThread: true));
    IShell shell = TestHost.CreateShell<FaultingDirector>(rootView: new ChainHost());
    shell.Start();
    await shell.Lifetime.Startup;

    Assert.Equal(IntentResult.Pass, await shell.DispatchIntent(null, new BackIntent()));
  }
}
