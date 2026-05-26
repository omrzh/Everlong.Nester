using Everlong.Nester.Routing;

namespace Everlong.Nester.RouteSync;

/// <summary>
///   Highlight matching for <see cref="IRouteItem" />.
/// </summary>
public static class RouteItemMatch
{
  /// <summary>
  ///   Gets the item's default match target — the destination's content
  ///   target type; <see langword="null" /> without a destination.
  /// </summary>
  /// <param name="item">The navigation item.</param>
  public static Type? TargetType(IRouteItem item)
    => item.Destination?.Path is { Count: > 0 } path ? path[^1].Type : null;

  /// <summary>
  ///   Determines whether <paramref name="item" /> is highlighted for
  ///   <paramref name="current" />.
  /// </summary>
  /// <param name="item">The navigation item.</param>
  /// <param name="current">The presented content target, or <see langword="null" /> when nothing is presented.</param>
  /// <returns><see langword="true" /> when the item is highlighted; otherwise <see langword="false" />.</returns>
  public static bool IsHighlighted(IRouteItem item, ILocation? current)
  {
    if (current is null || item.Destination is not { } destination || TargetType(item) is not { } target)
    {
      return false;
    }

    for (ILocation? node = current; node is not null; node = node.Parent)
    {
      bool? vote = node.Instance is IRouteHighlight voter ? voter.Represents(destination) : null;
      if (vote ?? (node.Type == target))
      {
        return true;
      }
    }

    return false;
  }

  /// <summary>
  ///   Determines whether any descendant of <paramref name="item" /> is
  ///   highlighted for <paramref name="current" />.
  /// </summary>
  /// <param name="item">The navigation item.</param>
  /// <param name="current">The presented content target, or <see langword="null" /> when nothing is presented.</param>
  /// <returns><see langword="true" /> when a descendant is highlighted; otherwise <see langword="false" />.</returns>
  public static bool HasHighlightedDescendant(IRouteItem item, ILocation? current)
  {
    foreach (IRouteItem child in item.Children)
    {
      if (IsHighlighted(child, current) || HasHighlightedDescendant(child, current))
      {
        return true;
      }
    }

    return false;
  }
}
