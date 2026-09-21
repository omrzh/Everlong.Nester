// NOTE: Single-source file — the WPF project compiles this exact file via
// <Compile Include> in Everlong.Nester.Wpf.csproj.  Edit it here only;
// never create a WPF-side copy (the two builds would drift).

using Everlong.Nester.Layer;

namespace Everlong.Nester.Presentation;

/// <summary>
///   The flying layer's tenant — rents the top band
///   (<see cref="KnownLayers.Flying" />) from the shell and delivers its
///   plane figure.
/// </summary>
internal sealed class ShellFlyingLayer : ILayerTenant, IFlyingLayer
{
  private readonly ILayerLease _lease;

  public ShellFlyingLayer(ILayerBroker broker)
  {
    // The void-penthouse contract: acquiring before the visual stack is
    // connected is a pure ledger entry; the shell's stage connects it
    // later.  The slot's z (KnownLayers.Flying) keeps the canvas above
    // every floor (navigation, dialog, notice).
    _lease = broker.Acquire(this, Canvas, KnownLayers.Flying, LayerPolicy.Floor);
  }

  /// <inheritdoc />
  public FlyingCanvas Canvas { get; } = new();

  ValueTask ILayerTenant.OnEvictedAsync(ILayerLease lease)
  {
    // The shell is tearing down — the scope dies with it; drop the ghosts
    // and whatever the surface was still carrying.
    if (_lease == lease)
    {
      Canvas.Clear();
    }

    return ValueTask.CompletedTask;
  }
}
