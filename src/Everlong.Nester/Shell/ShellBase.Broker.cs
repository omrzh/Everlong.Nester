using Everlong.Nester.Intent;
using Everlong.Nester.Layer;

namespace Everlong.Nester.Shell;

partial class ShellBase : ILayerBroker, ILayerLedger
{
  // ── Layer face ── ILayerBroker grants leases; ILayerLedger is the
  //    lease-facing channel (liveness and removal).  The platform supplies
  //    the lease factory and the stage; the stage is platform-internal and
  //    ConnectLedger hands it to the host at start.

  private readonly List<LeaseEntry> _entries = [];
  private ILayerStage? _stage;

  private sealed record LeaseEntry(ILayerLease Lease, ILayerTenant Tenant);

  /// <summary>
  ///   The lease-construction contract: the platform builds its lease
  ///   implementation over the ledger channel at the granted z.
  /// </summary>
  protected abstract ILayerLease CreateLease(ILayerLedger ledger, int z);

  /// <summary>
  ///   Connects the ledger to the stage: unmounts the live leases from a
  ///   previous stage and mounts them onto the new one.  Idempotent for the
  ///   same stage.
  /// </summary>
  protected void ConnectLedger(ILayerStage? stage)
  {
    if (ReferenceEquals(_stage, stage))
      return;
    if (_stage is not null)
    {
      foreach (var entry in _entries)
        _stage.UnmountLease(entry.Lease);
    }

    _stage = stage;
    if (stage is null)
      return;
    foreach (var entry in _entries)
      stage.MountLease(entry.Lease);
  }

  /// <inheritdoc />
  public ILayerLease Acquire(ILayerTenant tenant, object content, LayerBand band, LayerPolicy policy)
    => AcquireAt(tenant, content, ResolveZ(band, policy));

  /// <inheritdoc />
  public ILayerLease Acquire(ILayerTenant tenant, object content, int z)
    => AcquireAt(tenant, content, z);

  /// <summary>Grants a lease at the resolved z: content first, then mount, then record — a failed mount leaks no entry.</summary>
  private ILayerLease AcquireAt(ILayerTenant tenant, object content, int z)
  {
    if (_lifetime.Lifecycle == ShellLifecycle.Disposed)
      throw new InvalidOperationException("The shell is disposed — it grants no leases.");

    ILayerLease lease = CreateLease(this, z);
    lease.Content = content;
    _stage?.MountLease(lease);
    _entries.Add(new LeaseEntry(lease, tenant));
    return lease;
  }

  /// <inheritdoc />
  bool ILayerLedger.IsLive(ILayerLease lease)
    => _entries.Any(e => ReferenceEquals(e.Lease, lease));

  /// <inheritdoc />
  void ILayerLedger.Drop(ILayerLease lease) => DropLease(lease);

  /// <summary>Resolves the granted z: the closest position to the policy that stays inside the band.</summary>
  private int ResolveZ(LayerBand band, LayerPolicy policy)
  {
    if (policy == LayerPolicy.Ceiling)
      return band.Ceiling;
    if (policy != LayerPolicy.AboveHighest)
      return band.Floor;

    int top = band.Floor;
    bool any = false;
    foreach (var entry in _entries)
    {
      int z = entry.Lease.Z;
      if (!band.Contains(z))
        continue;
      if (!any || z > top)
      {
        top = z;
        any = true;
      }
    }

    if (!any)
      return band.Floor;
    // A full band degrades by sharing the ceiling (the later acquisition
    // sits above at equal z).
    return top >= band.Ceiling ? band.Ceiling : top + 1;
  }

  /// <summary>Evicts every live lease, topmost first; a failing tenant callback is reported and the cascade continues.</summary>
  private async ValueTask EvictAllAsync()
  {
    foreach (var entry in BottomUp().ToList())
    {
      if (RemoveEntry(entry.Lease) is null)
        continue;
      _stage?.UnmountLease(entry.Lease);
      try
      {
        await entry.Tenant.OnEvictedAsync(entry.Lease);
      }
      catch (Exception e)
      {
        // A broken tenant must not abort the cascade — and the error path
        // itself may fault (no error handler bound), so it is guarded too.
        try
        {
          ReportError(e);
        }
        catch
        {
          // the cascade owns teardown
        }
      }
    }
  }

