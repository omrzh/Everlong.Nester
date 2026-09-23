using Everlong.Nester.Presentation;
using Microsoft.Extensions.DependencyInjection;
using Terminal.Gui.ViewBase;

namespace Everlong.Nester.Routing;

/// <summary>
///   The terminal view assembly — creates each view-less node's view
///   through the window container's locator and attaches the participant.
///   Shared nodes keep the views they already hold.
/// </summary>
internal sealed class TerminalAssembleStage(IServiceProvider services)
{
  internal void Assemble(IReadOnlyList<Location> chain)
  {
    IViewLocator viewLocator = services.GetService<IViewLocator>()
      ?? throw new InvalidOperationException(
        "The window container provides no view locator — register IViewLocator " +
        "(a Terminal.Gui locator over the platform's view type).");

    foreach (Location node in chain)
    {
      if (node.Presenter is not null)
        continue;
      var location = (TerminalLocation)node;
      View? view = viewLocator.Match(location.Instance) ? viewLocator.Build(location.Instance) : null;
      if (view is null)
        continue;
      if (view is NesterView nesterView)
        nesterView.AttachDataContext(location.Instance);
      location.View = view;
      location.Body = ResolveBody(view);
    }
  }

  /// <summary>The node's inner mount point — its layout's declared Body slot.</summary>
  private static IBodyPanel<View>? ResolveBody(View view)
    => view is IBodyHolder holder ? holder.GetBodyPanel() : null;
}

/// <summary>
///   The terminal reveal — mounts the entering chain into its parents'
///   bodies (outermost first, the outermost into the host) and marks it
///   active; the settle restores the end-state invariant (only the active
///   child of every touched body is visible).
/// </summary>
internal sealed class TerminalRevealStage(IRoutingView host)
{
  internal Task RevealAsync(IConvergenceScene convergence)
  {
    var settledBodies = new HashSet<IBodyPanel<View>>();
    var mounted = new Dictionary<IBodyPanel<View>, IViewLocation<View>>();
    try
    {
      RevealCore(convergence, settledBodies, mounted);
    }
    finally
    {
      // The settled end-state invariant: only the active child of every
      // touched body is visible — read from the body's CURRENT active, so
      // a reveal that lands after a superseding one converges to the
      // superseding convergence.
      foreach (IBodyPanel<View> body in settledBodies)
        body.SettleActive();
    }

    return Task.CompletedTask;
  }

  private void RevealCore(IConvergenceScene convergence,
                          HashSet<IBodyPanel<View>> settledBodies,
                          Dictionary<IBodyPanel<View>, IViewLocation<View>> mounted)
  {
    // The arriving side spans from the transfer's first difference down; a node
    // on both sides is re-engaged in place and nothing re-mounts.
    var departing = new HashSet<Location>(ReferenceEqualityComparer.Instance);
    foreach (ILocation node in convergence.Departings)
    {
      if (node is Location departingNode)
        departing.Add(departingNode);
    }

    IReadOnlyList<ILocation> arrivings = convergence.Arrivings;
    int start = convergence.Chain.Count - arrivings.Count;
    bool mountedAny = false;
    for (int i = 0; i < arrivings.Count; i++)
    {
      if (arrivings[i] is not Location chainNode || chainNode.Presenter is not View)
        continue;
      if (departing.Contains(chainNode))
        continue; // re-engaged in place — already mounted

      // Every node of a platform chain is a TerminalLocation — the model
      // materializes them through CreateLocation.
      var location = (TerminalLocation)chainNode;
      IBodyPanel<View>? parentBody = start + i == 0
                                        ? host as IBodyPanel<View>
                                        : ((TerminalLocation)convergence.Chain[start + i - 1]).Body;
      if (parentBody is null)
        continue;
      settledBodies.Add(parentBody);

      // The location is its own mount child — adding is idempotent by
      // reference, so a restored visit re-enters its body without
      // duplicating the child.
      parentBody.Add(location);
      parentBody.SetActiveChild(location);
      mounted[parentBody] = location;
      mountedAny = true;
    }

    if (mountedAny)
      AdoptFloatingChain(convergence.Chain);
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
      if (node.Presenter is not View)
        continue;

      var location = (TerminalLocation)node;
      IBodyPanel<View>? parentBody = node.Parent is null
                                        ? host as IBodyPanel<View>
                                        : ((TerminalLocation)node.Parent).Body;
      if (parentBody is null)
        continue;

      // The location is its own mount child — removal is idempotent: a
      // node that never mounted is not registered in any body.
      parentBody.Remove(location);
    }
  }

  /// <summary>
  ///   Sizes the lease surface of a floating chain to its content — the
  ///   leaf declares <see cref="NesterView.FloatingSize" />; the surface
  ///   (and everything stretching inside it) shrinks to the window rect at
  ///   the screen center, so the chain clears only its own window instead
  ///   of wiping the surfaces beneath.
  /// </summary>
  private void AdoptFloatingChain(IReadOnlyList<Location> chain)
  {
    Location leaf = chain[^1];
    if (leaf.Presenter is not NesterView { StretchToBody: false, FloatingSize: { } size })
      return;

    // The lease surface above the router's navigation host.
    if (host is not View { SuperView: TuiLayer surface })
      return;

    surface.X = Pos.Center();
    surface.Y = Pos.Center();
    surface.Width = size.Width;
    surface.Height = size.Height;
  }
}
