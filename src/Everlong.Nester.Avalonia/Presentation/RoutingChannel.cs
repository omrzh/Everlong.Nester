using Avalonia;
using Everlong.Nester.Routing;

namespace Everlong.Nester.Presentation;

/// <summary>
///   Provides attached properties that record a routing surface's (the
///   <see cref="IRoutingView"/> host's) router and presented location on the
///   host element itself.
/// </summary>
/// <remarks>
///   The values are set on the host element and are not inherited — a navigation commit never
///   propagates through the element tree.
/// </remarks>
public abstract class RoutingChannel
{
  /// <summary>
  ///   Identifies the <see cref="LocationProperty"/> attached property.
  /// </summary>
  /// <remarks>
  ///   Holds the host's currently presented location.  Set on the host element
  ///   only; not inherited.
  /// </remarks>
  public static readonly AttachedProperty<ILocation?> LocationProperty =
    AvaloniaProperty.RegisterAttached<RoutingChannel, PControl, ILocation?>(
      "Location");

  /// <summary>
  ///   Identifies the <see cref="RouterProperty"/> attached property.
  /// </summary>
  /// <remarks>
  ///   Holds the <see cref="IRouter"/> owning the host.  Set on the host element
  ///   only; not inherited.
  /// </remarks>
  public static readonly AttachedProperty<IRouter?> RouterProperty =
    AvaloniaProperty.RegisterAttached<RoutingChannel, PControl, IRouter?>(
      "Router");

  /// <summary>
  ///   Gets the value of the <see cref="LocationProperty"/> attached property from a specified <see cref="PControl"/>.
  /// </summary>
  /// <param name="element">The element from which to read the property value.</param>
  /// <returns>The currently presented <see cref="ILocation"/>, or <c>null</c> if not set.</returns>
  public static ILocation? GetLocation(PControl element)
    => element.GetValue(LocationProperty);

  /// <summary>
  ///   Gets the value of the <see cref="RouterProperty"/> attached property from a specified <see cref="PControl"/>.
  /// </summary>
  /// <param name="element">The element from which to read the property value.</param>
  /// <returns>The owning <see cref="IRouter"/> instance, or <c>null</c> if not set.</returns>
  public static IRouter? GetRouter(PControl element)
    => element.GetValue(RouterProperty);
}
