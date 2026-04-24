namespace Everlong.Nester.Routing;


#region Transactional Callbacks

// The exception discipline of the routing callbacks — intentional consumer
// narrative, kept out of /// on purpose (xml-doc discipline: /// stays
// contract-only; this block is the sanctioned exemption).
//
//   Pre-commit phase (IParameterized, IAdaptiveParameterized, IRoutable,
//   IBodyChanged): the callbacks run before the commit, while the
//   transaction is still reversible.  A throw aborts the transaction —
//   nothing commits, no convergence launches, and the exception faults
//   the requester's await as-is.
//
//   Terminal closes are not transitions.  A close (a Direction =
//   Close) and a synchronous teardown fire no membership edges and
//   run no departure pair: the chain dies as a whole, and the release
//   drain runs IReleasable on every held member.  Every routing callback
//   therefore observes a real transition — none is ever called without a
//   context.

/// <summary>
///   A contract for membership notifications — the participant joined or
///   left the current chain through a navigation decision.
/// </summary>
/// <remarks>
///   Edge-triggered: fires on the membership flip only, never on data
///   refresh or re-arrival.
/// </remarks>
public interface IRoutable
{
  /// <summary>
  ///   Called when the participant becomes part of the current chain.
  ///   Synchronous side effects only.
  /// </summary>
  /// <param name="context">The routing context of the transition.</param>
  void OnRoutedTo(IRoutingContext context);

  /// <summary>
  ///   Called when the participant leaves the current chain, retained or not.
  /// </summary>
  /// <param name="context">The routing context of the transition.</param>
  void OnRoutedFrom(IRoutingContext context);
}

/// <summary>
///   A parameterized participant — an instance serves an engagement and
///   receives the arguments its route target carries.
/// </summary>
public interface IParameterized
{
  /// <summary>The arguments this instance serves now, read once after <see cref="DeliverArgs" /> — <see langword="null" /> means no correction: the requested arguments stand.</summary>
  IArgs? EngagedArgs { get; }

  /// <summary>Adopts the arguments this instance should serve.</summary>
  /// <param name="args">The arguments to adopt, or <see langword="null" /> when none were requested.</param>
  void DeliverArgs(IArgs? args);
}

/// <summary>
///   An adaptive parameterized participant — a live instance may serve a
///   revised request in place rather than being rebuilt.
/// </summary>
public interface IAdaptiveParameterized : IParameterized
{
  /// <summary>Reports whether this instance will serve the requested arguments in place.</summary>
  /// <param name="requested">The arguments being considered, or <see langword="null" /> when none were requested.</param>
  /// <remarks>No side effects — matching may consult it speculatively.</remarks>
  bool IsAdaptable(IArgs? requested);
}


/// <summary>
///   A contract for reacting when the membership of this participant's body
///   changes — the participants below it in the presented chain.
/// </summary>
/// <remarks>
///   The body's participants have not arrived when this fires.  A participant
///   that stays presented with a new engagement keeps its place in the
///   membership, so the callback does not fire for it: the value it serves is
///   read from the instance (<see cref="IParameterized.EngagedArgs" />) or
///   from <see cref="IRouterStack" />.
/// </remarks>
public interface IBodyChanged
{
  /// <summary>
  ///   Called when the membership of this participant's body changes.
  /// </summary>
  /// <param name="body">The body's participants, outermost first; each instance carries the value it serves.</param>
  void OnBodyChanged(IReadOnlyList<object> body);
}

/// <summary>
///   A contract for releasing resources when a participant is no longer
///   held by the chain.
/// </summary>
public interface IReleasable
{
  /// <summary>
  ///   Called when this participant is released from the chain.
  /// </summary>
  void Release();
}

#endregion
