using Everlong.Nester.Presentation;
using Everlong.Nester.Threading;

namespace Everlong.Nester.Routing;

/// <summary>
///   The convergence pipe — the run's supervision, the subclass observation
///   hook, the run scope, the convergence phases (assemble, gate, reveal,
///   completion) and the release-drain endpoint.  A landed transaction's
///   convergence runs the whole pipe.
/// </summary>
internal class ConvergencePipeline
{
  private readonly RouterBase router;
  private readonly IRoutingView view;
  private readonly ConvergeNext pipe;

  /// <summary>Folds the pipe — the observation hook wraps the whole convergence, over the release-drain endpoint.</summary>
  /// <param name="router">The router the stages act on.</param>
  internal ConvergencePipeline(RouterBase router)
  {
    this.router = router;
    view = router.View;

    // The observation hook wraps the whole convergence: the run's
    // supervision, the run scope, the convergence phases (assemble, gate,
    // reveal, completion) and the release-drain endpoint.
    pipe = Fold([Ensure, router.InvokeOnConvergenceAsync, Scope, Assemble, Gate, Reveal, Complete], Release);
  }

  /// <summary>Runs the convergence behind a landed transaction.</summary>
  internal Task Launch(IConvergenceContext context) => pipe(context);

  /// <summary>Links the stages over the endpoint — each stage's continuation is the rest of the pipe, built once at fold time.</summary>
  internal static ConvergeNext Fold(IReadOnlyList<ConvergeStage> stages, ConvergeNext terminal)
  {
    ConvergeNext rest = terminal;
    for (int i = stages.Count - 1; i >= 0; i--)
    {
      ConvergeStage stage = stages[i];
      ConvergeNext inner = rest;
      rest = ctx => stage(ctx, inner);
    }
    return rest;
  }

  /// <summary>
  ///   The run's supervision ensure — it opens the run as the pipe launches
  ///   and closes it when the pipe settles.  A cancellation of this run's own
  ///   lifetime token is silent; any other failure is reported through the
  ///   error channel.
  /// </summary>
  private async Task Ensure(IConvergenceContext ctx, ConvergeNext next)
  {
    router.Runs.Open();
    try
    {
      await next(ctx);
    }
    catch (OperationCanceledException) when (ctx.Lifetime.IsCancellationRequested)
    {
      // superseded mid-observation — benign
    }
    catch (Exception e)
    {
      router.ErrorReporter.ReportError(e);
    }
    finally
    {
      router.Runs.Close(ctx);
    }
  }

  /// <summary>
  ///   The run scope — the model marks the run in flight while the
  ///   convergence phases run, so trimmed orphans wait for the end-of-run
  ///   drain.
  /// </summary>
  private async Task Scope(IConvergenceContext ctx, ConvergeNext next)
  {
    router.Model.BeginConvergenceRun();
    try
    {
      await next(ctx);
    }
    finally
    {
      router.Model.EndConvergenceRun();
    }
  }

  /// <summary>
  ///   The assemble phase — the view fills the resolved chain's view slots.
  ///   Sync and idempotent; a failure is reported and the convergence
  ///   continues.
  /// </summary>
  private Task Assemble(IConvergenceContext ctx, ConvergeNext next)
  {
    try
    {
      view.AssembleViews(ctx.Chain);
    }
    catch (Exception e)
    {
      router.ErrorReporter.ReportError(e);
    }

    return !ctx.Lifetime.IsCancellationRequested ? next(ctx) : Task.CompletedTask;
  }

