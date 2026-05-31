// NOTE: Single-source file — the WPF project compiles this exact file via
// <Compile Include> in Everlong.Nester.Wpf.csproj.  Edit it here only;
// never create a WPF-side copy (the two builds would drift).

using Everlong.Nester.Layer;

namespace Everlong.Nester.Presentation;

/// <summary>
///   The transition common area — a window-scoped service that rents the
///   flying layer (the top z band) from the shell and delivers its canvas
///   as the plane figure.  Any domain that animates above the floors
///   (navigation ghosts, dialog ghosts) injects this service and draws on
///   <see cref="Canvas" />: one canvas per window, no per-domain
///   duplication, no stage dependency.
/// </summary>
internal sealed class ShellFlyingLayer : ILayerTenant
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

  /// <summary>The flying canvas — the plane figure filling the flying layer's slot.</summary>
  public PlatformCanvas Canvas { get; } = new();

  ValueTask ILayerTenant.OnEvictedAsync(ILayerLease lease)
  {
    // The shell is tearing down — the scope dies with it; just drop the ghosts.
    if (_lease == lease)
    {
      Canvas.Children.Clear();
    }

    return ValueTask.CompletedTask;
  }
}
