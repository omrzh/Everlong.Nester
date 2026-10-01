using Everlong.Nester.Layer;
using Microsoft.Extensions.DependencyInjection;

namespace Everlong.Nester.Routing;

/// <summary>
///   The per-scope creation parameters of a router.
/// </summary>
public interface IRouterSeed
{
  /// <summary>The router's role.</summary>
  RouterRole Role { get; }

  /// <summary>The plane this router's lease is granted in.</summary>
  LayerPlane Plane { get; }

  /// <summary>The scope this router owns, or <see langword="null" /> for the base router.</summary>
  IServiceScope? OwnScope { get; }

  /// <summary>The lease of the layer this router was derived from, or <see langword="null" /> for the base router.</summary>
  ILayerLease? Counterpart { get; }

  /// <summary>The parent targets every route the router computes is completed with, outermost first; empty for the base router.</summary>
  IReadOnlyList<ITarget> Parents { get; }

  /// <summary>Initializes the seed for a router.</summary>
  void Initialize(RouterRole role, LayerPlane plane, IServiceScope? ownScope,
                 IReadOnlyList<ITarget>? parents = null, ILayerLease? counterpart = null);
}

/// <summary>
///   The default router seed — uninitialized it describes the base router:
///   role <see cref="RouterRole.Base" /> in <see cref="LayerPlane.Base" />, no
///   owner.
/// </summary>
public sealed class RouterSeed : IRouterSeed
{
  /// <inheritdoc />
  public RouterRole Role { get; private set; } = RouterRole.Base;

  /// <inheritdoc />
  public LayerPlane Plane { get; private set; } = LayerPlane.Base;

  /// <inheritdoc />
  public IServiceScope? OwnScope { get; private set; }

  /// <inheritdoc />
  public IReadOnlyList<ITarget> Parents { get; private set; } = [];

  /// <inheritdoc />
  public ILayerLease? Counterpart { get; private set; }

  /// <inheritdoc />
  public void Initialize(RouterRole role, LayerPlane plane, IServiceScope? ownScope,
                         IReadOnlyList<ITarget>? parents = null, ILayerLease? counterpart = null)
  {
    Role = role;
    Plane = plane;
    OwnScope = ownScope;
    Parents = parents ?? [];
    Counterpart = counterpart;
  }
}
