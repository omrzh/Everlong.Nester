using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Everlong.Nester.Helpers;

/// <summary>
///   The ancestor resolution every element-tree walk shares — a caller names
///   the type it wants and the tree to look in, never the loop.
/// </summary>
internal static class AncestorExtensions
{
  /// <summary>
  ///   Resolves the nearest ancestor of type <typeparamref name="T" /> up
  ///   the logical tree, <paramref name="element" /> itself included;
  ///   <see langword="null" /> when the chain holds none.
  /// </summary>
  /// <remarks>
  ///   WPF's <c>ContentPresenter</c> never joins the logical tree, so
  ///   <c>DataTemplate</c> content and <c>ControlTemplate</c> children have
  ///   no logical parent.  The walk crosses such a boundary by the visual
  ///   parent, so a template-heavy chain still reaches the stage.
  /// </remarks>
  internal static T? FindAncestor<T>(this DependencyObject? element) where T : class
  {
    for (DependencyObject? node = element; node is not null; node = StepUp(node))
    {
      if (node is T ancestor)
        return ancestor;
    }

    return null;
  }

  /// <summary>
  ///   Steps one level up an element's ancestry: the logical parent, or the
  ///   visual parent where a template boundary leaves the logical tree.
  /// </summary>
  private static DependencyObject? StepUp(DependencyObject node)
  {
    DependencyObject? logical = node switch
    {
      FrameworkElement fe => fe.Parent,
      FrameworkContentElement fce => fce.Parent,
      _ => null,
    };

    if (logical is not null)
      return logical;

    return StepVisual(node);
  }

  /// <summary>Steps one level up the visual tree.</summary>
  private static DependencyObject? StepVisual(DependencyObject node)
    => node is Visual or Visual3D ? VisualTreeHelper.GetParent(node) : null;
}
