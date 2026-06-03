using Everlong.Nester.Messaging;

namespace Everlong.Nester.Activation;

/// <summary>
///   Activation agent for single-view platforms (Android, iOS, Browser).
/// </summary>
/// <remarks>
///   The OS guarantees a single instance, so the desktop negotiation surface
///   (<see cref="IDesktopActivationAgent" />) does not apply. OS activation
///   events are subscribed at construction and translated into activation
///   intents, forwarded through the host channel or parked.
/// </remarks>
public sealed class SingleViewActivationAgent : ActivationAgentBase
{
  private readonly ActivatableLifetimeBridge _bridge;

  /// <summary>Creates the single-view activation agent.</summary>
  public SingleViewActivationAgent(IMessageHub? messageHub = null)
  {
    _bridge = new ActivatableLifetimeBridge(ForwardAsync, messageHub);
    _bridge.Subscribe();
  }

  /// <inheritdoc />
  public override ValueTask DisposeAsync()
  {
    _bridge.Dispose();
    return ValueTask.CompletedTask;
  }
}
