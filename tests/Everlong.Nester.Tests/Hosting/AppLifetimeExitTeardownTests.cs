using System.Runtime.ExceptionServices;
using Everlong.Nester.Diagnostics;
using Everlong.Nester.Hosting;
using Everlong.Nester.Shell;
using Everlong.Nester.Threading;
using Xunit;

namespace Everlong.Nester.Tests.Hosting;

/// <summary>
///   The exit-hook contract: <see cref="AppLifetimeBase" /> runs the teardown
///   cascade by pumping the platform loop instead of awaiting it.  A desktop
///   toolkit tears its loop down the moment the exit handler returns, so a
///   cascade left awaiting would be cut off at its first continuation.
/// </summary>
public sealed class AppLifetimeExitTeardownTests : IDisposable
{
  public void Dispose() => MainDispatcher.ResetForTesting();

  [Fact]
  public void RunExitTeardown_PumpsWhileTheCascadeIsPending_ThenSettles()
  {
    using var gate = new ManualResetEventSlim(false);
    var lifetime = new TestAppLifetime(new GatedComponent(gate));

    bool pendingWhenPumped = false;
    OnPlatformThread(() => lifetime.Exit(task =>
    {
      pendingWhenPumped = !task.IsCompleted;
      gate.Set(); // the platform loop would be the thing that opens the gate
    }));

    Assert.True(pendingWhenPumped, "The exit hook pumps a cascade that has not settled yet.");
    Assert.True(lifetime.Stopped.IsCancellationRequested, "The hook returns only once the cascade settled.");
  }

  [Fact]
  public void RunExitTeardown_ReportsAPumpFailure_NeverThrowsIntoTheExitPath()
  {
    var reported = new List<Exception>();
    using var gate = new ManualResetEventSlim(false);
    var lifetime = new TestAppLifetime(new GatedComponent(gate));
    lifetime.ObserveErrors(new RecordingErrorHandler(reported));

    var pumpFailure = new InvalidOperationException("the platform loop blew up");
    OnPlatformThread(() => lifetime.Exit(_ =>
    {
      gate.Set();       // release the cascade so it is observed, never faulted
      throw pumpFailure;
    }));

    Assert.Same(pumpFailure, Assert.Single(reported));
    Assert.True(lifetime.Stopped.WaitHandle.WaitOne(TimeSpan.FromSeconds(5)),
      "The cascade still settles even when the pump fails.");
  }

  [Fact]
  public async Task RunExitTeardown_SkipsThePump_WhenTheCascadeAlreadySettled()
  {
    var lifetime = new TestAppLifetime();
    await lifetime.DisposeAsync();   // the exit hook runs after an explicit dispose

    bool pumped = false;
    lifetime.Exit(_ => pumped = true);

    Assert.False(pumped, "A settled cascade has nothing left for the pump to drive.");
    Assert.True(lifetime.Stopped.IsCancellationRequested);
  }

  /// <summary>
  ///   Runs the exit hook off the test's synchronization context — a real
  ///   exit handler sits on the platform's thread, where the cascade's
  ///   continuations never need this one to resume.
  /// </summary>
  private static void OnPlatformThread(Action exitHook)
  {
    ExceptionDispatchInfo? failure = null;
    var thread = new Thread(() =>
    {
      try
      {
        exitHook();
      }
      catch (Exception ex)
      {
        failure = ExceptionDispatchInfo.Capture(ex);
      }
    })
    { IsBackground = true };

    thread.Start();
    Assert.True(thread.Join(TimeSpan.FromSeconds(10)), "The exit teardown never settled.");
    failure?.Throw();
  }

  /// <summary>The host under test — the real base, the real cascade, a fake platform loop.</summary>
  private sealed class TestAppLifetime : AppLifetimeBase
  {
    public TestAppLifetime(IServiceProvider? services = null)
    {
      Dispatcher = new StubDispatcher();
      Services = services;
    }

    public override bool IsSingleView => false;

    /// <summary>Runs the platform exit hook — the base member both platform handlers call.</summary>
    public void Exit(Action<Task> pump) => RunExitTeardown(pump);

    /// <summary>Installs an error handler without binding the process-global error hooks.</summary>
    public void ObserveErrors(IErrorHandler handler) => ErrorHandler = handler;

    protected override void ReplaceSingleViewShell(IShell shellContext)
    {
    }

    protected override void ShutdownCore(int exitCode)
    {
    }
  }

  /// <summary>
  ///   A process component whose release parks until the platform loop opens
  ///   the gate — the cascade is genuinely pending when the exit hook takes
  ///   over.
  /// </summary>
  private sealed class GatedComponent : IServiceProvider, IAsyncDisposable
  {
    private readonly ManualResetEventSlim _gate;

    public GatedComponent(ManualResetEventSlim gate) => _gate = gate;

    public object? GetService(Type serviceType) => null;

    public async ValueTask DisposeAsync() => await Task.Run(_gate.Wait).ConfigureAwait(false);
  }

  /// <summary>Records every reported failure and claims it handled — the host's reporting contract, observed.</summary>
  private sealed class RecordingErrorHandler : IErrorHandler
  {
    private readonly List<Exception> _errors;

    public RecordingErrorHandler(List<Exception> errors) => _errors = errors;

    public bool HandleError(Exception exception)
    {
      _errors.Add(exception);
      return true;
    }
  }

  /// <summary>Inline dispatcher stub — the teardown path never needs real marshaling.</summary>
  private sealed class StubDispatcher : IMainDispatcher
  {
    public void Post(Action action, DispatchPriority priority = DispatchPriority.Normal) => action();

    public Task InvokeAsync(Action action, DispatchPriority priority = DispatchPriority.Normal)
    {
      action();
      return Task.CompletedTask;
    }

    public Task InvokeAsync(Func<Task> callback, DispatchPriority priority = DispatchPriority.Normal) => callback();

    public bool CheckAccess() => true;
  }
}
