// NOTE: Single-source file — the WPF project compiles this exact file via
// <Compile Include> in Everlong.Nester.Wpf.csproj.  Edit it here only;
// never create a WPF-side copy (the two builds would drift).

using Everlong.Nester.Controls;
using Everlong.Nester.Helpers;
using Everlong.Nester.Presentation;
using Everlong.Nester.Shell;
using Microsoft.Extensions.DependencyInjection;

namespace Everlong.Nester.Routing;

/// <summary>
///   The platform view assembly — creates each view-less node's view through
///   the shell's view locator and resolves its layout body.  Shared nodes
///   keep the views they already hold.
/// </summary>
internal sealed class AssembleViewsStage
{
  private readonly NavigationHost _host;

  internal AssembleViewsStage(NavigationHost host) => _host = host;

  internal void Assemble(IReadOnlyList<Location> chain)
  {
    // The host is a platform view — its shell comes from the visual tree
    // (the stage's inherited attached property).  A host that never entered
    // the tree (bare test construction) has no shell: no views to assemble.
    IShell? shell = _host.GetShell();
    if (shell is null)
      return;

    IViewLocator<PControl> viewLocator = shell.GetPlatformService<IViewLocator<PControl>>()
      ?? throw new InvalidOperationException(
        "The shell provides no view locator — GetPlatformService<IViewLocator<T>>() returned null.");

    foreach (Location node in chain)
    {
      if (node.Presenter is not null)
        continue;
      var location = (PlatformLocation)node;
      location.View = BuildView(location.Instance, viewLocator, out ILayoutBody<PControl>? body);
      location.Body = body;
    }
  }

  private static PControl? BuildView(object viewModel,
                                            IViewLocator<PControl> viewLocator,
                                            out ILayoutBody<PControl>? body)
  {
#if AVALONIA
    return AvaloniaShell.BuildView(viewModel, viewLocator, out body);
#else
    return WpfShell.BuildView(viewModel, viewLocator, out body);
#endif
  }
}

/// <summary>
///   The platform reveal — mounts the entering chain into its parents'
///   bodies (outermost first, the outermost into the host), marks it
///   active, and runs the transition: the arriving chain is laid out but
///   invisible, the chain's first <see cref="ISceneTransition" /> directs
///   the change, and the final visibility is restored.
/// </summary>
internal sealed class PlatformRevealStage
{
  private readonly NavigationHost _host;
  private readonly Lazy<PCanvas?> _flyingCanvas;

  internal PlatformRevealStage(NavigationHost host)
  {
    _host = host;
    _flyingCanvas = new(() => _host.GetShell()?.Services?.GetService<ShellFlyingLayer>()?.Canvas);
  }

  internal async Task RevealAsync(IConvergenceScene convergence)
  {
    // The bodies the mounting touched — their visibility settles onto
    // the active child at the very end, whatever the choreography did.
    var settledBodies = new HashSet<ILayoutBody<PControl>>();
    var mounted = new Dictionary<ILayoutBody<PControl>, IViewLocation<PControl>>();
    try
    {
      await RevealAsync(convergence, settledBodies, mounted);
    }
    finally
    {
      // The settled end-state invariant: only the active child of every
      // touched body is visible — read from the body's CURRENT active, so
      // a reveal that lands after a superseding one converges to the
      // superseding convergence.
      foreach (ILayoutBody<PControl> body in settledBodies)
        body.SettleActive();
    }
  }

  private async Task RevealAsync(IConvergenceScene convergence,
                                 HashSet<ILayoutBody<PControl>> settledBodies,
                                 Dictionary<ILayoutBody<PControl>, IViewLocation<PControl>> mounted)
  {
    // ── The transfer's moving sides — the choreography's subjects ──
    // The arriving side spans from the first difference down, so a revision is
    // a subject too.  A node on both sides is re-engaged in place: it is not
    // re-mounted, not hidden and not dipped — the director owns whatever
    // re-entrance it gets.
    var arriving = new HashSet<Location>(ReferenceEqualityComparer.Instance);
    foreach (ILocation node in convergence.Arrivings)
    {
      if (node is Location arrivingNode)
        arriving.Add(arrivingNode);
    }

    var departing = new HashSet<Location>(ReferenceEqualityComparer.Instance);
    foreach (ILocation node in convergence.Departings)
    {
      if (node is Location departingNode)
        departing.Add(departingNode);
    }

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

      if (departing.Contains(arrivingNode))
        continue; // re-engaged in place — already mounted and visible
      enteringNodes.Add(arrivingNode);
      if (view is not null)
        enteringViews.Add(view);
    }

