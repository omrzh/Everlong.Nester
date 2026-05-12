namespace Everlong.Nester.Routing;

partial class TransactionPipeline
{
  /// <summary>Fills the route decision and runs the rest of the pipe when something landed.</summary>
  private void FillRoute(TransactionContext ctx, TransactNext next)
  {
    IReadOnlyList<ITarget> route = ctx.ExpandedLocator ?? throw new InvalidOperationException("The route direction requires a request.");
    Location[]? onScreen = router.Model.CurrentChain is { Length: > 0 } content ? content : null;

    // ① the absorption — every position served by the current node,
    //    revised arguments adapted on exclusively-held adaptive nodes; a
    //    rigid node ignores the requested arguments.
    var revisions = ScanAbsorption(onScreen, route, out int from);
    if (revisions is { Count: 0 })
      return; // an equal route — nothing to land

    if (revisions is not null && RevisionsExclusive(revisions))
    {
      // All accepted — the absorption lands in place: no push, no membership
      // edges, the moving side below the first difference replays its arrival,
      // and the delivery settles the revised engagements before the
      // notifications.
      ctx.Absorbed = true;
      ctx.Settled = revisions;
      ctx.Land(onScreen!, onScreen!, from, from);
    }
    else
    {
      // ② the structural walk — reuse carries equal arguments only, so
      //    any revision materializes a fresh node (adopted at resolution).
      FillStructural(ctx, route, onScreen);
    }

    next(ctx);
  }

  /// <summary>
  ///   Scans the absorption — every position must be served by the current
  ///   node.  Revised arguments on an adaptive node become a revision
  ///   candidate; on a non-adaptive parameterized node they force the
  ///   structural walk (a fresh node); a rigid node ignores them.  Returns
  ///   <see langword="null" /> when the presented chain does not serve the
  ///   route (the structural walk fills); an empty list when the route
  ///   equals the current engagement (nothing lands); the revision
  ///   candidates otherwise.  <paramref name="from" /> is the arriving
  ///   side's first position — the first revised node, or the chain's end
  ///   when nothing was revised.
  /// </summary>
  private static List<(Location Node, IArgs? Requested)>? ScanAbsorption(Location[]? onScreen, IReadOnlyList<ITarget> route, out int from)
  {
    from = 0;
    if (onScreen is not { Length: > 0 } || onScreen.Length != route.Count)
      return null;

    var revisions = new List<(Location Node, IArgs? Requested)>();
    bool differs = false;
    for (int i = 0; i < route.Count; i++)
    {
      ITarget spec = route[i];
      Location current = onScreen[i];
      if (current.Type != spec.Type
          || (spec.Instance is not null && !ReferenceEquals(spec.Instance, current.Instance)))
        return null;

      if (current.Instance is IParameterized && !Equals(current.Args, spec.Args))
      {
        // A non-adaptive node rebuilds on revised arguments; an adaptive one
        // is asked once, here, and a refusal rebuilds the same way.
        if (current.Instance is not IAdaptiveParameterized adaptive || !adaptive.IsAdaptable(spec.Args))
          return null;
        revisions.Add((current, spec.Args));
        if (!differs)
        {
          differs = true;
          from = i;
        }
      }
    }

    if (!differs)
      from = route.Count;
    return revisions;
  }

  /// <summary>
  ///   Fills the structural decision — the resolved chain against the
  ///   presented one, a landed push.
  /// </summary>
  private void FillStructural(TransactionContext ctx, IReadOnlyList<ITarget> route, Location[]? onScreen)
  {
    var (resolved, created) = ResolveChain(ctx, route);

    // The commit attaches the fresh nodes and pushes the chain — the
    // decision's own record, not the fill-side capture.  A reused prefix is
    // the same node by reference, so the moving sides start at the first
    // miss.
    int from = TransactionContext.SharedPrefix(onScreen, resolved);
    ctx.Land(resolved, onScreen ?? [], from, from);
    ctx.Created = created;
  }

  /// <summary>
  ///   Resolves the chain position by position — a tree child serving the
  ///   spec is reused; a miss materializes a fresh participant.  Returns
  ///   the resolved chain and the created nodes with their parents (the
  ///   tree attach runs at the commit).
  /// </summary>
  private (Location[] Resolved, List<(Location? Parent, Location Node)> Created) ResolveChain(TransactionContext ctx, IReadOnlyList<ITarget> route)
  {
    var resolved = new Location[route.Count];
    var created = new List<(Location? Parent, Location Node)>();
    Location? parent = null;
    for (int i = 0; i < route.Count; i++)
    {
      ITarget spec = route[i];

      // A held child serving the spec is reused; a miss materializes a
      // fresh node, whose children are empty — every deeper position misses
      // too, so the tail is all fresh.
      var hit = router.Model.Match(spec, parent);
      if (hit is not null)
      {
        resolved[i] = hit;
        parent = hit;
        continue;
      }

      var node = Materialize(spec);
      resolved[i] = node;
      created.Add((parent, node));
      // A materialized node that never reaches the commit is abandoned by
      // the transaction — the ensure records it so the close-out releases it.
      ctx.Materialized.Add(node);
      // A parameterized participant is delivered by the Deliver stage — its
      // engagement is read back into the node's identity then.
      if (node.Instance is IParameterized)
        (ctx.Settled ??= []).Add((node, spec.Args));
      parent = node;
    }

    return (resolved, created);
  }

  /// <summary>
  ///   Materializes a fresh participant — the router resolves the instance
  ///   (the request's own, then the container, then the subclass resolve
  ///   seam) and runs the member injection.  No delivery: the Deliver
  ///   stage adopts the engagement and reads it back into the node's args.
  /// </summary>
  private Location Materialize(ITarget spec)
  {
    var instance = spec.Instance ?? router.ResolveInstance(spec.Type);
    router.Inject(instance);
    return router.Model.CreateLocation(spec.Type, instance, null);
  }

  /// <summary>
  ///   Whether every revised node is held by the current entry alone —
  ///   revising a shared node would rewrite another entry's identity.  The
  ///   incremental retention count answers in O(revisions): the current
  ///   entry holds each revised instance once, so a count above one means
  ///   another entry shares it.
  /// </summary>
  private bool RevisionsExclusive(List<(Location Node, IArgs? Requested)> revisions)
  {
    var pins = router.Model.InstancePins;
    foreach (var (node, _) in revisions)
      if (pins.TryGetValue(node.Instance, out var count) && count > 1)
        return false;
    return true;
  }

}
