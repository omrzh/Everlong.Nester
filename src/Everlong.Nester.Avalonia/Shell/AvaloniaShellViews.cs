using Avalonia.LogicalTree;
using Everlong.Nester.Controls;
using Everlong.Nester.Presentation;

namespace Everlong.Nester.Shell;

public partial class AvaloniaShell
{
  /// <summary>
  ///   Builds a view for a visit ViewModel via the stage's view locator,
  ///   wiring the data context and the layout body — the shared mount
  ///   sequence of the navigation and dialog pipelines.  Returns
  ///   <see langword="null" /> when the locator has no view.
  /// </summary>
  internal static PlatformControl? BuildView(object viewModel, IViewLocator<PlatformControl> viewLocator,
                                             out ILayoutBody<PlatformControl>? body)
  {
    PlatformControl? view = viewLocator.Build(viewModel);
    if (view is null)
    {
      body = null;
      return null;
    }

    view.DataContext = viewModel;
    body = TryResolveLayoutBody(view);
    return view;
  }

  internal static ILayoutBody? TryResolveLayoutBody(PlatformControl layout)
  {
    if (layout is ILayoutControl layoutControl)
      return layoutControl.GetLayoutBody();

    TryFindLayoutBody(layout, out LayoutBody? body);
    return body;
  }

  private static bool IsNamedBody(LayoutBody layoutBody)
    => string.Equals(layoutBody.Name, "Body", StringComparison.Ordinal);

  private static bool TryFindLayoutBody(PlatformControl layout, out LayoutBody? body)
  {
    List<LayoutBody> all = [];

    if (layout is LayoutBody rootLb)
    {
      if (IsNamedBody(rootLb))
      {
        body = rootLb;
        return true;
      }

      all.Add(rootLb);
    }

    foreach (ILogical logical in layout.GetLogicalDescendants())
    {
      if (logical is not LayoutBody lb)
        continue;
      if (IsNamedBody(lb))
      {
        body = lb;
        return true;
      }

      all.Add(lb);
    }

    if (all.Count == 1)
    {
      body = all[0];
      return true;
    }

    body = null;
    return false;
  }
}
