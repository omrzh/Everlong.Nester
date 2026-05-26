using Everlong.Nester.Routing;

namespace Everlong.Nester.RouteSync;

/// <summary>
///   A navigation item — presentation, children, and an optional navigation request.
/// </summary>
public interface IRouteItem
{
  /// <summary>Gets the display title.</summary>
  string Title { get; }

  /// <summary>Gets the optional icon shown alongside <see cref="Title" />, or <see langword="null" /> when none.</summary>
  object? Icon { get; }

  /// <summary>Gets the child items; empty for a leaf.</summary>
  IReadOnlyList<IRouteItem> Children { get; }

  /// <summary>Gets the navigation request issued on activation; <see langword="null" /> for a pure group.</summary>
  ILocator? Destination { get; }

  /// <summary>Gets whether the item can be collapsed. Defaults to <see langword="true" />.</summary>
  bool IsCollapsible => true;
}
