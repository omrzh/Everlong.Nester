using Avalonia.Controls.ApplicationLifetimes;
using Everlong.Nester.Intent;
using Everlong.Nester.Messaging;

namespace Everlong.Nester.Activation;

/// <summary>
///   Subscribes the Avalonia <see cref="IActivatableLifetime" /> and translates
///   OS activation events into activation intents, forwarded to the owning
///   agent (which dispatches to its host channel or parks).
/// </summary>
/// <remarks>
///   No-op on Windows/Linux (no IActivatableLifetime there). Subscribing is
///   idempotent. Intents arriving while the agent has no host are parked by
///   the agent and drained by a later flush. Lifecycle facts publish through
///   the optional <see cref="IMessageHub" />.
/// </remarks>
internal sealed class ActivatableLifetimeBridge : IDisposable
{
  private readonly Func<IActivationIntent, ValueTask<IntentResult>> _forward;
  private readonly IMessageHub? _hub;
  private IActivatableLifetime? _lifetime;

  public ActivatableLifetimeBridge(Func<IActivationIntent, ValueTask<IntentResult>> forward, IMessageHub? hub = null)
  {
    _forward = forward;
    _hub = hub;
  }

  public void Subscribe()
  {
    if (_lifetime is not null)
      return; // already subscribed

    if (OperatingSystem.IsWindows()
        || OperatingSystem.IsLinux()
        || Avalonia.Application.Current?.TryGetFeature(typeof(IActivatableLifetime)) is not IActivatableLifetime lifetime)
      return;

    _lifetime = lifetime;
    _lifetime.Activated += OnActivated;
    _lifetime.Deactivated += OnDeactivated;
  }

  public void Unsubscribe()
  {
    if (_lifetime is null)
      return;

    _lifetime.Activated -= OnActivated;
    _lifetime.Deactivated -= OnDeactivated;

    _lifetime = null;
  }

  public void Dispose() => Unsubscribe();

  private void OnActivated(object? sender, ActivatedEventArgs args)
  {
    switch (args)
    {
      case ProtocolActivatedEventArgs protocolArgs:
        Dispatch(new UriActivationIntent(protocolArgs.Uri));
        break;

      case FileActivatedEventArgs fileArgs:
        // OS file events carry platform storage items — the platform intent;
        // command-line files stay the core string intent (FileActivationIntent).
        Dispatch(new StorageItemActivationIntent(fileArgs.Files));
        break;

      default:
        Publish(AppLifecycleMessageFactory.FromActivation(args));
        break;
    }
  }

  private void OnDeactivated(object? sender, ActivatedEventArgs args)
    => Publish(AppLifecycleMessageFactory.FromDeactivation());

  private void Publish(IMessage? message)
  {
    if (message is not null)
      _hub?.Publish(message);
  }

  private async void Dispatch(IActivationIntent intent)
  {
    try
    {
      await _forward(intent);
    }
    catch
    {
      // fire-and-forget: a failing forward must not crash the event handler
    }
  }
}
