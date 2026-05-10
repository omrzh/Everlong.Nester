using System.ComponentModel;

namespace Everlong.Nester.Routing;

/// <summary>Runs the rest of the transaction pipe over a transaction.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public delegate void TransactNext(TransactionContext context);

/// <summary>
///   A sync transaction-pipe stage — decides whether and how the rest of the
///   pipe (<paramref name="next" />) runs.  A stage that returns without
///   calling <paramref name="next" /> short-circuits the transaction.
/// </summary>
internal delegate void TransactStage(TransactionContext context, TransactNext next);

/// <summary>Runs the rest of the convergence pipe over a convergence context.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public delegate Task ConvergeNext(IConvergenceContext context);

/// <summary>
///   A convergence-pipe stage — decides whether and how the rest of the pipe
///   (<paramref name="next" />) runs.  A stage that returns without
///   awaiting <paramref name="next" /> skips the convergence it wraps.
/// </summary>
internal delegate Task ConvergeStage(IConvergenceContext ctx, ConvergeNext next);
