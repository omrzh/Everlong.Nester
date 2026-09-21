using Everlong.Nester.Controls;
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
  ///   The layers of derived routers — a derived router (a full navigator) shares the
  ///   ground router's navigation band, one slot above its floor; a one-shot
  ///   dialog rents the dialog band.  The ground router's own layer is excluded.
  /// </summary>
  public static IEnumerable<ContentLayer> DerivedLayers(this StagePanel panel)
    => panel.Children.OfType<ContentLayer>()
            .Where(layer => layer.ZIndex > KnownLayers.Navigation.Floor && layer.Content is RoutingView);

  /// <summary>The hosts of derived layers currently mounted (the router's close-stack ledger — dialogs and one-shot routers; the ground host excluded).</summary>
  public static IEnumerable<RoutingView> DerivedHosts(this StagePanel panel)
    => panel.DerivedLayers().Select(layer => (RoutingView)layer.Content!);
}
