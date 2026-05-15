using Everlong.Nester.Activation;
using Everlong.Nester.Intent;
using Microsoft.Extensions.DependencyInjection;

namespace Everlong.Nester.Shell;

partial class ShellBase : IActivationChannel
{
  // ── Activation face ── IActivationChannel (the shell is the agent's host
  //    face — it delivers activation intents into its own pipeline and
  //    publishes the host window handle). ──

  /// <summary>
  ///   The declared activation agent — assigned before <see cref="Start" />;
  ///   <see langword="null" /> resolves from the shell's container.  A
  ///   declared agent is owned by this shell and released at teardown.
  ///   Registering one in the shell's container is the late-assignment
  ///   channel (the shell binds it, the registrant owns it).
  /// </summary>
  public IActivationAgent? ActivationAgent { get; init; }

  /// <summary>The bound agent (declared ?? container-resolved); <see langword="null" /> = no activation surface.</summary>
  private IActivationAgent? _boundAgent;

  /// <summary>Whether the declared agent supplies the binding — the declaration transfers ownership.</summary>
  private bool _ownsAgent;

  /// <summary>
  ///   Binds the activation agent: the declared agent wins, otherwise the
  ///   shell's container supplies one; absent = no activation surface.
  /// </summary>
  private void BindAgent()
  {
    _ownsAgent = ActivationAgent is not null;
    _boundAgent = ActivationAgent ?? ShellServiceScope!.ServiceProvider.GetService<IActivationAgent>();
    _boundAgent?.Bind(this);
  }

  /// <summary>
  ///   Releases the activation agent: unbinds (the agent stops serving only
  ///   when this shell is still the bound channel) and disposes a declared
  ///   agent — a relayed agent outlives the shell that handed it over.
  /// </summary>
  private async ValueTask ReleaseAgentAsync()
  {
    if (_boundAgent is not { } agent)
      return;

    bool lastBound = await agent.UnbindAsync(this);
    _boundAgent = null;
    if (_ownsAgent && lastBound)
      await agent.DisposeAsync();
  }

  /// <inheritdoc />
  nint IActivationChannel.HostHandle => HostHandle;

  /// <inheritdoc />
  ValueTask<IntentResult> IActivationChannel.DispatchAsync(IActivationIntent intent)
    => DispatchIntent(_boundAgent, intent);
}
