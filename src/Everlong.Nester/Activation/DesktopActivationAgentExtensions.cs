namespace Everlong.Nester.Activation;

/// <summary>
///   Extension methods for <see cref="IDesktopActivationAgent" />.
/// </summary>
public static class DesktopActivationAgentExtensions
{
  /// <summary>
  ///   Runs the startup activation flow: agents without the desktop
  ///   negotiation surface (single-view, inert) are always primary; desktop
  ///   leaders return <see cref="ActivationOutcome.Primary" /> (serving starts
  ///   at the first channel binding); desktop followers synchronously
  ///   negotiate (blocks until the leader responds or times out).
  /// </summary>
  /// <returns>
  ///   <see cref="ActivationOutcome.Primary" /> when this process is the
  ///   primary instance; <see cref="ActivationOutcome.Yield" /> when the
  ///   leader accepted this activation (follower should exit);
  ///   <see cref="ActivationOutcome.Proceed" /> when the leader declined or
  ///   was unreachable (follower should proceed).
  /// </returns>
  public static ActivationOutcome Negotiate(this IDesktopActivationAgent agent)
  {

    if (agent.IsLeader)
    {
      // Primary: serving starts when a channel binds (Bind starts the
      // listener) — handshakes must never arrive
      // before the agent can dispatch.
      return ActivationOutcome.Primary;
    }

    // Follower: synchronously negotiate (blocks until leader responds or times out)
    bool yielded = agent.TryNegotiate(ActivationArgs.Current);
    return yielded ? ActivationOutcome.Yield : ActivationOutcome.Proceed;
  }
}

// ── ActivationOutcome ──────────────────────────────

/// <summary>
///   Describes how the current process should proceed during activation.
/// </summary>
public enum ActivationOutcome
{
  /// <summary>The current process is the primary instance.</summary>
  Primary,

  /// <summary>The current process may continue as a secondary instance.</summary>
  Proceed,

  /// <summary>The current process should yield to another instance and exit.</summary>
  Yield
}
