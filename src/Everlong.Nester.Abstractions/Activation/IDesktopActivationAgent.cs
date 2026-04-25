namespace Everlong.Nester.Activation;

/// <summary>
///   Cross-process activation over a named-pipe handshake (leader
///   election).
/// </summary>
public interface IDesktopActivationAgent : IActivationAgent
{
  /// <summary>
  ///   Returns <see langword="true" /> when this process is the primary
  ///   instance (the leader lease host).  Without negotiation enabled, this
  ///   process is its own primary instance — always <see langword="true" />.
  /// </summary>
  bool IsLeader { get; }

  /// <summary>
  ///   Synchronously negotiates with the primary instance (follower only;
  ///   blocks the calling thread until the handshake finishes or the connect
  ///   timeout expires).
  /// </summary>
  /// <returns>
  ///   <see langword="true" /> when the leader accepted this activation
  ///   (follower should exit); <see langword="false" /> when the leader
  ///   declined or was unreachable (follower should proceed).
  /// </returns>
  bool TryNegotiate(IReadOnlyList<string> args);
}
