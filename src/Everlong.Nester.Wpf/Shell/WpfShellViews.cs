using Everlong.Nester.Controls;
using Everlong.Nester.Presentation;
using System.Windows;

namespace Everlong.Nester.Shell;

public partial class WpfShell
{
  /// <summary>
  ///   Builds a view for a visit ViewModel via the stage's view locator,
  ///   wiring the data context and the layout body — the shared mount
  ///   sequence of the navigation and dialog pipelines.  Returns
  ///   <see langword="null" /> when the locator has no view.
  /// </summary>
  internal static PControl? BuildView(object viewModel, IViewLocator<PControl> viewLocator,
                                             out ILayoutBody<PControl>? body)
  {
    PControl? view = viewLocator.Build(viewModel);
    if (view is null)
    {
      body = null;
      return null;
    }

    view.DataContext = viewModel;
    body = TryResolveLayoutBody(view);
    return view;
  }

  internal static ILayoutBody? TryResolveLayoutBody(PControl layout)
  {
    if (layout is ILayoutControl layoutControl)
      return layoutControl.GetLayoutBody();

    TryFindLayoutBody(layout, out LayoutBody? body);
    return body;
  }

  private static bool IsNamedBody(LayoutBody layoutBody)
    => string.Equals(layoutBody.Name, "Body", StringComparison.Ordinal);

  private static bool TryFindLayoutBody(PControl layout, out LayoutBody? body)
  {
    List<LayoutBody> all = [];
    body = null;

    if (layout is LayoutBody rootLb)
    {
      if (IsNamedBody(rootLb))
      { body = rootLb; return true; }
      all.Add(rootLb);
    }

    CollectLayoutBodies(layout, all, ref body);
    if (body is not null)
      return true;

    if (all.Count == 1)
    { body = all[0]; return true; }

    body = null;
    return false;
  }

  private static bool CollectLayoutBodies(DependencyObject root, List<LayoutBody> all, ref LayoutBody? named)
  {
    foreach (object child in LogicalTreeHelper.GetChildren(root))
    {
      if (child is LayoutBody lb)
      {
        if (IsNamedBody(lb))
        { named = lb; return true; }
        all.Add(lb);
      }

      if (child is DependencyObject dep && CollectLayoutBodies(dep, all, ref named))
        return true;
    }
    return false;
  }
}
