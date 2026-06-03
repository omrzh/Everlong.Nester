using Everlong.Nester.Messaging;

namespace Everlong.Nester.Activation;

/// <summary>
///   Desktop activation agent for macOS: inherited named-pipe negotiation plus
///   OS activation events via <see cref="ActivatableLifetimeBridge" />.
/// </summary>
/// <remarks>
///   OS activation events are subscribed at construction — regardless of the
///   leader/follower outcome — so any running instance translates them.
/// </remarks>
public sealed class MacActivationAgent : DesktopActivationAgent
{
  private readonly ActivatableLifetimeBridge _bridge;

  /// <summary>Creates the macOS activation agent.</summary>
  public MacActivationAgent(
    string? leaseName = null,
    bool enableNegotiation = false,
    TimeSpan? negotiateTimeout = null,
    IMessageHub? messageHub = null)
    : base(leaseName, enableNegotiation, negotiateTimeout)
  {
    _bridge = new ActivatableLifetimeBridge(ForwardAsync, messageHub);
    _bridge.Subscribe();
  }

  /// <inheritdoc />
  /// <remarks>The LaunchServices <c>-psn_…</c> process-serial-number argument is filtered out — it is never activation content.</remarks>
  public override IReadOnlyList<IActivationIntent> Convert(IReadOnlyList<string> args)
  {
    // LaunchServices injects a -psn_XXXX process-serial-number argument —
    // noise, never activation content.
    var filtered = args.Where(a => !a.StartsWith("-psn_", StringComparison.Ordinal)).ToArray();
    return base.Convert(filtered);
  }

  /// <summary>Releases the OS activation bridge.</summary>
  public override async ValueTask DisposeAsync()
  {
    _bridge.Dispose();
    await base.DisposeAsync();
  }

  // TODO: Foreground activation via NSWorkspace / NSRunningApplication
  //       (activateIgnoringOtherApps) when a Cocoa interop path is added.
}
