using System.ComponentModel;

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
}
