using Everlong.Nester.Layer;

namespace Everlong.Nester.Routing;

/// <summary>
///   The creation parameters of a derived router — the band its lease is
///   granted in, the position asked for inside it, and the parent targets
///   every routed chain is completed with.
/// </summary>
/// <remarks>
///   The parents are the overlay's default layouts: the router holds the
///   targets, so the nodes they resolve to are shared across the overlay's
///   entries — a layout prepended to every route is one node, never a fresh
///   one per entry.
/// </remarks>
public sealed record DeriveOptions
{
  /// <summary>The band the derived router's lease is granted in.</summary>
  public required LayerBand Band { get; init; }

  /// <summary>The position asked for inside <see cref="Band" />.</summary>
  public LayerPolicy Policy { get; init; } = LayerPolicy.AboveHighest;

  /// <summary>
  ///   The parent targets every route the derived router computes is
  ///   completed with, outermost first — the overlay's default layouts.
  /// </summary>
  public IReadOnlyList<ITarget> Parents { get; init; } = [];
}
