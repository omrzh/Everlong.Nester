using System.Windows;
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
public sealed class RoutingChannel
{
  /// <summary>
  ///   Identifies the <see cref="LocationProperty"/> attached property.
  /// </summary>
  /// <remarks>
  ///   Holds the host's currently presented location.  Set on the host element
  ///   only; not inherited.
  /// </remarks>
  public static readonly DependencyProperty LocationProperty =
    DependencyProperty.RegisterAttached(
      "Location", typeof(ILocation), typeof(RoutingChannel),
      new FrameworkPropertyMetadata(null));

  /// <summary>
  ///   Identifies the <see cref="RouterProperty"/> attached property.
  /// </summary>
  /// <remarks>
  ///   Holds the <see cref="IRouter"/> owning the host.  Set on the host element
  ///   only; not inherited.
  /// </remarks>
  public static readonly DependencyProperty RouterProperty =
    DependencyProperty.RegisterAttached(
      "Router", typeof(IRouter), typeof(RoutingChannel),
      new FrameworkPropertyMetadata(null));

  /// <summary>
  ///   Gets the value of the <see cref="LocationProperty"/> attached property from a specified <see cref="DependencyObject"/>.
  /// </summary>
  /// <param name="element">The element from which to read the property value.</param>
  /// <returns>The currently presented <see cref="ILocation"/>, or <c>null</c> if not set.</returns>
  public static ILocation? GetLocation(DependencyObject element)
    => (ILocation?)element.GetValue(LocationProperty);

  /// <summary>
  ///   Gets the value of the <see cref="RouterProperty"/> attached property from a specified <see cref="DependencyObject"/>.
  /// </summary>
  /// <param name="element">The element from which to read the property value.</param>
  /// <returns>The owning <see cref="IRouter"/> instance, or <c>null</c> if not set.</returns>
  public static IRouter? GetRouter(DependencyObject element)
    => (IRouter?)element.GetValue(RouterProperty);
}
