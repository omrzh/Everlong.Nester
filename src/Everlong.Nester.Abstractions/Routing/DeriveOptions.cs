using Everlong.Nester.Layer;

namespace Everlong.Nester.Routing;

/// <summary>
///   The creation parameters of a derived router — the plane its lease is
///   granted in and the parent targets every routed chain is completed with.
/// </summary>
/// <remarks>
///   The parents are the overlay's default layouts: the router holds the
///   targets, so the nodes they resolve to are shared across the overlay's
///   entries — a layout prepended to every route is one node, never a fresh
///   one per entry.
/// </remarks>
public sealed record DeriveOptions
{
  /// <summary>The plane the derived router's lease is granted in.</summary>
  public LayerPlane Plane { get; init; } = LayerPlane.Overlay;

  /// <summary>
  ///   The parent targets every route the derived router computes is
  ///   completed with, outermost first — the overlay's default layouts.
  /// </summary>
  public IReadOnlyList<ITarget> Parents { get; init; } = [];
}
