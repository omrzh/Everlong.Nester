// NOTE: Single-source file — the WPF project compiles this exact file via
// <Compile Include> in Everlong.Nester.Wpf.csproj.  Edit it here only;
// never create a WPF-side copy (the two builds would drift).

#if AVALONIA
using Avalonia.LogicalTree;
#endif

namespace Everlong.Nester.Presentation;

internal sealed partial class RoutingView
{
  /// <summary>
  ///   Resolves the body a view mounts its inner content into — the view's
  ///   own <see cref="ILayoutControl" /> contract, or the one
  ///   <see cref="LayoutBody" /> its logical tree carries (the one named
  ///   <c>Body</c>, or the only one).  <see langword="null" /> when the view
  ///   declares neither.
  /// </summary>
  private static ILayoutBody<PControl>? ResolveLayoutBody(PControl view)
  {
    if (view is ILayoutControl layoutControl)
      return layoutControl.GetLayoutBody();

    TryFindLayoutBody(view, out LayoutBody? body);
    return body;
  }

  private static bool IsNamedBody(LayoutBody body)
    => string.Equals(body.Name, "Body", StringComparison.Ordinal);

#if AVALONIA
  private static bool TryFindLayoutBody(PControl view, out LayoutBody? body)
  {
    List<LayoutBody> all = [];

    if (view is LayoutBody rootBody)
    {
      if (IsNamedBody(rootBody))
      {
        body = rootBody;
        return true;
      }

      all.Add(rootBody);
    }

    foreach (ILogical logical in view.GetLogicalDescendants())
    {
      if (logical is not LayoutBody descendant)
        continue;
      if (IsNamedBody(descendant))
      {
        body = descendant;
        return true;
      }

      all.Add(descendant);
    }

    // One body and no name: the layout declares exactly one mount point.
    if (all.Count == 1)
    {
      body = all[0];
      return true;
    }

    body = null;
    return false;
  }
#else
  private static bool TryFindLayoutBody(PControl view, out LayoutBody? body)
  {
    List<LayoutBody> all = [];
    body = null;

    if (view is LayoutBody rootBody)
    {
      if (IsNamedBody(rootBody))
      { body = rootBody; return true; }
      all.Add(rootBody);
    }

    CollectLayoutBodies(view, all, ref body);
    if (body is not null)
      return true;

    // One body and no name: the layout declares exactly one mount point.
    if (all.Count == 1)
    { body = all[0]; return true; }

    body = null;
    return false;
  }

  private static bool CollectLayoutBodies(System.Windows.DependencyObject root,
                                          List<LayoutBody> all,
                                          ref LayoutBody? named)
  {
    foreach (object child in System.Windows.LogicalTreeHelper.GetChildren(root))
    {
      if (child is LayoutBody descendant)
      {
        if (IsNamedBody(descendant))
        { named = descendant; return true; }
        all.Add(descendant);
      }

      if (child is System.Windows.DependencyObject element && CollectLayoutBodies(element, all, ref named))
        return true;
    }

    return false;
  }
#endif
}
