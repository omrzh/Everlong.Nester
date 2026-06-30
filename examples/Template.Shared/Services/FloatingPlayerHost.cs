using Everlong.DI;
using Everlong.Nester.Layer;
using NesterApp.Pages.Labs;

namespace NesterApp.Services;

/// <summary>
///   The floating player's layer tenant — rents the floating band's ceiling
///   (immediately below the dialog band) and delivers a
///   <see cref="FloatingPlayerViewModel" /> into the slot.
/// </summary>
[Scoped<FloatingPlayerHost>]
public sealed class FloatingPlayerHost : ILayerTenant
{
  private readonly ILayerBroker _broker;
  private ILayerLease? _lease;
  private FloatingPlayerViewModel? _vm;
  private bool _evicted;

  public FloatingPlayerHost(ILayerBroker broker) => _broker = broker;

  /// <summary>Shows the floating player with the given title and start position, replacing any existing one.</summary>
  public void Show(string title, double position)
  {
    if (_evicted)
      return;

    _vm?.Stop();

    FloatingPlayerViewModel floatingPlayerViewModel = new()
    {
      VideoTitle = title,
      PlaybackPosition = position,
      IsPlaying = true
    };
    var vm = floatingPlayerViewModel;
    vm.CloseRequested = Close;
    vm.StartPlaybackLoop();

    _vm = vm;
    // The floating band's ceiling sits immediately below the dialog band.
    _lease ??= _broker.Acquire(this, vm, KnownLayers.Floating, LayerPolicy.Ceiling);
    _lease.Content = vm;
  }

  /// <summary>Stops playback and returns the layer lease.</summary>
  public void Close()
  {
    _vm?.Stop();
    _vm = null;
    _lease?.Release();
    _lease = null;
  }

  /// <summary>The landlord reclaimed the lease — stop playback and forget it.</summary>
  public ValueTask OnEvictedAsync(ILayerLease lease)
  {
    _evicted = true;
    _vm?.Stop();
    _vm = null;
    _lease = null;
    return ValueTask.CompletedTask;
  }
}
