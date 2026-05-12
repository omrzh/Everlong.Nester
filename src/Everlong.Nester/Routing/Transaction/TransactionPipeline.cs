namespace Everlong.Nester.Routing;

/// <summary>
///   The transaction pipe — one sub-pipe per direction, each carrying the same
///   guard, pre-routing say, request window and plan expansion around the
///   direction's own decision, over the suffix every direction shares.  A
///   transaction lands on its own sub-pipe at intake.
/// </summary>
internal partial class TransactionPipeline
{
  private readonly RouterBase router;
  private readonly TransactNext routingPipe;
  private readonly TransactNext traversePipe;
  private readonly TransactNext jumpPipe;
  private readonly TransactNext refreshPipe;
  private readonly TransactNext closePipe;

  /// <summary>Folds the pipes — the suffix every direction shares, and one pipe per direction in front of it.</summary>
  /// <param name="router">The router the stages act on.</param>
  internal TransactionPipeline(RouterBase router)
  {
    this.router = router;

    // The five directions, each written out whole.  Only a route carries a
    // descriptor: it is the one direction that materializes a chain, so it is
    // the one the request hook and the expansion are asked for.  Every other
    // direction replays or departs an engagement that already exists, and
    // walks neither.
    routingPipe = Fold([EnsureTransaction, router.InvokeHandleRouteRequest, HoldWindow, ExpandLocator, FillRoute, Deliver, Notify], Settle);
    traversePipe = Fold([EnsureTransaction, HoldWindow, FillTraverse, Notify], Settle);
    jumpPipe = Fold([EnsureTransaction, HoldWindow, FillJump, Notify], Settle);
    refreshPipe = Fold([EnsureTransaction, HoldWindow, FillRefresh, Notify], Settle);
    closePipe = Fold([EnsureTransaction, HoldWindow, FillClose, Notify], Settle);
  }

  /// <summary>Lands a transaction on its own direction's pipe.</summary>
  internal void Handle(TransactionContext context)
  {
    switch (context.Direction)
    {
      case RoutingDirection.Route:
        routingPipe(context);
        break;
      case RoutingDirection.Back:
      case RoutingDirection.Forward:
        traversePipe(context);
        break;
      case RoutingDirection.Jump:
        jumpPipe(context);
        break;
      case RoutingDirection.Refresh:
        refreshPipe(context);
        break;
      case RoutingDirection.Close:
        closePipe(context);
        break;
      default:
        InvalidDirection(context);
        break;
    }
  }

  /// <summary>Links the stages over the endpoint — each stage's continuation is the rest of the pipe, built once at fold time.</summary>
  internal static TransactNext Fold(IReadOnlyList<TransactStage> stages, TransactNext terminal)
  {
    TransactNext rest = terminal;
    for (int i = stages.Count - 1; i >= 0; i--)
    {
      TransactStage stage = stages[i];
      TransactNext inner = rest;
      rest = ctx => stage(ctx, inner);
    }
    return rest;
  }

  /// <summary>
  ///   Holds the model's sync transaction window open while the pipe lands the
  ///   transaction; trims are declined while it is open.
  /// </summary>
  private void HoldWindow(TransactionContext ctx, TransactNext next)
  {
    router.Model.BeginTransactionWindow();
    try
    {
      next(ctx);
    }
    finally
    {
      router.Model.EndTransactionWindow();
    }
  }

  /// <summary>
  ///   Resolves the transaction's locator into its spec — the stage that closes
  ///   the request window: after this the ordered chain the decision phases
  ///   read stands, and nothing may rewrite it.
  /// </summary>
  private void ExpandLocator(TransactionContext ctx, TransactNext next)
  {
    if (ctx.Locator is { } location)
      ctx.ExpandedLocator = location.Path;
    next(ctx);
  }

  /// <summary>Fills the back/forward decision and runs the rest of the pipe when the entry landed.</summary>
  private void FillTraverse(TransactionContext ctx, TransactNext next)
  {
    var direction = ctx.Direction;
    var chain = direction == RoutingDirection.Back ? router.Model.PreviousEntry : router.Model.NextEntry;
    if (chain is null || chain.Length == 0)
      return;

    var onScreen = router.Model.CurrentChain!;
    int from = TransactionContext.SharedPrefix(onScreen, chain);
    ctx.Land(chain, onScreen, from, from);

    next(ctx);
  }

  /// <summary>Fills the refresh decision and runs the rest of the pipe when the chain re-armed.</summary>
  private void FillRefresh(TransactionContext ctx, TransactNext next)
  {
    var chain = router.Model.CurrentChain;
    if (chain.Length == 0)
      return; // nothing presented — no arrival to replay; the pipe stops uncommitted

    // A refresh re-engages the whole chain: the same chain on both sides, so
    // every position pairs on its own node.
    ctx.Land(chain, chain, 0, 0);
    next(ctx);
  }

  /// <summary>
  ///   Fills the close decision — the presented chain leaves entirely (the
  ///   convergence's whole leaving side).  The stack stays intact through the
  ///   close — the layer presents its departing chain until the model dies;
  ///   the close run's drain releases every held member at the convergence's
  ///   end.  The pipe stops uncommitted when the model holds nothing to close.
  /// </summary>
  private void FillClose(TransactionContext ctx, TransactNext next)
  {
    Location[] chain = router.Model.CurrentChain;
    if (chain.Length == 0)
      return;

    // A close departs the whole chain — nothing arrives.
    ctx.Land(chain, chain, chain.Length, 0);
    next(ctx);
  }
}
