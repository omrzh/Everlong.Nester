using Everlong.Nester.Hosting;
using Everlong.Nester.Shell;
using Everlong.Nester.Threading;
using Xunit;

namespace Everlong.Nester.Tests.Hosting;

/// <summary>
///   <see cref="AppLifetimeBase"/> teardown-signal contract:
///   <see cref="IHostLifetime.Stopping"/> fires when the cascade begins,
///   <see cref="IHostLifetime.Stopped"/> when it completes, and both stay
///   readable after disposal.
/// </summary>
public sealed class AppLifetimeSignalsTests : IDisposable
{
  public void Dispose() => MainDispatcher.ResetForTesting();

  [Fact]
  public async Task DisposeAsync_FiresStoppingThenStopped_InOrder()
  {
    var lifetime = new TestAppLifetime();
    var order = new List<string>();
    using var stopping = lifetime.Stopping.Register(() => order.Add("stopping"));
    using var stopped = lifetime.Stopped.Register(() => order.Add("stopped"));

    Assert.False(lifetime.Stopping.IsCancellationRequested);
    Assert.False(lifetime.Stopped.IsCancellationRequested);

    await lifetime.DisposeAsync();

    Assert.Equal(new[] { "stopping", "stopped" }, order);
  }

  [Fact]
  public async Task DisposeAsync_IsIdempotent_AndSignalsStayReadable()
  {
    var lifetime = new TestAppLifetime();

    await lifetime.DisposeAsync();
    await lifetime.DisposeAsync();

    Assert.True(lifetime.Stopping.IsCancellationRequested);
    Assert.True(lifetime.Stopped.IsCancellationRequested);
    Assert.True(lifetime.Stopping.CanBeCanceled, "The signals stay readable after disposal — never disposed.");
  }

  /// <summary>The host under test — the real base, the real teardown cascade.</summary>
  private sealed class TestAppLifetime : AppLifetimeBase
  {
    public TestAppLifetime() => Dispatcher = new StubDispatcher();

    public override bool IsSingleView => false;

    protected override void ReplaceSingleViewShell(IShell shellContext)
    {
    }

    protected override void ShutdownCore(int exitCode)
    {
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
