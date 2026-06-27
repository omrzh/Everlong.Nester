using Everlong.Nester.Presentation;
// NOTE: Single-source file — the Extensions.Wpf project compiles this exact
// file via <Compile Include> in Everlong.Nester.Extensions.Wpf.csproj.
// Edit it here only; never create a WPF-side copy (the two builds would drift).

using Everlong.Nester.Routing;
#if AVALONIA
using Avalonia.VisualTree;
#endif

namespace Everlong.Nester.Controls;

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
  {
#if AVALONIA
    Avalonia.Visual? node = control;
    while (node is not null)
    {
      if (node is IRoutingView view)
        return view.Router;
      node = node.GetVisualParent();
    }

    return null;
#else
    System.Windows.DependencyObject? node = control;
    while (node is not null)
    {
      if (node is IRoutingView view)
        return view.Router;
      node = System.Windows.Media.VisualTreeHelper.GetParent(node);
    }

    return null;
#endif
  }

  /// <summary>
  ///   Finds the nearest <see cref="RouteTreeControl" /> ancestor —
  ///   <see langword="null" /> when the control is not inside a nav tree.
  /// </summary>
  internal static RouteTreeControl? FindTreeControl(PControl control)
  {
#if AVALONIA
    Avalonia.Visual? node = control;
    while (node is not null)
    {
      if (node is RouteTreeControl tree)
        return tree;
      node = node.GetVisualParent();
    }

    return null;
#else
    System.Windows.DependencyObject? node = control;
    while (node is not null)
    {
      if (node is RouteTreeControl tree)
        return tree;
      node = System.Windows.Media.VisualTreeHelper.GetParent(node);
    }

    return null;
#endif
  }
}
