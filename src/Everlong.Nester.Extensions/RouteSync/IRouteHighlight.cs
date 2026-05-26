using Everlong.Nester.Routing;

namespace Everlong.Nester.RouteSync;

/// <summary>
///   A presented participant that votes on navigation highlighting.
/// </summary>
public interface IRouteHighlight
{
  /// <summary>Votes on whether this participant is current for <paramref name="destination" />.</summary>
  /// <param name="destination">The item's navigation request.</param>
  /// <returns>
  ///   <see langword="true" /> to claim the destination; <see langword="false" />
  ///   to reject it, suppressing the default comparison for this participant;
  ///   <see langword="null" /> to defer to the default target-type comparison.
  /// </returns>
  bool? Represents(ILocator destination);
}
