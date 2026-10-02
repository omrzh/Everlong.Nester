// NOTE: Single-source file — the Extensions WPF project compiles this exact
// file via <Compile Include> in Everlong.Nester.Extensions.Wpf.csproj.
// Edit it here only; never create a WPF-side copy (the two builds would drift).

#if AVALONIA
using Avalonia;
using Avalonia.VisualTree;
#else
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
#endif

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
    => FindVisualAncestor<IRoutingView>(control)?.Router;

  /// <summary>
  ///   Finds the nearest <see cref="RouteTreeControl" /> ancestor —
  ///   <see langword="null" /> when the control is not inside a nav tree.
  /// </summary>
  internal static RouteTreeControl? FindTreeControl(PControl control)
    => FindVisualAncestor<RouteTreeControl>(control);

#if AVALONIA
  /// <summary>The nearest ancestor of type <typeparamref name="T" /> up the visual tree, the control included.</summary>
  private static T? FindVisualAncestor<T>(PControl control) where T : class
  {
    for (Visual? node = control; node is not null; node = node.GetVisualParent())
    {
      if (node is T ancestor)
        return ancestor;
    }

    return null;
  }
#else
  /// <summary>The nearest ancestor of type <typeparamref name="T" /> up the visual tree, the control included.</summary>
  private static T? FindVisualAncestor<T>(PControl control) where T : class
  {
    for (DependencyObject? node = control; node is not null; node = StepVisual(node))
    {
      if (node is T ancestor)
        return ancestor;
    }

    return null;
  }

  /// <summary>Steps one level up the visual tree.</summary>
  private static DependencyObject? StepVisual(DependencyObject node)
    => node is Visual or Visual3D ? VisualTreeHelper.GetParent(node) : null;
#endif
}
