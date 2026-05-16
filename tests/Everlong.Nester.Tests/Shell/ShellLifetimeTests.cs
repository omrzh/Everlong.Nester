using Everlong.Nester.Shell;
using Xunit;

namespace Everlong.Nester.Tests.Shell;

/// <summary>
///   <see cref="ShellLifetime"/> contract: the single forward lifecycle walk
///   and the startup / teardown signals it owns.
/// </summary>
public class ShellLifetimeTests
{
  [Fact]
  public void Advance_WalksForward_ExactlyOnce()
  {
    var lifetime = new ShellLifetime();

    Assert.Equal(ShellLifecycle.Created, lifetime.Lifecycle);
    lifetime.Advance(ShellLifecycle.Assembling);
    lifetime.Advance(ShellLifecycle.Assembled);
    lifetime.Advance(ShellLifecycle.Started);
    lifetime.Advance(ShellLifecycle.Disposed);

    Assert.Equal(ShellLifecycle.Disposed, lifetime.Lifecycle);
  }

  [Fact]
  public void Advance_SkippingAState_Throws_AndLeavesTheStateUntouched()
  {
    var lifetime = new ShellLifetime();

    Assert.Throws<InvalidOperationException>(() => lifetime.Advance(ShellLifecycle.Started));

    Assert.Equal(ShellLifecycle.Created, lifetime.Lifecycle);
  }

  [Fact]
  public void Advance_Backwards_Throws()
  {
    var lifetime = new ShellLifetime();
    lifetime.Advance(ShellLifecycle.Assembling);
    lifetime.Advance(ShellLifecycle.Assembled);

    Assert.Throws<InvalidOperationException>(() => lifetime.Advance(ShellLifecycle.Assembling));
    Assert.Equal(ShellLifecycle.Assembled, lifetime.Lifecycle);
  }

  [Fact]
  public void Advance_Disposed_IsLegalFromAnyState_AndIdempotent()
  {
    var lifetime = new ShellLifetime();

    lifetime.Advance(ShellLifecycle.Disposed);
    lifetime.Advance(ShellLifecycle.Disposed);

    Assert.Equal(ShellLifecycle.Disposed, lifetime.Lifecycle);
  }

  [Fact]
  public void Advance_Assembling_ReEntryIsIdempotent()
  {
    var lifetime = new ShellLifetime();

    lifetime.Advance(ShellLifecycle.Assembling);
    lifetime.Advance(ShellLifecycle.Assembling); // retry after a failed Start

    Assert.Equal(ShellLifecycle.Assembling, lifetime.Lifecycle);
  }

  [Fact]
  public async Task Startup_CompletesOnce_ALateFaultIsIgnored()
  {
    var lifetime = new ShellLifetime();
    Assert.False(lifetime.Startup.IsCompleted);

    lifetime.CompleteStartup();
    await lifetime.Startup; // completes — no fault

    lifetime.FaultStartup(new InvalidOperationException("late"));
    Assert.True(lifetime.Startup.IsCompletedSuccessfully, "A settled signal never reopens.");
  }

  [Fact]
  public async Task Startup_Faults_WhenTheStartupFlowFailed()
  {
    var lifetime = new ShellLifetime();

    lifetime.FaultStartup(new InvalidOperationException("boom"));

    var cause = await Assert.ThrowsAsync<InvalidOperationException>(() => lifetime.Startup);
    Assert.Equal("boom", cause.Message);
  }

  [Fact]
  public async Task Teardown_StoppingFiresBeforeStopped()
  {
    var lifetime = new ShellLifetime();
    var order = new List<string>();
    using var stopping = lifetime.Stopping.Register(() => order.Add("stopping"));
    using var stopped = lifetime.Stopped.Register(() => order.Add("stopped"));

    Assert.False(lifetime.Stopping.IsCancellationRequested);
    Assert.False(lifetime.Stopped.IsCancellationRequested);

    await lifetime.BeginTeardownAsync();
    Assert.True(lifetime.Stopping.IsCancellationRequested);
    Assert.False(lifetime.Stopped.IsCancellationRequested, "Stopped waits for the cascade.");

    await lifetime.CompleteTeardownAsync();
    Assert.Equal(new[] { "stopping", "stopped" }, order);
  }
}
