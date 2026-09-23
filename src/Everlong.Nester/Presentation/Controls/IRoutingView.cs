using Everlong.Nester.Routing;

namespace Everlong.Nester.Presentation;

/// <summary>
///   The root-view surface of a router's layer — carries the presented
///   chain and presents the router's convergences.
/// </summary>
/// <remarks>
///   All members run on the UI thread.  A presentation sequence is
///   <see cref="AssembleViews"/> before any view-side lifecycle hook,
///   <see cref="RevealAsync"/> after the arrival gate, and
///   <see cref="ReleaseViews"/> when the run drains its releases.
/// </remarks>
public interface IRoutingView
{
  /// <summary>The presented chain's content target — <see langword="null" /> when nothing is presented.</summary>
  ILocation? Location { get; }

  /// <summary> The router bound to this routing view. </summary>
  IRouter? Router { get; }

  /// <summary>Updates the status — the owning router's current site.</summary>
  void SetLocation(ILocation? location);

  /// <summary>Sets the router on this routing view.</summary>
  void SetRouter(IRouter router);

  /// <summary>Fills the resolved chain's view slots.  Idempotent — shared nodes keep their views.</summary>
  void AssembleViews(IReadOnlyList<Location> chain);

  /// <summary>Presents the convergence — the visual switch of its resolved chain.</summary>
  Task RevealAsync(IConvergenceScene scene);

  /// <summary>Removes the released nodes' views from the visual tree.</summary>
  void ReleaseViews(IReadOnlyList<Location> released);
}
