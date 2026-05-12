namespace Everlong.Nester.Routing;

partial class TransactionPipeline
{
  /// <summary>
  ///   The delivery — each settled participant (a fresh parameterized node or
  ///   an absorbed revision) adopts the requested arguments, and its
  ///   engagement is read back into the node's identity.  A throwing delivery
  ///   aborts the transaction — the commit never runs.
  /// </summary>
  private void Deliver(TransactionContext ctx, TransactNext next)
  {
    if (ctx.Settled is { } settled)
    {
      foreach (var (node, requested) in settled)
      {
        if (node.Instance is not IParameterized parameterized)
          continue;
        parameterized.DeliverArgs(requested);
        node.Args = parameterized.EngagedArgs ?? requested;
      }
    }

    next(ctx);
  }
}
