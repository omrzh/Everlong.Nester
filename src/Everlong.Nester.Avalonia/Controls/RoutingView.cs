// NOTE: Single-source file — the WPF project compiles this exact file via
// <Compile Include> in Everlong.Nester.Wpf.csproj.  Edit it here only;
// never create a WPF-side copy (the two builds would drift).


using Everlong.Nester.Presentation;
using Everlong.Nester.Routing;
using Everlong.Nester.Shell;

namespace Everlong.Nester.Controls;

/// <summary>
///   The platform routing view — the layout body that hosts the presented
///   chain's outermost view, carries the model's committed state, assembles
///   the resolved chain's views and reveals each convergence.
/// </summary>
internal sealed partial class RoutingView : LayoutBody, IRoutingView, IFocusPolicySurface
{
  /// <inheritdoc />
  public FocusPolicy FocusPolicy
  {
    get;
    set
    {
      if (field == value)
        return;
      field = value;
      FocusPolicyChanged?.Invoke();
    }
  } = FocusPolicy.Reachable;

  /// <inheritdoc />
  public event Action? FocusPolicyChanged;

  public ILocation? Location { get; private set; }
  public IRouter? Router { get; private set; }

  void IRoutingView.SetLocation(ILocation? location)
  {
    Location = location;

    // The site is also mirrored on the host's own attached property — a
    // host-local write (not inherited) that lets a consumer discovering the
    // surface from outside read it.  The nav chrome (RouteLink /
    // RouteTreeItem) reads this host's router stack instead.
    SetValue(RoutingChannel.LocationProperty, location);
  }


  void IRoutingView.SetRouter(IRouter? router)
  {
    Router = router;
    SetValue(RoutingChannel.RouterProperty, router);
  }

  // ── The stage this view belongs to ────────────────────────────────────────
  //
  // One walk of the logical parents, then held.  The walk is the only way to
  // reach the stage: the view is constructed before its lease mounts it, and a
  // lease granted before the stage connects is back-filled onto it later, so an
  // assembly can run with no stage at all — a null answer is therefore never
  // cached, and the view keeps looking until the mount lands.  The stage is an
  // ancestor, so it cannot change without the view's place in the tree
  // changing: every tree transition below drops the cache.
  private StagePanel? _stage;

  private StagePanel? Stage => _stage ??= this.GetStage();

  private IShell? Shell => Stage?.Shell;

#if AVALONIA
  /// <inheritdoc />
  protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
  {
    base.OnAttachedToVisualTree(e);
    _stage = null;
  }

  /// <inheritdoc />
  protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
  {
    base.OnDetachedFromVisualTree(e);
    _stage = null;
  }
#else
  // WPF has no visual-tree attach/detach virtual; a visual parent change is
  // the same signal — the WPF LayoutBody fork tracks it through the layout's
  // source change for its own IsAttachedToVisualTree.
  /// <inheritdoc />
  protected override void OnVisualParentChanged(System.Windows.DependencyObject oldParent)
  {
    base.OnVisualParentChanged(oldParent);
    _stage = null;
  }
#endif

  /// <inheritdoc />
  void IRoutingView.AssembleViews(IReadOnlyList<Location> chain)
  {
    foreach (Location node in chain)
    {
      if (node.Presenter is not null)
        continue;
      var location = (PlatformLocation)node;

      // The view comes from the tree this view sits in — no shell, no
      // locator: the platform's own table is the translation table.
      PControl? view = ViewResolution.Build(this, location.Instance);
      if (view is null)
        continue; // no view for this instance — the node stays unassembled and retries

      // The data context is wired before the body is resolved: a view whose
      // content comes from its data context (an imperative content control)
      // builds that content on the assignment, and the body it carries is
      // only in the logical tree afterwards.
      view.DataContext = location.Instance;
      location.View = view;
      location.Body = ResolveLayoutBody(view);
    }
  }

  /// <inheritdoc />
  void IRoutingView.ReleaseViews(IReadOnlyList<Location> released)
  {
    foreach (Location node in released)
    {
      if (node.Presenter is not PControl)
        continue;

      var location = (PlatformLocation)node;
      ILayoutBody<PControl>? parentBody = node.Parent is null
                                                   ? this
                                                   : ((PlatformLocation)node.Parent).Body;
      if (parentBody is null)
        continue;

      // The location is its own mount child — removal is idempotent: a
      // node that never mounted is not registered in any body.
      parentBody.Remove(location);
    }
  }
}
