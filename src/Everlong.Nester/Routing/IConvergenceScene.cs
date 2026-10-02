using System.ComponentModel;
using Everlong.Nester.Layer;

namespace Everlong.Nester.Routing;

/// <summary>
///   The scene of a landed navigation — the resolved chain and the transfer's
///   two moving sides.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IConvergenceScene : IRoutingTransfer
{
  /// <summary>The resolved chain — participants outermost first, the content target last.</summary>
  IReadOnlyList<Location> Chain { get; }

  /// <summary>
  ///   The lease of the layer this convergence crosses to or from, or
  ///   <see langword="null" /> when the transfer moves inside one layer — the
  ///   layer the transitioning router was derived from.
  /// </summary>
  ILayerLease? Counterpart { get; }
}
