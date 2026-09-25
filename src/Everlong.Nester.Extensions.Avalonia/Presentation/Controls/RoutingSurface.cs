// NOTE: Single-source file — the Extensions.Wpf project compiles this exact
// file via <Compile Include> in Everlong.Nester.Extensions.Wpf.csproj.
// Edit it here only; never create a WPF-side copy (the two builds would drift).

using Everlong.Nester.Helpers;
using Everlong.Nester.Routing;

namespace Everlong.Nester.Presentation;

/// <summary>
///   Resolves the routing surface a control lives on — the nearest
///   <see cref="IRoutingView"/> ancestor's router, the surface the control
///   navigates and highlights against.
/// </summary>
internal static class RoutingSurface
{
  /// <summary>
  ///   Finds the nearest <see cref="IRoutingView"/> ancestor's router —
  ///   <see langword="null"/> when the control is not under a routing
  ///   surface (designer preview, bare test construction, popup roots).
  /// </summary>
  internal static IRouter? FindRouter(PControl control)
    => control.FindVisualAncestor<IRoutingView>()?.Router;

  /// <summary>
  ///   Finds the nearest <see cref="RouteTreeControl" /> ancestor —
  ///   <see langword="null" /> when the control is not inside a nav tree.
  /// </summary>
  internal static RouteTreeControl? FindTreeControl(PControl control)
    => control.FindVisualAncestor<RouteTreeControl>();
}
