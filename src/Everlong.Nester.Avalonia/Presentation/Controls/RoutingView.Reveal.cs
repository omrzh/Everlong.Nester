// NOTE: Single-source file — the WPF project compiles this exact file via
// <Compile Include> in Everlong.Nester.Wpf.csproj.  Edit it here only;
// never create a WPF-side copy (the two builds would drift).

using Everlong.Nester.Helpers;
using Everlong.Nester.Routing;
using Everlong.Nester.Shell;

namespace Everlong.Nester.Presentation;

internal sealed partial class RoutingView
{
  /// <inheritdoc />
  async Task IRoutingView.RevealAsync(IConvergenceScene scene)
  {
    // The bodies the mounting touched — their visibility settles onto
    // the active child at the very end, whatever the choreography did.
    var settledBodies = new HashSet<ILayoutBody<PControl>>();
    var mounted = new Dictionary<ILayoutBody<PControl>, IViewLocation<PControl>>();
    try
    {
      await RevealCoreAsync(scene, settledBodies, mounted);
    }
    finally
    {
      // The visibility half of the end state: only the active child of every
      // touched body is visible — read from the body's CURRENT active, so a
      // reveal that lands after a superseding one converges to the superseding
      // convergence.  The presentation state a director leaves (opacity,
      // hit-test, transform, z-index) is restored only on the non-superseded
      // path below; the superseding run owns it otherwise.
      foreach (ILayoutBody<PControl> body in settledBodies)
        body.SettleActive();
    }
  }

  /// <summary>
  ///   Mounts the entering chain into its parents' bodies (outermost first,
  ///   the outermost into this view), marks it active and runs the
  ///   transition: the entering side is laid out but invisible, the chain's
  ///   first <see cref="ISceneTransition" /> directs the change, and the
  ///   final visibility is restored.
  /// </summary>
  private async Task RevealCoreAsync(IConvergenceScene convergence,
                                     HashSet<ILayoutBody<PControl>> settledBodies,
                                     Dictionary<ILayoutBody<PControl>, IViewLocation<PControl>> mounted)
  {
    // ── The transfer's moving sides — the choreography's subjects ──
    // The arriving side spans from the first difference down, so a revision is
    // a subject too.  A node on both sides is re-engaged in place: it is not
    // re-mounted, not hidden and not dipped — the director owns whatever
    // re-entrance it gets.  The sides are chain slices, a handful of nodes:
    // the membership tests below scan them rather than build a set.
    List<PControl> arrivingViews = [];
    List<Location> enteringNodes = [];
    List<PControl> enteringViews = [];
    foreach (ILocation node in convergence.Arrivings)
    {
      if (node is not Location arrivingNode)
        continue;

      PControl? view = arrivingNode.Presenter as PControl;
      if (view is not null)
        arrivingViews.Add(view);

      if (Contains(convergence.Departings, arrivingNode))
        continue; // re-engaged in place — already mounted and visible
      enteringNodes.Add(arrivingNode);
      if (view is not null)
        enteringViews.Add(view);
    }

    List<PControl> departingViews = [];
    foreach (ILocation node in convergence.Departings)
    {
      if (node is Location departingNode && !Contains(convergence.Arrivings, departingNode)
          && departingNode.Presenter is PControl view)
        departingViews.Add(view);
    }

    // The layout probe is the innermost entering view: the mount cascade
    // reaches it last, so its arrangement implies every ancestor's.
    PControl? layoutProbe = enteringViews.Count > 0 ? enteringViews[^1] : null;

    // ── The transition's kind and its director ──
    // One shape decides both: a side with nothing entering is the current view
    // re-presented in place, and an entering side is a view arriving — the
    // director is read off the same side the kind walks.
    TransitionKind kind = SceneKind(convergence, enteringNodes.Count > 0);
    IReadOnlyList<PControl> directorChain = kind is TransitionKind.Exit or TransitionKind.Dismiss
                                                     ? departingViews
                                                     : arrivingViews;
    PControl? directorView = directorChain.FirstOrDefault(v => v is ISceneTransition);

    // ── Mount the entering side (outermost first, the outermost into this view) ──
    if (enteringNodes.Count > 0)
    {
      for (int i = 0; i < enteringNodes.Count; i++)
      {
        Location chainNode = enteringNodes[i];
        if (chainNode.Presenter is not PControl)
          continue;

        // Every node of a platform chain is a PlatformLocation — the model
        // materializes them through CreateLocation.  The mount parent is the
        // node's own chain parent — the shared prefix's site, or this view at
        // the root — never a positional alignment with the resolved chain.
        var location = (PlatformLocation)chainNode;
        ILayoutBody<PControl>? parentBody = chainNode.Parent is { } parent
                                                     ? BodyOf((PlatformLocation)parent)
                                                     : this;
        if (parentBody is null)
          continue;
        settledBodies.Add(parentBody);

        // The location is its own mount child — adding is idempotent by
        // reference, so a restored visit re-enters its body without
        // duplicating the panel child.
        parentBody.Add(location);
        parentBody.SetActiveChild(location);
        mounted[parentBody] = location;
      }
    }

    // ── Director animation (failure-isolated — a throwing director must
    // not block the final visibility or the entry-release) ──
    //
    // The director is the only subject of the prep: an entering view is
    // mounted, laid out and invisible when the method runs, and the layout
    // wait exists to make that true.  A convergence no moving-side view
    // directs — or one whose stage carries no flying plane — mounts and shows
    // in this same turn: no dip to opacity 0, and no dispatcher pass to wait
    // on.
    if (directorView is ISceneTransition director && Stage?.FlyingCanvas is { } canvas)
    {
      // ── the entering side is laid out but invisible ──
      foreach (PControl view in enteringViews)
      {
        SetViewVisible(view, true);
        view.Opacity = 0;
        view.IsHitTestVisible = false;
      }

      // The subject is the entering view, not this view — a derived router's
      // view joins the tree in this very turn — and the gate is the stage,
      // the signal that layout is possible at all.
      if (layoutProbe is { } probe &&
          Stage is { } stage && stage.IsInVisualTree())
        await UIDispatcher.WaitForLayoutAsync(probe, convergence.Lifetime);

      var transition = new TransitionContext(canvas, kind)
      {
        ArrivingChain = arrivingViews,
        DepartingChain = departingViews,
      };

      try
      {
        if (kind is TransitionKind.Exit or TransitionKind.Dismiss)
          await director.AnimateExitAsync(transition, convergence.Lifetime);
        else
          await director.AnimateEnterAsync(transition, convergence.Lifetime);
      }
      catch (OperationCanceledException) when (convergence.Lifetime.IsCancellationRequested)
      {
        // Superseded mid-flight — benign: the superseding run's reveal owns
        // the end state (the settle in the finally converges to it).
      }
      catch (Exception e)
      {
        Shell?.ReportError(e);
      }
    }

    // ── Final visibility: arriving shown, departing cascade-hidden.  A
    // reveal superseded while suspended (a newer reveal moved its body's
    // active child) skips this block entirely — the settle at the end
    // converges visibility to the superseding convergence, and the state
    // this block would restore is the superseding run's to own. ──
    if (!IsSuperseded(mounted))
    {
      foreach (PControl view in arrivingViews)
      {
        SetViewVisible(view, true);
        view.Opacity = 1;
        view.IsHitTestVisible = true;
      }

      foreach (PControl view in departingViews)
      {
        SetViewVisible(view, false);
        view.Opacity = 1;
        view.IsHitTestVisible = false;
#if AVALONIA
        view.RenderTransform = null;
        view.ZIndex = 0;
#else
        view.RenderTransform = null;
        System.Windows.Controls.Panel.SetZIndex(view, 0);
#endif
      }
    }
  }

