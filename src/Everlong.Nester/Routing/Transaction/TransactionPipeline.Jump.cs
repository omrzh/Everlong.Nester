namespace Everlong.Nester.Routing;

partial class TransactionPipeline
{
  /// <summary>
  ///   Fills the jump decision — a jump records a fresh visit to an existing
  ///   engagement: the live entry whose chain's terminal is the requested site
  ///   lands again (a push of the held chain, no resolution).  A jump to the
  ///   current engagement records nothing; a site no live entry presents
  ///   settles the jump unanswered.
  /// </summary>
  private void FillJump(TransactionContext ctx, TransactNext next)
  {
    if (ctx.Site is not { } site)
    {
      ctx.Result.TrySetResult(false);
      return;
    }

    Location[] current = router.Model.CurrentChain;
    if (current is { Length: > 0 } && ReferenceEquals(current[^1], site))
    {
      // A visit to where one stands records nothing — the engagement is
      // already current.
      ctx.Result.TrySetResult(true);
      return;
    }

    Location[]? entry = router.Model.FindEntryByTerminal(site);
    if (entry is null)
    {
      // No live entry presents the site — the engagement is gone; the
      // caller rebuilds it by description.
      ctx.Result.TrySetResult(false);
      return;
    }

    Location[] chain = [.. entry];
    int from = TransactionContext.SharedPrefix(current, chain);
    ctx.Land(chain, current, from, from);
    next(ctx);
  }
}
