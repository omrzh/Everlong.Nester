using Everlong.Nester.Presentation;
using Everlong.Nester.Layer;

namespace Everlong.Nester.Tests.Shell;

/// <summary>
///   Content-slot test helpers (v3): navigation structures live inside lease
///   content slots — the router's overlays (dialogs/derived routers) mount their
///   hosts directly under the stage panel.
/// </summary>
internal static class TreeTestHelpers
{
  /// <summary>
  ///   The layers of derived routers — a derived router rents the dock plane;
  ///   an overlay rents the overlay plane.  The ground router's own layer is
  ///   excluded.
  /// </summary>
  public static IEnumerable<ContentLayer> DerivedLayers(this StagePanel panel)
    => panel.Children.OfType<ContentLayer>()
            .Where(layer => layer.ZIndex >= LayerPlanes.Range(LayerPlane.Dock).Floor && layer.Content is RoutingView);

  /// <summary>The hosts of derived layers currently mounted (the router's close-stack ledger — dialogs and derived overlays; the ground host excluded).</summary>
  public static IEnumerable<RoutingView> DerivedHosts(this StagePanel panel)
    => panel.DerivedLayers().Select(layer => (RoutingView)layer.Content!);
}
