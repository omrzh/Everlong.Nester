namespace Everlong.Nester.Activation;

/// <summary>
///   Desktop activation agent for Linux.
/// </summary>
/// <remarks>
///   Leader election, argument forwarding, and the handshake protocol are
///   inherited; foreground activation is a no-op.
/// </remarks>
public sealed class LinuxActivationAgent : DesktopActivationAgent
{
  /// <summary>Creates the Linux activation agent.</summary>
  public LinuxActivationAgent(
    string? leaseName = null,
    bool enableNegotiation = false,
    TimeSpan? negotiateTimeout = null)
    : base(leaseName, enableNegotiation, negotiateTimeout)
  {
  }

  // TODO: X11 foreground activation (e.g. XSetInputFocus / xdotool) once a
  //       display target is verified (WSLg GUI passthrough is a test bed).
}
