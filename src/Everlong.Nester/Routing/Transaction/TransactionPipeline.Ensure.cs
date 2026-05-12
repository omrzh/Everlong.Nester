namespace Everlong.Nester.Routing;

partial class TransactionPipeline
{
  /// <summary>
  ///   Faults a direction no pipe claims — the throw rides the completion
  ///   guard, so the fault reaches the caller's await instead of escaping the
  ///   pump.
  /// </summary>
  private void InvalidDirection(TransactionContext ctx)
  {
    EnsureTransaction(ctx, static c => throw new InvalidOperationException(
      $"The direction {c.Direction} has no transaction fill."));
  }

  /// <summary>
  ///   The completion ensure — a transaction that faults or short-circuits
  ///   still closes out per the router's role, and a derived router that never
  ///   commits faults its completion.
  /// </summary>
  private void EnsureTransaction(TransactionContext ctx, TransactNext next)
  {
    try
    {
      next(ctx);
    }
    catch (Exception ex)
    {
      // A sync-phase fault — the transaction never committed, nothing runs
      // behind it: the caller's transaction faults.  The nodes it
      // materialized release before the fault leaves.
      ReleaseMaterialized(ctx);
      ctx.Result.TrySetException(ex);
      if (router.Role == RouterRole.Derived)
      {
        // A derived router's fault settles its own completion.
        router.FaultRouterCompletion(ex);
      }

      return;
    }
    finally
    {
      // The pipe's exit — a transaction that never committed cancels the
      // lifetime token it handed out; nothing behind it would.
      router.Runs.Retire(ctx);
    }

    // The close-out ensure — a transaction that settles without a commit
    // (vetoed / nothing landed) completes its caller with null; a derived
    // router whose stack stayed empty settles the result channel.  Nodes it
    // materialized without committing release here.
    if (!ctx.Result.Task.IsCompleted)
    {
      ReleaseMaterialized(ctx);
      ctx.Result.TrySetResult(false);
      if (router is { Role: RouterRole.Derived, Model: { IsClosed: false, CurrentChain.Length: 0 } })
        router.Completion!.Complete(null);
    }
  }

  /// <summary>
  ///   Releases the nodes a transaction materialized without committing — a
  ///   faulted or short-circuited transaction abandons them.  A materialized
  ///   instance a live entry still holds is not abandoned; release failures
  ///   report through the error channel.
  /// </summary>
  private void ReleaseMaterialized(TransactionContext ctx)
  {
    if (!ctx.HasMaterialized)
      return;

    var pins = router.Model.InstancePins;
    var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
    var failures = new List<Exception>();
    foreach (var node in ctx.Materialized)
    {
      if (!visited.Add(node.Instance))
        continue;
      if (pins.TryGetValue(node.Instance, out var held) && held > 0)
        continue; // a live entry still holds the instance — not abandoned
      try
      {
        if (node.Instance is IReleasable releasable)
          releasable.Release();
      }
      catch (Exception e)
      {
        failures.Add(e);
      }
    }

    router.ReportFailures(failures);
  }
}
