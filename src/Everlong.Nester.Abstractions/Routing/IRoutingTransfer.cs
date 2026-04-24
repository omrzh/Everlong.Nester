namespace Everlong.Nester.Routing;

/// <summary>
///   One routing transfer — the direction it applies, the token that bounds
///   its lifetime and the two chains it moves.
/// </summary>
public interface IRoutingTransfer
{
  /// <summary>The direction the transfer applies.</summary>
  RoutingDirection Direction { get; }

  /// <summary>The token that bounds this transfer's lifetime — cancelled when its convergence ends, when a newer transfer supersedes it, when it leaves the pipe uncommitted, or when the router tears down.</summary>
  CancellationToken Lifetime { get; }

  /// <summary>The arriving side — the new chain from the transfer's first difference down to the content target; the whole chain on a refresh, empty on a close.</summary>
  IReadOnlyList<ILocation> Arrivings { get; }

  /// <summary>The departing side — the old chain from the transfer's first difference down to its content target; the whole chain on a close and on a refresh, empty when nothing was presented.</summary>
  IReadOnlyList<ILocation> Departings { get; }
}

/// <summary>
///   The read-only observation surface of one routing transfer — the
///   context delivered to lifecycle callbacks.
/// </summary>
public interface IRoutingContext : IRoutingTransfer
{
  /// <summary>The site this transfer lands on — the resolved content target; <see langword="null" /> when the transfer closes the presented layer.</summary>
  ILocation? Arrival { get; }

  /// <summary>The presented site this transfer departs from — <see langword="null" /> for initial routing.</summary>
  ILocation? Departure { get; }

  /// <summary>Whether this transfer runs on a derived router.</summary>
  bool IsElevated { get; }

  /// <summary>The transfer's features, keyed by interface type.</summary>
  IFeatureCollection Features { get; }
}