  /// <summary>
  ///   The gate phase — the leaving side departs (innermost first), then the
  ///   entering side runs its arrival gate (outermost first).  Hook failures
  ///   collect and report at the phase's end; a cut run reports what it
  ///   collected and stops.
  /// </summary>
  private async Task Gate(IConvergenceContext ctx, ConvergeNext next)
  {
    var failures = new List<Exception>();

    // The departure pair belongs to navigation evictions — a terminal
    // convergence (Close) runs none: the chain dies as a whole and the
    // release drain closes its members out.  A position that is also arriving
    // is re-engaged in place: it never left, so it takes no departure hook.
    if (ctx.Direction != RoutingDirection.Close)
    {
      for (int i = ctx.DepartingNodes.Count - 1; i >= 0; i--)
      {
        if (Reengaged(ctx.DepartingNodes[i], ctx))
          continue;
        LifecycleHelper.RunDeparting(ctx.DepartingNodes[i], ctx, failures);
      }
    }

    foreach (Location node in ctx.ArrivingNodes)
    {
      if (ctx.Lifetime.IsCancellationRequested)
      {
        router.ReportFailures(failures);
        return;
      }

      await LifecycleHelper.RunArrivingAsync(node, ctx, failures);
    }

    router.ReportFailures(failures);
    if (!ctx.Lifetime.IsCancellationRequested)
      await next(ctx);
  }

  /// <summary>Whether a departing position is also arriving — the same node, re-engaged in place.</summary>
  private static bool Reengaged(Location node, IConvergenceContext ctx)
  {
    IReadOnlyList<Location> arriving = ctx.ArrivingNodes;
    for (int i = 0; i < arriving.Count; i++)
    {
      if (ReferenceEquals(arriving[i], node))
        return true;
    }

    return false;
  }

  /// <summary>
  ///   The reveal phase — the chain is published to the model and the view's
  ///   visual switch runs.  Reconcile-first: a superseded reveal must leave
  ///   the end state to the superseding run.
  /// </summary>
  private async Task Reveal(IConvergenceContext ctx, ConvergeNext next)
  {
    try
    {
      // Publishes the resolved chain's content target to the view.
      view.SetLocation(ctx.Chain.Count > 0 ? ctx.Chain[^1] : null);
      await view.RevealAsync(ctx);
    }
    catch (OperationCanceledException) when (ctx.Lifetime.IsCancellationRequested)
    {
      return;
    }
    catch (Exception e)
    {
      router.ErrorReporter.ReportError(e);
    }

    if (!ctx.Lifetime.IsCancellationRequested)
      await next(ctx);
  }

  /// <summary>
  ///   The completion phase — the departure notices and the arrival
  ///   completions, innermost-first on the leaving side (mirroring the gate
  ///   order), outermost-first on the convergence subjects.  A cut run leaves
  ///   the unreached nodes unarrived — their next entry simply runs the
  ///   convergence again.
  /// </summary>
  private async Task Complete(IConvergenceContext ctx, ConvergeNext next)
  {
    var failures = new List<Exception>();

    // Mirror of the gate: the departure notices belong to navigation
    // evictions; a terminal convergence (Close) runs none — the chain dies
    // as a whole and the release drain closes its members out.
    if (ctx.Direction != RoutingDirection.Close)
    {
      for (int i = ctx.DepartingNodes.Count - 1; i >= 0; i--)
      {
        if (Reengaged(ctx.DepartingNodes[i], ctx))
          continue;
        LifecycleHelper.RunDeparted(ctx.DepartingNodes[i], ctx, failures);
      }
    }

    foreach (Location node in ctx.ArrivingNodes)
    {
      if (ctx.Lifetime.IsCancellationRequested)
      {
        router.ReportFailures(failures);
        return;
      }

      await LifecycleHelper.RunArrivedAsync(node, ctx, failures);
    }

    router.ReportFailures(failures);
    if (!ctx.Lifetime.IsCancellationRequested)
      await next(ctx);
  }

  /// <summary>
  ///   The endpoint — the release drain: every path that ends a presentation
  ///   settles here.  A bound main-thread dispatcher is required — without
  ///   one the drains run on the caller's thread.
  /// </summary>
  private Task Release(IConvergenceContext ctx)
  {
    if (ctx.Direction == RoutingDirection.Close)
      ReleaseAll();
    else
      ReleaseOrphans();
    return Task.CompletedTask;
  }

