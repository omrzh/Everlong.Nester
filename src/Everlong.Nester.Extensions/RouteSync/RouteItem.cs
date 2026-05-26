using Everlong.Nester.Routing;

namespace Everlong.Nester.RouteSync;

/// <summary>
///   A concrete, immutable navigation item.  A <see langword="null" />
///   <see cref="Destination" /> makes the item a pure group.
/// </summary>
public class RouteItem : IRouteItem
{
  /// <summary>Gets the display title.</summary>
  public required string Title { get; init; }

  /// <summary>Gets the optional icon shown alongside <see cref="Title" />.</summary>
  public object? Icon { get; init; }

  /// <summary>Gets the child items; empty for a leaf.</summary>
  public IReadOnlyList<IRouteItem> Children { get; init; } = [];

  /// <summary>Gets the navigation request issued on activation; <see langword="null" /> for a pure group.</summary>
  public ILocator? Destination { get; init; }

  /// <summary>Gets whether the item can be collapsed. Defaults to <see langword="true" />.</summary>
  public bool IsCollapsible { get; init; } = true;

  /// <summary>
  ///   Gets the default match target — the destination's content target
  ///   type; <see langword="null" /> without a destination.
  /// </summary>
  public Type? TargetType => RouteItemMatch.TargetType(this);

  /// <inheritdoc cref="RouteItemMatch.IsHighlighted(IRouteItem, ILocation?)" />
  public bool IsHighlighted(ILocation? current) => RouteItemMatch.IsHighlighted(this, current);
}
