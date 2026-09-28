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

  /// <summary>The band this router's lease is granted in.</summary>
  LayerBand Band { get; }

  /// <summary>The policy this router's lease is granted under.</summary>
  LayerPolicy Policy { get; }

  /// <summary>The scope this router owns, or <see langword="null" /> for the base router.</summary>
  IServiceScope? OwnScope { get; }

  /// <summary>The parent targets every route the router computes is completed with, outermost first; empty for the base router.</summary>
  IReadOnlyList<ITarget> Parents { get; }

  /// <summary>Initializes the seed for a router.</summary>
  void Initialize(RouterRole role, LayerBand band, LayerPolicy policy, IServiceScope? ownScope,
                 IReadOnlyList<ITarget>? parents = null);
}

/// <summary>
///   The default router seed — uninitialized it describes the base router:
///   role <see cref="RouterRole.Base" /> at the navigation band's floor, no
///   owner.
/// </summary>
public sealed class RouterSeed : IRouterSeed
{
  /// <inheritdoc />
  public RouterRole Role { get; private set; } = RouterRole.Base;

  /// <inheritdoc />
  public LayerBand Band { get; private set; } = KnownLayers.Navigation;

  /// <inheritdoc />
  public LayerPolicy Policy { get; private set; } = LayerPolicy.Floor;

  /// <inheritdoc />
  public IServiceScope? OwnScope { get; private set; }

  /// <inheritdoc />
  public IReadOnlyList<ITarget> Parents { get; private set; } = [];

  /// <inheritdoc />
  public void Initialize(RouterRole role, LayerBand band, LayerPolicy policy, IServiceScope? ownScope,
                         IReadOnlyList<ITarget>? parents = null)
  {
    Role = role;
    Band = band;
    Policy = policy;
    OwnScope = ownScope;
    Parents = parents ?? [];
  }
}
