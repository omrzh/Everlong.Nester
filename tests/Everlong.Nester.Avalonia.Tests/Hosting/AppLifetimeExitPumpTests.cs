using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Everlong.Nester.Hosting;
using Everlong.Nester.Shell;
using Everlong.Nester.Threading;
using Xunit;

namespace Everlong.Nester.Tests.Hosting;

/// <summary>
///   The Avalonia exit pump: the exit hook keeps the dispatcher's loop alive
///   while the teardown cascade settles.  A cascade that hops back to the UI
///   thread — every real shell teardown does — would otherwise be cut off the
///   moment the exit handler returned.
/// </summary>
public sealed class AppLifetimeExitPumpTests : IDisposable
{
  public void Dispose() => MainDispatcher.ResetForTesting();

  [AvaloniaFact]
  public void PumpUntil_RunsDispatcherQueuedContinuations_UntilTheTaskSettles()
  {
    var settled = new TaskCompletionSource();

    // The continuation a cascade awaits is queued on this dispatcher; an
    // unpumped frame (the old `async void` handler) leaves it stranded.
    Dispatcher.UIThread.Post(() => settled.SetResult());

    AppLifetimeImpl.PumpUntil(settled.Task);

    Assert.True(settled.Task.IsCompleted, "Pumping the frame must run the queued continuation.");
  }

  [AvaloniaFact]
  public void RunExitTeardown_PumpsADispatcherBoundCascade_ToCompletion()
  {
    var lifetime = new PumpingLifetime(new DispatcherYieldingComponent());

    lifetime.Exit();

    Assert.True(lifetime.Stopped.IsCancellationRequested,
      "The exit hook returns only once the cascade settled.");
  }

  /// <summary>The base host wired to the real Avalonia pump — the seam the platform exit handler uses.</summary>
  private sealed class PumpingLifetime : AppLifetimeBase
  {
    public PumpingLifetime(IServiceProvider services)
    {
      Dispatcher = new AvaloniaMainDispatcher();
      Services = services;
    }

    public override bool IsSingleView => false;

    public void Exit() => RunExitTeardown(AppLifetimeImpl.PumpUntil);

    protected override void ReplaceSingleViewShell(IShell shellContext)
    {
    }

    protected override void ShutdownCore(int exitCode)
    {
    }
  }

  /// <summary>Releases through one dispatcher hop — the shape a real teardown awaits.</summary>
  private sealed class DispatcherYieldingComponent : IServiceProvider, IAsyncDisposable
  {
    public object? GetService(Type serviceType) => null;

    public async ValueTask DisposeAsync() => await Task.Yield();
  }
}
