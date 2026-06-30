using Everlong.DI;
using Everlong.Nester.Intent;
using Everlong.Nester.Layer;
using Everlong.Nester.Messaging;
using Everlong.Nester.Shell;
using NesterApp.Backdrop;

namespace NesterApp.Services;

/// <summary>Which backdrop a shell runs.</summary>
public enum BackdropProfile
{
  /// <summary>The main window: the full field plus the tuner panel.</summary>
  Main,

  /// <summary>The login window: a quieter field, no panel.</summary>
  Login,
}

/// <summary>The backdrop domain's intent: flip the tuner panel's visibility.</summary>
public sealed record ToggleBackdropPanelIntent : IIntent;

/// <summary>Broadcast after the tuner panel's visibility changed.</summary>
/// <param name="IsVisible">Whether the panel is now shown.</param>
public sealed record BackdropPanelVisibilityChangedMessage(bool IsVisible) : IMessage;

/// <summary>
///   Creates the platform surfaces the backdrop domain rents.
/// </summary>
public interface IBackdropSurfaceFactory
{
  /// <summary>Creates the animated field bound to <paramref name="lab" />.</summary>
  object CreateField(BackdropLab lab);

  /// <summary>Creates the tuner panel bound to <paramref name="lab" />.</summary>
  object CreatePanel(BackdropLab lab);
}

/// <summary>
///   The backdrop domain's tenant: it rents the backdrop band for the
///   animated field and the dev-tools band for the tuner panel, and answers
///   its own intent as those leases' intent handler.  The panel's visibility
///   lives on the lease — the shell never holds it.
/// </summary>
[Singleton]
public sealed class BackdropService : ILayerTenant, IIntentHandler
{
  private readonly ILayerBroker _broker;
  private readonly IBackdropSurfaceFactory _surfaces;
  private readonly IMessageHub _hub;
  private readonly IShellLifetime _lifetime;
  private ILayerLease? _fieldLease;
  private ILayerLease? _panelLease;

  /// <summary>Creates the service.</summary>
  public BackdropService(ILayerBroker broker,
                         IBackdropSurfaceFactory surfaces,
                         IMessageHub hub,
                         IShellLifetime lifetime)
  {
    _broker = broker;
    _surfaces = surfaces;
    _hub = hub;
    _lifetime = lifetime;
  }

  /// <summary>The tuner the dev panel edits.</summary>
  public BackdropLab Lab { get; } = new();

  /// <summary>Whether the tuner panel is currently shown.</summary>
  public bool IsPanelVisible => _panelLease is { IsVisible: true };

  /// <summary>Rents the shell's backdrop bands for <paramref name="profile" />.</summary>
  public void Mount(BackdropProfile profile)
  {
    if (profile is BackdropProfile.Login)
      Lab.UseLoginPreset();

    _fieldLease = Rent(_surfaces.CreateField(Lab), KnownLayers.Backdrop, LayerPolicy.Floor);

    if (profile is BackdropProfile.Main)
      _panelLease = Rent(_surfaces.CreatePanel(Lab), KnownLayers.DevTool, LayerPolicy.AboveHighest);

    _ = PublishInitialVisibilityAsync();
  }

  /// <inheritdoc />
  public ValueTask HandleAsync(IntentContext context, IntentDelegate next)
  {
    if (context.Intent is not ToggleBackdropPanelIntent || _panelLease is not { IsActive: true } lease)
      return next(context);

    lease.IsVisible = !lease.IsVisible;
    _hub.Publish(new BackdropPanelVisibilityChangedMessage(lease.IsVisible));
    context.Handle(this);
    return ValueTask.CompletedTask;
  }

  /// <inheritdoc />
  public ValueTask OnEvictedAsync(ILayerLease lease)
  {
    if (ReferenceEquals(lease, _fieldLease))
      _fieldLease = null;
    else if (ReferenceEquals(lease, _panelLease))
      _panelLease = null;

    return ValueTask.CompletedTask;
  }

  /// <summary>Rents one band and presents this service as the lease's intent handler.</summary>
  private ILayerLease Rent(object content, LayerBand band, LayerPolicy policy)
  {
    ILayerLease lease = _broker.Acquire(this, content, band, policy);
    lease.IntentHandler = this;
    return lease;
  }

  /// <summary>
  ///   Announces the initial visibility once the startup flow settles.  The
  ///   first-screen subscribers register during the first navigation, which
  ///   completes before that signal — publishing at mount time would reach
  ///   nobody and leave their projection drifting from the truth.
  /// </summary>
  private async Task PublishInitialVisibilityAsync()
  {
    try
    {
      await _lifetime.Startup;
    }
    catch
    {
      return; // startup failed, or the shell was disposed first — nothing to announce
    }

    _hub.Publish(new BackdropPanelVisibilityChangedMessage(IsPanelVisible));
  }
}
