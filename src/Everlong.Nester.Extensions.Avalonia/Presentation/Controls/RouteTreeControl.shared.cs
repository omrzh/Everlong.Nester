// NOTE: Single-source file — the Extensions.Wpf project compiles this exact
// file via <Compile Include> in Everlong.Nester.Extensions.Wpf.csproj.
// Edit it here only; never create a WPF-side copy (the two builds would drift).
using System.ComponentModel;
using Everlong.Nester.RouteSync;
using Everlong.Nester.Routing;

namespace Everlong.Nester.Presentation;

/// <summary>
///   A navigation tree container that renders a hierarchical list of <see cref="IRouteItem" /> items
///   using recursive <see cref="RouteTreeItem" /> controls. Each item manages its own active state
///   and expand/collapse state independently.
/// </summary>
/// <remarks>
///   <para>
///     RouteTreeControl exists as a root container for navigation trees (single or multi-root).
///   </para>
///   <para>
///     The control also folds the whole item tree once per landing and publishes the
///     result to its items, so a group's descendant highlight is not recomputed by
///     every ancestor.
///   </para>
///   <para>
///     When a <see cref="RouteTreeItem" /> header is clicked, the bubbled <see cref="TreeItem.ItemClickedEvent" />
///     reaches <see cref="RouteTreeControl" />, which routes the item's location on the
///     router of its hosting navigation surface (the nearest <see cref="IRoutingView" />).
///   </para>
///   <para>
///     <see cref="TreeControl.ItemCommand" /> is also available on <see cref="RouteTreeControl" />.
///     It executes with the clicked item's <c>DataContext</c> as its parameter, AFTER navigation
///     has been triggered.
///   </para>
/// </remarks>
public sealed partial class RouteTreeControl
{
  private IRouter? _surfaceRouter;
  private IRouterStack? _surfaceStack;
  private ILocation? _highlightSite;

  /// <summary>Identity-keyed: a custom item's value equality must not merge two distinct nodes.</summary>
  private readonly Dictionary<IRouteItem, HighlightState> _highlights =
    new(ReferenceEqualityComparer.Instance);

  /// <summary>A node's folded highlight — its own state and whether any descendant is highlighted.</summary>
  internal readonly record struct HighlightState(bool Self, bool HasDescendant);

  /// <summary>
  ///   Gets the highlight folded for <paramref name="item" /> at the current
  ///   site.  Returns <see langword="false" /> when the snapshot is stale or
  ///   the item is not part of this tree — the caller then computes directly.
  /// </summary>
  internal bool TryGetHighlight(IRouteItem item, ILocation? site, out bool self, out bool hasDescendant)
  {
    if (ReferenceEquals(_highlightSite, site) && _highlights.TryGetValue(item, out HighlightState state))
    {
      self = state.Self;
      hasDescendant = state.HasDescendant;
      return true;
    }

    self = false;
    hasDescendant = false;
    return false;
  }

  /// <summary>
  ///   Binds to the routing surface — resolves the surface's router by walking
  ///   ancestors to the <see cref="IRoutingView" /> host and folds the item
  ///   tree.  Called by the platform attach hooks; a no-op when already bound.
  /// </summary>
  private void BindSurface()
  {
    if (_surfaceStack is not null)
    {
      return;
    }

    _surfaceRouter = RoutingSurface.FindRouter(this);
    if (_surfaceRouter is null)
    {
      return;
    }

    _surfaceStack = _surfaceRouter.Stack;
    _surfaceStack.PropertyChanged += OnStackPropertyChanged;
    RecomputeHighlights();
  }

  /// <summary>Unbinds from the surface — called by the platform detach hooks; a no-op when not bound.</summary>
  private void UnbindSurface()
  {
    _surfaceStack?.PropertyChanged -= OnStackPropertyChanged;

    _surfaceStack = null;
    _surfaceRouter = null;
    _highlightSite = null;
    _highlights.Clear();
  }

  private void OnStackPropertyChanged(object? sender, PropertyChangedEventArgs e)
  {
    if (e.PropertyName == nameof(IRouterStack.Location))
    {
      RecomputeHighlights();
    }
  }

  /// <summary>
  ///   Folds the whole item tree once for the presented site — every node's own
  ///   highlight is evaluated once, and the descendant flag falls out of the
  ///   same post-order pass.
  /// </summary>
  private void RecomputeHighlights()
  {
    ILocation? site = _surfaceRouter?.Stack.Location;
    _highlightSite = site;
    _highlights.Clear();
    if (site is null || ItemsSource is null)
    {
      return;
    }

    foreach (object? root in ItemsSource)
    {
      if (root is IRouteItem item)
      {
        Fold(item, site);
      }
    }
  }

  private HighlightState Fold(IRouteItem item, ILocation site)
  {
    bool self = RouteItemMatch.IsHighlighted(item, site);
    bool hasDescendant = false;
    foreach (IRouteItem child in item.Children)
    {
      HighlightState childState = Fold(child, site);
      hasDescendant |= childState.Self || childState.HasDescendant;
    }

    var state = new HighlightState(self, hasDescendant);
    _highlights[item] = state;
    return state;
  }

  /// <summary>
  ///   Called when a child <see cref="RouteTreeItem" /> header is clicked (via the bubbled
  ///   <see cref="TreeItem.ItemClickedEvent" />). Drives navigation for leaves and navigable groups.
  /// </summary>
  private void OnChildItemClicked(TreeItem item)
  {
    if (item is not RouteTreeItem navItem)
      return;
    ILocator? route = navItem.GetNavigationRoute();
    if (route is not null)
    {
      // The nav chrome addresses its own surface's router directly — a
      // navigation request is a directive, not a consultable intent.
      IRouter? router = RoutingSurface.FindRouter(this);
      if (router is null)
      {
        return;
      }

      _ = router.RouteAsync(route);
    }
  }
}
