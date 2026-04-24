namespace Everlong.Nester.Routing;


#region ConvergenceCallbacks

// The convergence-phase exception discipline — intentional consumer narrative,
// kept out of /// on purpose (xml-doc discipline: /// stays contract-only;
// this block is the sanctioned exemption).
//
//   The arrival / departure / release callbacks run during the
//   convergence, after the commit.  A throw (or a faulted task) is
//   quarantined: it rides the router's error channel, the convergence
//   continues for the remaining participants, and nothing rolls back.



/// <summary>
///   A contract for executing logic when a participant is about to arrive.
/// </summary>
public interface IArriving
{
  /// <summary>
  ///   Called before the participant arrives.  The returned task is
  ///   awaited before the arrival completes.
  /// </summary>
  /// <param name="context">The routing context of this arrival.</param>
  Task OnArrivingAsync(IRoutingContext context);
}

/// <summary>
///   A contract for executing logic when a participant has arrived.
/// </summary>
public interface IArrived
{
  /// <summary>
  ///   Called when this participant has arrived.
  /// </summary>
  /// <param name="context">The routing context of this arrival.</param>
  Task OnArrivedAsync(IRoutingContext context);
}

/// <summary>
///   A contract for executing logic before a participant departs.
/// </summary>
public interface IDeparting
{
  /// <summary>
  ///   Called before this participant departs, while it is still
  ///   presented.
  /// </summary>
  /// <param name="context">The routing context of the transition.</param>
  void OnDeparting(IRoutingContext context);
}

/// <summary>
///   A contract for executing logic after a participant departs.
/// </summary>
public interface IDeparted
{
  /// <summary>
  ///   Called after this participant departs.
  /// </summary>
  /// <param name="context">The routing context of the transition.</param>
  void OnDeparted(IRoutingContext context);
}

#endregion