  /// <summary>
  ///   Drains the release ledger — a ledgered node whose instance holds no
  ///   live entry left runs the release sequence and detaches from the tree;
  ///   an instance still held elsewhere defers its nodes.  The common
  ///   drain — an empty ledger — settles immediately.  The released views
  ///   remove; failures report through the error channel.
  /// </summary>
  internal void ReleaseOrphans()
  {
    VerifyMainThread();

    var model = router.Model;
    if (model.ReleaseLedger.Count == 0)
      return;

    var pins = model.InstancePins;
    var failures = new List<Exception>();
    var released = new List<Location>();
    foreach (var node in model.ReleaseLedger)
    {
      if (pins.TryGetValue(node.Instance, out var held) && held > 0)
        continue; // another node still holds the instance — deferred
      released.Add(node);
      RunRelease(node, failures);
      model.Detach(node);
    }

    foreach (var node in released)
      model.ReleaseLedger.Remove(node);

    router.ReportFailures(failures);
    RemoveViews(released);
  }

  /// <summary>
  ///   The close drain — releases every held member: the entries' nodes
  ///   and the ledger's evicted orphans, instance-deduplicated; the
  ///   released views remove.  Failures report through the error channel.
  /// </summary>
  internal void ReleaseAll()
  {
    VerifyMainThread();

    var model = router.Model;
    var released = new List<Location>();
    var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
    var failures = new List<Exception>();
    var entries = model.Entries;
    for (int entry = entries.Count - 1; entry >= 0; entry--)
    {
      Location[] chain = entries[entry];
      for (int i = chain.Length - 1; i >= 0; i--)
      {
        Location node = chain[i];
        if (!visited.Add(node.Instance))
          continue;
        released.Add(node);
        RunRelease(node, failures);
      }
    }

    // The ledger's orphans no entry pins — their release tail runs here
    // (they departed when they left); the shared ones deduplicate away.
    foreach (var node in model.ReleaseLedger)
    {
      if (!visited.Add(node.Instance))
        continue;
      released.Add(node);
      RunRelease(node, failures);
    }
    model.ReleaseLedger.Clear();

    router.ReportFailures(failures);
    RemoveViews(released);
  }

  /// <summary>
  ///   The sync teardown's release sequence — every held member releases
  ///   under the transaction window (a terminal teardown runs no departure
  ///   hooks — the router is dying, not navigating; the release tail is
  ///   the whole convergence); the ledger's evicted orphans release
  ///   alongside, instance-deduplicated.  Trims are declined while it
  ///   runs.
  /// </summary>
  internal void ReleaseDeparting()
  {
    VerifyMainThread();

    var model = router.Model;
    model.BeginTransactionWindow();
    try
    {
      var failures = new List<Exception>();
      var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
      var entries = model.Entries;
      for (int entry = entries.Count - 1; entry >= 0; entry--)
      {
        var chain = entries[entry];
        for (int i = chain.Length - 1; i >= 0; i--)
        {
          var node = chain[i];
          if (!visited.Add(node.Instance))
            continue;
          RunRelease(node, failures);
        }
      }

      foreach (var node in model.ReleaseLedger)
      {
        if (!visited.Add(node.Instance))
          continue;
        RunRelease(node, failures);
      }
      model.ReleaseLedger.Clear();

      router.ReportFailures(failures);
    }
    finally
    {
      model.EndTransactionWindow();
    }
  }

  /// <summary>Demands the main thread when a dispatcher is bound — without one the caller's thread stands.</summary>
  private static void VerifyMainThread()
  {
    if (MainDispatcher.TryGet(out var dispatcher))
      dispatcher.VerifyAccess();
  }

  private void RemoveViews(List<Location> released)
  {
    if (released.Count == 0)
      return;
    try
    {
      view.ReleaseViews(released);
    }
    catch (Exception e)
    {
      router.ReportFailures([e]);
    }
  }

  /// <summary>Runs the release sequence on the node's instance — isolated from the drain that collected it.</summary>
  private static void RunRelease(Location node, List<Exception> failures)
  {
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
}
