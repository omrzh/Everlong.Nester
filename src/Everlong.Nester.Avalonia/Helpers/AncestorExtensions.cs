using Avalonia;

namespace Everlong.Nester.Helpers;

/// <summary>
///   The ancestor resolution every element-tree walk shares — a caller names
///   the type it wants and the tree to look in, never the loop.
/// </summary>
internal static class AncestorExtensions
{
  /// <summary>
  ///   Resolves the nearest ancestor of type <typeparamref name="T" /> by
  ///   the logical tree, <paramref name="element" /> itself included;
  ///   <see langword="null" /> when the chain holds none.
  /// </summary>
  internal static T? FindAncestor<T>(this StyledElement? element) where T : class
  {
    for (StyledElement? node = element; node is not null; node = node.Parent)
    {
      if (node is T ancestor)
        return ancestor;
    }

    return null;
  }
}
