namespace Everlong.Nester.Routing;

partial class TransactionPipeline
{
  /// <summary>
  ///   The endpoint — the commit: supersedes the active transaction, applies
  ///   the direction's own mutation, raises the surface notification and
  ///   completes the caller's await.  The landing is atomic: every delivery
  ///   and notification has run, nothing mutates behind it.
  /// </summary>
  private void Settle(TransactionContext ctx)
  {
    // The supersession — the landed truth takes the active slot and cancels
    // the transaction that held it.
    router.Runs.Supersede(ctx);

    // The atomic commit — each direction owns one named apply.
    switch (ctx.Direction)
    {
      case RoutingDirection.Route:
        ApplyRouting(ctx);
        break;
      case RoutingDirection.Back:
      case RoutingDirection.Forward:
        ApplyTraverse(ctx);
        break;
      case RoutingDirection.Jump:
        ApplyJump(ctx);
        break;
      case RoutingDirection.Refresh:
        ApplyRefresh(ctx);
        break;
      case RoutingDirection.Close:
        ApplyClose(ctx);
        break;
    }

    // The surface notification — post-commit and isolated: a failing
    // subscriber reports through the error channel, the commit stands.
    try
    {
      router.Model.RaiseNavigated();
    }
    catch (Exception e)
    {
      router.ReportFailures([e]);
    }

    // The landing — the convergence context is the run's observation handle:
    // its completion spans the convergence, whether the convergence runs now
    // or is superseded before it starts.  The context is assigned before the
    // relay completes so an awaiting caller observes it.
    ctx.Convergence = router.CreateConvergenceContext(ctx);
    ctx.Result.TrySetResult(true);
  }

  /// <summary>The route commit — a structural landing attaches its fresh nodes and pushes the chain; an absorption changes nothing (the revised nodes replay their arrival).</summary>
  private void ApplyRouting(TransactionContext ctx)
  {
    if (ctx.Absorbed)
      return; // absorbed in place — no attach, no push

    if (ctx.Created is { } created)
      foreach ((Location? parent, Location node) in created)
        router.Model.Attach(parent, node);
    router.Model.Commit(ctx.Resolved);
  }

  /// <summary>The traverse commit — the cursor moves back or forward; the chain itself is already retained.</summary>
  private void ApplyTraverse(TransactionContext ctx)
  {
    if (ctx.Direction == RoutingDirection.Back)
      router.Model.MoveBack();
    else
      router.Model.MoveForward();
  }

  /// <summary>The jump commit — the visited entry is pushed as the current chain.</summary>
  private void ApplyJump(TransactionContext ctx) => router.Model.Commit(ctx.Resolved);

  /// <summary>The refresh commit — nothing moves: the re-armed chain replays its arrival in the convergence.</summary>
  private void ApplyRefresh(TransactionContext ctx)
  {
  }

  /// <summary>The close commit — nothing moves: the presented chain departs as a whole and the close drain releases its members.</summary>
  private void ApplyClose(TransactionContext ctx)
  {
  }
}
