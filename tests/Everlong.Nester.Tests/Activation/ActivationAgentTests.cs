using Everlong.Nester.Activation;
using Everlong.Nester.Intent;
using Xunit;

namespace Everlong.Nester.Tests.Activation;

/// <summary>
///   The agent's host-channel contract: conversion, parking (no channel),
///   drain (<see cref="IActivationAgent.FlushAsync" />), and the
///   identity-guarded channel binding.
/// </summary>
public sealed class ActivationAgentTests
{
  private sealed record FakeActivationIntent : IActivationIntent;

  /// <summary>Records dispatched intents; decides by intent type.</summary>
  private sealed class RecordingChannel : IActivationChannel
  {
    public List<IActivationIntent> Intents { get; } = [];

    public bool HandleAll { get; set; }

    public nint HostHandle => 0;

    public ValueTask<IntentResult> DispatchAsync(IActivationIntent intent)
    {
      Intents.Add(intent);
      return new(HandleAll ? IntentResult.Handled : IntentResult.Pass);
    }
  }

  private sealed class TestAgent : ActivationAgentBase
  {
    public ValueTask<IntentResult> Forward(IActivationIntent intent) => ForwardAsync(intent);
  }

  /// <summary>An agent whose startup input is fixed, so the test never reads the runner's own command line.</summary>
  private sealed class StartupInputAgent(IActivationIntent startup) : ActivationAgentBase
  {
    public override IReadOnlyList<IActivationIntent> Convert(IReadOnlyList<string> args) => [startup];
  }

  [Fact]
  public async Task ForwardWithoutChannel_Parks()
  {
    var agent = new TestAgent();
    var intent = new FakeActivationIntent();

    var result = await agent.Forward(intent);

    Assert.Equal(IntentResult.Pass, result);
    Assert.Equal([intent], agent.TakePendingIntents());
    Assert.Empty(agent.TakePendingIntents());   // drained
  }

  [Fact]
  public async Task ForwardWithChannel_DispatchesThroughTheHostChannel()
  {
    var agent = new TestAgent();
    var channel = new RecordingChannel { HandleAll = true };
    agent.Bind(channel);
    var intent = new FakeActivationIntent();

    var result = await agent.Forward(intent);

    Assert.Equal(IntentResult.Handled, result);
    Assert.Equal([intent], channel.Intents);
    Assert.Empty(agent.TakePendingIntents());   // nothing parked
  }

  [Fact]
  public async Task FlushAsync_DrainsParkedThroughChannel_ReturnsDecided()
  {
    var agent = new TestAgent();
    var parked = new FakeActivationIntent();
    await agent.Forward(parked);   // no channel yet — parked
    var channel = new RecordingChannel { HandleAll = true };
    agent.Bind(channel);

    bool decided = await agent.FlushAsync();

    Assert.True(decided);
    Assert.Contains(parked, channel.Intents);
    Assert.Empty(agent.TakePendingIntents());
  }

  [Fact]
  public async Task FlushAsync_WithNothingHandled_ReturnsFalse()
  {
    var agent = new TestAgent();
    var channel = new RecordingChannel { HandleAll = false };
    agent.Bind(channel);

    bool decided = await agent.FlushAsync();

    Assert.False(decided);
    Assert.Empty(agent.TakePendingIntents());
  }

  [Fact]
  public async Task FlushAsync_WithoutChannel_KeepsInputQueued()
  {
    var startup = new FakeActivationIntent();
    var agent = new StartupInputAgent(startup);

    bool decided = await agent.FlushAsync();

    Assert.False(decided);
    // The startup input (converted args) stays queued — a later FlushAsync
    // drains it once a channel binds.
    Assert.Equal([startup], agent.TakePendingIntents());
  }

  [Fact]
  public async Task TakePendingIntents_Drains()
  {
    var agent = new TestAgent();
    var intent = new FakeActivationIntent();
    await agent.Forward(intent);

    Assert.Equal([intent], agent.TakePendingIntents());
    Assert.Empty(agent.TakePendingIntents());
  }

  [Fact]
  public async Task Bind_LastChannelWins()
  {
    var agent = new TestAgent();
    var first = new RecordingChannel();
    var second = new RecordingChannel { HandleAll = true };

    agent.Bind(first);
    agent.Bind(second);

    await agent.Forward(new FakeActivationIntent());

    Assert.Empty(first.Intents);
    Assert.Single(second.Intents);
  }

  [Fact]
  public async Task Unbind_ByTheBoundChannel_ReleasesAndParksAgain()
  {
    var agent = new TestAgent();
    var channel = new RecordingChannel();
    agent.Bind(channel);

    bool released = await agent.UnbindAsync(channel);

    Assert.True(released);
    var intent = new FakeActivationIntent();
    await agent.Forward(intent);
    Assert.Empty(channel.Intents);
    Assert.Equal([intent], agent.TakePendingIntents());
  }

  [Fact]
  public async Task Unbind_ByASupersededChannel_LeavesTheBindingUntouched()
  {
    var agent = new TestAgent();
    var first = new RecordingChannel();
    var second = new RecordingChannel();
    agent.Bind(first);
    agent.Bind(second);

    bool released = await agent.UnbindAsync(first);

    Assert.False(released);
    await agent.Forward(new FakeActivationIntent());
    Assert.Single(second.Intents);
  }
}
