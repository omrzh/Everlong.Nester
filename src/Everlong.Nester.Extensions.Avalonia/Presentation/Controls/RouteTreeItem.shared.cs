// NOTE: Single-source file — the Extensions.Wpf project compiles this exact
// file via <Compile Include> in Everlong.Nester.Extensions.Wpf.csproj.
// Edit it here only; never create a WPF-side copy (the two builds would drift).
using System.ComponentModel;
using Everlong.Nester.RouteSync;
using Everlong.Nester.Routing;

namespace Everlong.Nester.Presentation;

/// <summary>
///   Shared logic for <see cref="RouteTreeItem" /> on Avalonia and WPF.
///   Each item binds to its routing surface (the nearest
///   <see cref="IRoutingView" /> host) and follows the surface router's
///   <see cref="IRouterStack" /> site changes to manage its own
///   <see cref="IsRouteHighlighted" /> / <see cref="HasHighlightedDescendant" /> /
///   <see cref="TreeItem.IsExpanded" /> state.
/// </summary>
public partial class RouteTreeItem
{
  /// <summary>The slot width below which the row the item composes — indicator, icon, title, chevron — no longer fits.</summary>
  private const double RailWidth = 150;

  private IRouter? _surfaceRouter;
  private IRouterStack? _surfaceStack;

  /// <summary>The surface's currently presented site — the value this item matches against; <see langword="null" /> before it binds to a surface.</summary>
  private ILocation? CurrentSite => _surfaceRouter?.Stack.Location;

  /// <summary>
  ///   Tracks the last DataContext that was resolved as a group, initialising
  ///   <see cref="TreeItem.IsExpanded" /> once when a new group DataContext is set.
  /// </summary>
  private IRouteItem? _lastGroupContext;

  /// <summary>The enclosing tree coordinator, or <see langword="null" /> when the item is standalone.</summary>
  private RouteTreeControl? _treeControl;

  /// <summary>
  ///   Binds to the routing surface — resolves the surface's router by
  ///   walking ancestors to the <see cref="IRoutingView" /> host, follows the
  ///   stack's site changes and recomputes the item state.  Called by the
  ///   platform attach hooks; a no-op when already bound.
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
    Refresh();
  }

  /// <summary>Unbinds from the surface — called by the platform detach hooks; a no-op when not bound.</summary>
  private void UnbindSurface()
  {
    _surfaceStack?.PropertyChanged -= OnStackPropertyChanged;

    _surfaceStack = null;
    _surfaceRouter = null;
    _treeControl = null;
  }

  private void OnStackPropertyChanged(object? sender, PropertyChangedEventArgs e)
  {
    // The stack raises its whole surface on every landing — the site is the
    // only member the item state follows.
    if (e.PropertyName == nameof(IRouterStack.Location))
    {
      Refresh();
    }
  }

  /// <summary>
  ///   Recalculates all state from the current <c>DataContext</c> and the
  ///   surface's presented site.  Called when the site changes, when
  ///   DataContext changes, and on surface bind.
  /// </summary>
  private void Refresh()
  {
    UpdateState(CurrentSite);
  }

  /// <summary>Recalculates all state from the current <c>DataContext</c> and the supplied site.</summary>
  private void UpdateState(ILocation? current)
  {
    if (DataContext is not IRouteItem item)
    {
      ResetState();
      return;
    }

    Title = item.Title;
    Icon = item.Icon;
    HasIcon = Icon is not null;

    bool hasChildren = item.Children.Count > 0;
    bool selfHighlighted;
    bool hasHighlightedDescendant;

    // Under a RouteTreeControl the whole tree's highlight is folded once per
    // landing; a standalone item folds its own subtree.
    RouteTreeControl? control = _treeControl ??= RoutingSurface.FindTreeControl(this);
    if (control is not null && control.TryGetHighlight(item, current, out bool cachedSelf, out bool cachedDescendant))
    {
      selfHighlighted = cachedSelf;
      hasHighlightedDescendant = cachedDescendant;
    }
    else
    {
      selfHighlighted = current is not null && RouteItemMatch.IsHighlighted(item, current);
      hasHighlightedDescendant = current is not null
                                 && hasChildren
                                 && RouteItemMatch.HasHighlightedDescendant(item, current);
    }

    IsRouteHighlighted = selfHighlighted;
    HasHighlightedDescendant = hasHighlightedDescendant;
    IsCollapsible = hasChildren && item.IsCollapsible;
    ItemsSource = item.Children;

    if (hasChildren)
    {
      if (selfHighlighted || hasHighlightedDescendant)
      {
        IsExpanded = true;
      }
      else if (!ReferenceEquals(item, _lastGroupContext))
      {
        // New group DataContext: default to expanded.
        IsExpanded = true;
      }
    }

    _lastGroupContext = hasChildren ? item : null;
  }

  private void ResetState()
  {
    _lastGroupContext = null;
    Title = string.Empty;
    Icon = null;
    HasIcon = false;
    IsRouteHighlighted = false;
    HasHighlightedDescendant = false;
    IsCollapsible = false;
    ItemsSource = null;
  }

  /// <summary>Recomputes the effective highlight — the app override wins when set.</summary>
  private void RefreshEffective()
  {
    IsActive = IsHighlighted ?? IsRouteHighlighted;
  }

  /// <summary>Resolves the navigation request for this item, or <see langword="null" /> for a pure group.</summary>
  protected internal ILocator? GetNavigationRoute()
    => DataContext is IRouteItem item ? item.Destination : null;

  /// <inheritdoc />
  /// <remarks>
  ///   Handles expand/collapse only. Navigation is driven by the parent <see cref="RouteTreeControl" />
  ///   via the bubbled <see cref="TreeItem.ItemClickedEvent" />.
  ///   <list type="bullet">
  ///     <item>Navigable groups: always expand (active state from navigation triggers <see cref="Refresh" />).</item>
  ///     <item>Pure groups: toggles expand/collapse respecting <see cref="IsCollapsible" />.</item>
  ///     <item>Leaves: no expand/collapse action.</item>
  ///   </list>
  /// </remarks>
  protected override void OnHeaderClicked()
  {
    if (DataContext is not IRouteItem item || item.Children.Count == 0)
    {
      return;
    }

    if (item.Destination is not null)
    {
      // Navigable group: always ensure expanded.  Navigation is handled by
      // RouteTreeControl; Refresh() sets IsExpanded when the route becomes active.
      IsExpanded = true;
    }
    else
    {
      // Pure group: toggle with IsCollapsible guard.
      if (!IsExpanded)
      {
        IsExpanded = true;
      }
      else if (IsCollapsible)
      {
        IsExpanded = false;
      }
    }
  }

  /// <inheritdoc />
  /// <remarks>
  ///   Respects <see cref="IsCollapsible" />: when <see langword="false" />, collapse is prevented.
  /// </remarks>
  protected override void OnExpandClicked()
  {
    if (!HasItems)
    {
      return;
    }

    if (IsExpanded && !IsCollapsible)
    {
      return;
    }

    IsExpanded = !IsExpanded;
  }
}