  /// <summary>Ends a lease (tenant-initiated): removed, slot unmounted, no notice.</summary>
  private void DropLease(ILayerLease lease)
  {
    if (RemoveEntry(lease) is not null)
      _stage?.UnmountLease(lease);
  }

  private LeaseEntry? RemoveEntry(ILayerLease lease)
  {
    var entry = _entries.FirstOrDefault(e => ReferenceEquals(e.Lease, lease));
    if (entry is null)
      return null;
    _entries.Remove(entry);
    return entry;
  }

  /// <summary>The intent dispatch order: z desc, latest grant first within a z.</summary>
  private IEnumerable<LeaseEntry> BottomUp()
    => _entries.Select((e, index) => (e, index))
      .OrderByDescending(x => x.e.Lease.Z)
      .ThenByDescending(x => x.index)
      .Select(x => x.e);

  /// <summary>Live leases in intent-dispatch order.</summary>
  internal IEnumerable<ILayerLease> LeaseOrder() => BottomUp().Select(e => e.Lease);

  /// <summary>Intent dispatch over the live leases (z desc, latest grant first within a z) — each lease's intent handler gets first refusal.</summary>
  protected async ValueTask<bool> TryDispatchToLayers(IntentContext context)
  {
    var handlers = new List<IIntentHandler>();
    foreach (var entry in BottomUp())
      handlers.Add(entry.Lease.IntentHandler);
    IntentDelegate? pipeline = handlers.Count > 0 ? IntentPipelineHelper.Build(handlers) : null;
    if (pipeline is null)
      return false;
    await pipeline(context);
    return context.IsTerminated;
  }

  /// <summary>Lease-only dispatch (bypasses the Director/host fallbacks) — the layer broker's own channel.</summary>
  internal ValueTask<bool> DispatchToLayerLeases(object? sender, IIntent intent)
    => TryDispatchToLayers(new IntentContext(intent, sender));

  // ── Intent dispatch ────────────────────────────

  private IntentDelegate? _shellPipeline;

  /// <summary>Folds the shell's intent stages — layers, Director, host, fallback — into one pipeline (an absent host substitutes a pass-through).</summary>
  private void BuildShellPipeline()
  {
    var stages = new List<Func<IntentContext, IntentDelegate, ValueTask>>
    {
      RunLayersStage,
      RunDirectorStage,
      RunHostStage,
      RunFallbackStage,
    };
    _shellPipeline = IntentPipelineHelper.Fold(stages);
  }

  private async ValueTask RunLayersStage(IntentContext ctx, IntentDelegate next)
  {
    try
    {
      await TryDispatchToLayers(ctx);
    }
    catch (Exception e)
    {
      ReportError(e);
    }

    if (!ctx.IsTerminated)
      await next(ctx);
  }

  private async ValueTask RunDirectorStage(IntentContext ctx, IntentDelegate next)
  {
    try
    {
      await Director!.HandleAsync(ctx, next);
    }
    catch (Exception e)
    {
      // A Director that does not guard its own handling answers through its
      // error handler: accepted → the dispatch settles as Pass; declined →
      // the exception faults the dispatch caller.
      if (!Director!.HandleError(e))
        throw;
    }
  }

  private ValueTask RunHostStage(IntentContext ctx,
                                 IntentDelegate next)
  {
    if (HostHandler == null)
    {
      return next(ctx);
    }
    return HostHandler.HandleAsync(ctx, next);
  }

  private ValueTask RunFallbackStage(IntentContext ctx, IntentDelegate next)
    => HandleFallbackIntentAsync(ctx, next);

  /// <inheritdoc/>
  public async ValueTask<IntentResult> DispatchIntent(object? sender, IIntent intent)
  {
    // An inactive shell must not answer intents: before the pipeline
    // activation (OnAssembled — the pre-flight assembly window) and after
    // disposal (the window-close re-entry path: OnClosing → TryCloseIntent →
    // DisposeAsync → window.Close → OnClosing) dispatches pass — the second
    // pass falls through immediately.
    if (_lifetime.Lifecycle != ShellLifecycle.Started || _shellPipeline is not { } pipeline)
      return IntentResult.Pass;

    var ctx = new IntentContext(intent, sender);
    await pipeline(ctx);
    return ctx.Result;
  }
}
