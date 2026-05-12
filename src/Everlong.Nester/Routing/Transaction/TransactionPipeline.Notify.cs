namespace Everlong.Nester.Routing;

partial class TransactionPipeline
{
  /// <summary>
  ///   The pre-commit notifications — the membership edges, then the
  ///   body-change subscribers.  A throwing notifier aborts the transaction:
  ///   the commit never runs.
  /// </summary>
  private void Notify(TransactionContext ctx, TransactNext next)
  {
    // The request-scoped capabilities, mounted before any callback runs.
    if (router.Role == RouterRole.Derived)
      ctx.Features.Set<IRouterCompletion>(router.Completion!);

    // Edge — membership flips derive from the diff: the current chain
    // against the resolved one, leaving innermost first and entering
    // outermost first below the shared prefix.  A terminal convergence
    // (Close) fires no edges — the chain dies as a whole and the drain
    // releases its members; only navigation decisions flip membership.
    if (ctx.Direction != RoutingDirection.Close)
    {
      Location[] current = router.Model.CurrentChain;
      int shared = TransactionContext.SharedPrefix(current, ctx.Resolved);
      for (int i = current.Length - 1; i >= shared; i--)
        if (current[i].Instance is IRoutable routableFrom)
          routableFrom.OnRoutedFrom(ctx.RoutingContext);

      for (int i = shared; i < ctx.Resolved.Length; i++)
        if (ctx.Resolved[i].Instance is IRoutable routableTo)
          routableTo.OnRoutedTo(ctx.RoutingContext);
    }

    // Structure — body-aware participants observe the new chain below them
    // (diff-suppressed: only the participants whose body actually changed).
    RunBodyChanged(ctx);

    next(ctx);
  }

  /// <summary>
  ///   Notifies the body-change subscribers — the participants of the
  ///   resolved chain whose body (the nodes below them) differs from the
  ///   previously published chain.
  /// </summary>
  private void RunBodyChanged(TransactionContext context)
  {
    Location[] chain = context.Resolved;
    Location[] prev = router.Model.CurrentChain;
    foreach (Location node in chain)
    {
      if (node.Instance is not IBodyChanged changed)
        continue;

      object[] fresh = Subchain(chain, node);
      object[] old = Subchain(prev, node);
      if (!ContainsInstance(prev, node) || !ChainsEqual(old, fresh))
        changed.OnBodyChanged(fresh);
    }
  }

  private static bool ContainsInstance(Location[] chain, Location node)
  {
    for (int i = 0; i < chain.Length; i++)
    {
      if (ReferenceEquals(chain[i].Instance, node.Instance))
        return true;
    }

    return false;
  }

  private static object[] Subchain(Location[] chain, Location node)
  {
    for (int i = 0; i < chain.Length; i++)
    {
      if (ReferenceEquals(chain[i].Instance, node.Instance))
      {
        var body = new object[chain.Length - i - 1];
        for (int j = i + 1; j < chain.Length; j++)
          body[j - i - 1] = chain[j].Instance;
        return body;
      }
    }

    return [];
  }

  private static bool ChainsEqual(object[] a, object[] b)
  {
    if (a.Length != b.Length)
      return false;
    for (int i = 0; i < a.Length; i++)
    {
      if (!ReferenceEquals(a[i], b[i]))
        return false;
    }

    return true;
  }
}