  /// <summary>Whether <paramref name="side" /> holds <paramref name="node" /> by reference.</summary>
  private static bool Contains(IReadOnlyList<ILocation> side, Location node)
  {
    for (int i = 0; i < side.Count; i++)
    {
      if (ReferenceEquals(side[i], node))
        return true;
    }

    return false;
  }

  /// <summary>
  ///   The scene kind of a transfer — a moving side with nothing entering is
  ///   the current view re-presented in place; an entering side is a view
  ///   arriving.
  /// </summary>
  private static TransitionKind SceneKind(IConvergenceScene scene, bool anyEntering)
    => scene.Direction switch
    {
      RoutingDirection.Close => TransitionKind.Dismiss,
      RoutingDirection.Back => TransitionKind.Exit,
      _ when scene.Arrivings.Count == 0 => TransitionKind.Exit,
      _ when !anyEntering => TransitionKind.Refresh,
      _ => TransitionKind.Enter,
    };

  /// <summary>Whether a newer reveal already moved a mounted body's active child past this change.</summary>
  private static bool IsSuperseded(
    IReadOnlyDictionary<ILayoutBody<PControl>, IViewLocation<PControl>> mounted)
  {
    foreach (var pair in mounted)
      if (!ReferenceEquals(pair.Key.ActiveChild, pair.Value))
        return true;
    return false;
  }

  private static void SetViewVisible(PControl view, bool visible)
  {
#if AVALONIA
    view.IsVisible = visible;
#else
    view.Visibility = visible ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
#endif
  }
}
