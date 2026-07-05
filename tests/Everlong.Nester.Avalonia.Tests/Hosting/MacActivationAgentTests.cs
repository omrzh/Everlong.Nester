using Avalonia.Headless.XUnit;
using Xunit;
using Everlong.Nester.Activation;
using Everlong.Nester.Intent;

namespace Everlong.Nester.Tests.Hosting;

/// <summary>
///   Platform-specialized agent conversion: the macOS agent filters the
///   LaunchServices noise argument (<c>-psn_XXX</c>) before the inherited
///   conservative heuristics run.
/// </summary>
public sealed class MacActivationAgentTests
{
  /// <summary>An inert host channel — the agent only needs a binding target.</summary>
  private sealed class InertChannel : IActivationChannel
  {
    public nint HostHandle => 0;
    public ValueTask<IntentResult> DispatchAsync(IActivationIntent intent) => new(IntentResult.Pass);
  }
  [AvaloniaFact]
  public void Convert_FiltersPsnNoise_ThenAppliesBaseHeuristics()
  {
    var agent = new MacActivationAgent();
    try
    {
      var intents = agent.Convert(["-psn_0_12345678", "https://example.com/deep"]);

      var uri = Assert.Single(intents.OfType<UriActivationIntent>());
      Assert.Equal(new Uri("https://example.com/deep"), uri.Uri);
    }
    finally
    {
      agent.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
  }

  [AvaloniaFact]
  public void Convert_OnlyPsnNoise_YieldsNothing()
  {
    var agent = new MacActivationAgent();
    try
    {
      Assert.Empty(agent.Convert(["-psn_0_87654321"]));
    }
    finally
    {
      agent.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
  }

  [AvaloniaFact]
  public void NegotiationDisabled_ByDefault_IsOwnPrimaryWithoutLease()
  {
    // EnableNegotiation defaults to false: the agent keeps its platform
    // identity (conversion, OS events) without claiming single-instance —
    // no lease is acquired (a second instance would not be negotiated with).
    var agent = new MacActivationAgent();
    try
    {
      Assert.True(agent.IsLeader);
      Assert.False(agent.TryNegotiate(["--whatever"]));
      agent.Bind(new InertChannel());   // must not start a pipe server
    }
    finally
    {
      agent.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
  }
}
