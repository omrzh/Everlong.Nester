using System.ComponentModel;

namespace Everlong.Nester.Routing;

/// <summary>
///   The run of a landed navigation — the committed decision the convergence
///   phases consume and the observation surface its callbacks receive.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IConvergenceContext : IConvergenceScene, IRoutingContext
{
  /// <summary>The presented site this overlay presentation borrowed, or <see langword="null" /> for the base router.</summary>
  Location? Borrowed { get; }

  /// <summary>The participants whose arrival convergence replays — the arriving side as concrete nodes.</summary>
  IReadOnlyList<Location> ArrivingNodes { get; }

  /// <summary>The participants whose departure convergence runs — the departing side as concrete nodes.</summary>
  IReadOnlyList<Location> DepartingNodes { get; }

  /// <summary>The run's observation channel — completes when the run settles.</summary>
  Task Completion { get; }

  /// <summary>Ends the run — completes the observation channel and cancels the transfer's lifetime token, each once.</summary>
  void End();
}
