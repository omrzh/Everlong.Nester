using System.Windows;

namespace Everlong.Nester.Presentation;

/// <summary>
///   WPF view-resolution contract: a locator claims the data it maps and
///   builds the view for it.
/// </summary>
/// <remarks>
///   <see cref="Match" /> is a claim, not a precondition of
///   <see cref="Build" />: both answer independently for the same data.
/// </remarks>
public interface IViewLocator
{
  /// <summary>Determines whether this locator claims <paramref name="data" />.</summary>
  /// <param name="data">A data instance, or <c>null</c>.</param>
  /// <returns><c>true</c> when this locator builds a view for <paramref name="data" />.</returns>
  bool Match(object? data);

  /// <summary>
  ///   Builds and returns a view instance for the specified data.
  /// </summary>
  /// <param name="data">A view-model instance.</param>
  /// <returns>A new view instance, or <c>null</c> if no view is registered for the given data.</returns>
  FrameworkElement? Build(object? data);
}