    List<PControl> departingViews = [];
    foreach (ILocation node in convergence.Departings)
    {
      if (node is Location departingNode && !arriving.Contains(departingNode)
          && departingNode.Presenter is PControl view)
        departingViews.Add(view);
    }

    PControl? arrivingView = enteringViews.Count > 0 ? enteringViews[^1] : null;

    // ── The transition's kind and its director ──
    // One shape decides both: a side with nothing entering is the current view
    // re-presented in place, and an entering side is a view arriving — the
    // director is read off the same side the kind walks.
    TransitionKind kind = SceneKind(convergence, enteringNodes.Count > 0);
    IReadOnlyList<PControl> directorChain = kind is TransitionKind.Exit or TransitionKind.Dismiss
                                                     ? departingViews
                                                     : arrivingViews;
    PControl? directorView = directorChain.FirstOrDefault(v => v is ISceneTransition);

    // ── Mount the entering side (outermost first, the outermost into the host) ──
    if (enteringNodes.Count > 0)
    {
      for (int i = 0; i < enteringNodes.Count; i++)
      {
        Location chainNode = enteringNodes[i];
        if (chainNode.Presenter is not PControl)
          continue;

        // Every node of a platform chain is a PlatformLocation — the model
        // materializes them through CreateLocation.  The mount parent is the
        // node's own chain parent — the shared prefix's site, or the host at
        // the root — never a positional alignment with the resolved chain.
        var location = (PlatformLocation)chainNode;
        ILayoutBody<PControl>? parentBody = chainNode.Parent is { } parent
                                                     ? ((PlatformLocation)parent).Body
                                                     : _host;
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

    // ── the entering side is laid out but invisible ──
    foreach (PControl view in enteringViews)
    {
      SetVisible(view, true);
      view.Opacity = 0;
      view.IsHitTestVisible = false;
    }

    if (_host is ILayoutBody<PControl> { IsAttachedToVisualTree: true } &&
        arrivingView is { IsLoaded: false })
      await UIDispatcher.WaitForLoadedAsync();

    // ── Director animation (failure-isolated — a throwing director must
    // not block the final visibility or the entry-release) ──
    if (directorView is ISceneTransition director && _flyingCanvas.Value is { } canvas)
    {
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
        _host.GetShell()?.ReportError(e);
      }
    }

    // ── Final visibility: arriving shown, departing cascade-hidden.  A
    // reveal superseded while suspended (a newer reveal moved its body's
    // active child) skips the switch — the settle at the end converges to
    // the superseding convergence. ──
    if (!IsSuperseded(mounted))
    {
      foreach (PControl view in arrivingViews)
      {
        SetVisible(view, true);
        view.Opacity = 1;
        view.IsHitTestVisible = true;
      }

      foreach (PControl view in departingViews)
      {
        SetVisible(view, false);
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

  /// <summary>
  ///   Removes the released nodes' views from the visual tree — shared
  ///   layout nodes (still referenced by a live chain) never reach this
  ///   stage: the release drain filters them before it.
  /// </summary>
  internal void RemoveReleasedViews(IReadOnlyList<Location> released)
  {
    foreach (Location node in released)
    {
      if (node.Presenter is not PControl)
        continue;

      var location = (PlatformLocation)node;
      ILayoutBody<PControl>? parentBody = node.Parent is null
                                                   ? _host as ILayoutBody<PControl>
                                                   : ((PlatformLocation)node.Parent).Body;
      if (parentBody is null)
        continue;

      // The location is its own mount child — removal is idempotent: a
      // node that never mounted is not registered in any body.
      parentBody.Remove(location);
    }
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

  private static void SetVisible(PControl view, bool visible)
  {
#if AVALONIA
    view.IsVisible = visible;
#else
    view.Visibility = visible ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
#endif
  }
}
