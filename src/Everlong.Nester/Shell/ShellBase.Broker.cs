using Everlong.Nester.Intent;
using Everlong.Nester.Layer;

namespace Everlong.Nester.Shell;

partial class ShellBase : ILayerBroker, ILayerLedger
{
  // ── Layer face ── ILayerBroker grants leases and owns layer focus;
  //    ILayerLedger is the handle-facing channel (liveness and removal).  The
  //    platform supplies the handle factory and the stage; the stage is
  //    platform-internal and ConnectLedger hands it to the host at start.

  private readonly List<LeaseEntry> _entries = [];
  private ILayerStage? _stage;
  private ILayerLease? _focused;
  private bool _electing;
  private bool _focusDirty;

  private sealed record LeaseEntry(ILayerHandle Handle, ILayerTenant Tenant);

  /// <summary>
  ///   The handle-construction contract: the platform builds its handle
  ///   implementation over the ledger channel, holding the granted content
  ///   at the granted plane and z.
  /// </summary>
  protected abstract ILayerHandle CreateHandle(ILayerLedger ledger, object content, LayerPlane plane, int z);

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
        _stage.UnmountLease(entry.Handle);
    }

    _stage = stage;
    if (stage is null)
      return;
    foreach (var entry in _entries)
      stage.MountLease(entry.Handle);
  }

  /// <inheritdoc />
  public ILayerHandle Acquire(ILayerTenant tenant, object content, LayerPlane plane)
    => AcquireAt(tenant, content, plane, ResolveZ(plane));

  /// <inheritdoc />
  public ILayerLease? Focused => _focused;

  /// <inheritdoc />
  public void RequestFocus(ILayerHandle handle)
  {
    ILayerLease lease = handle.Lease;
    if (!IsLive(lease) || lease.Content is not IFocusableContent)
      return;
    ApplyFocus(lease, LayerFocusCause.Requested);
  }

  /// <summary>Grants a handle at the resolved z, content included: mount, then record — a failed mount leaks no entry.</summary>
  internal ILayerHandle AcquireAt(ILayerTenant tenant, object content, LayerPlane plane, int z)
  {
    if (_lifetime.Lifecycle == ShellLifecycle.Disposed)
      throw new InvalidOperationException("The shell is disposed — it grants no leases.");

    ILayerHandle handle = CreateHandle(this, content, plane, z);
    _stage?.MountLease(handle);
    _entries.Add(new LeaseEntry(handle, tenant));
    ReelectFocus(LayerFocusCause.Granted);
    return handle;
  }

  /// <inheritdoc />
  bool ILayerLedger.IsLive(ILayerLease lease)
    => IsLive(lease);

  /// <summary>Whether <paramref name="lease" /> is still recorded.</summary>
  private bool IsLive(ILayerLease lease)
    => _entries.Any(e => ReferenceEquals(e.Handle.Lease, lease));

  /// <inheritdoc />
  void ILayerLedger.Drop(ILayerLease lease) => DropLease(lease);

  /// <summary>Resolves the granted z: the plane's floor, or one above the plane's highest live z when the plane stacks.</summary>
  private int ResolveZ(LayerPlane plane)
  {
    LayerRange range = LayerPlanes.Range(plane);
    if (!LayerPlanes.Stacks(plane))
      return range.Floor;

    int top = range.Floor;
    bool any = false;
    foreach (var entry in _entries)
    {
      int z = entry.Handle.Lease.Z;
      if (!range.Contains(z))
        continue;
      if (!any || z > top)
      {
        top = z;
        any = true;
      }
    }

    if (!any)
      return range.Floor;
    // A full plane degrades by sharing the ceiling (the later acquisition
    // sits above at equal z).
    return top >= range.Ceiling ? range.Ceiling : top + 1;
  }

  // ── layer focus — the single foreground grant ──────────────────────────────

  /// <summary>Runs the focus election; a re-entrant call marks the run dirty and the outer loop re-elects.</summary>
  private void ReelectFocus(LayerFocusCause cause)
  {
    if (_electing)
    {
      _focusDirty = true;
      return;
    }

    _electing = true;
    try
    {
      do
      {
        _focusDirty = false;
        ApplyFocus(Elect(cause), cause);
      } while (_focusDirty);
    }
    finally
    {
      _electing = false;
    }
  }

  /// <summary>The election: the highest live content that accepts layer focus, or none.</summary>
  private ILayerLease? Elect(LayerFocusCause cause)
  {
    var context = new LayerFocusContext(cause);
    foreach (var entry in BottomUp())
    {
      if (entry.Handle.Lease.Content is IFocusableContent focusable && focusable.TryFocus(context))
        return entry.Handle.Lease;
    }

    return null;
  }

  /// <summary>Transfers layer focus: the outgoing pre-hook, the incoming pre-hook, the commit, then both post-hooks.</summary>
  private void ApplyFocus(ILayerLease? winner, LayerFocusCause cause)
  {
    if (ReferenceEquals(winner, _focused))
      return;

    var context = new LayerFocusContext(cause);
    ILayerLease? outgoing = _focused;
    IFocusableContent? outgoingFocus = outgoing?.Content as IFocusableContent;
    IFocusableContent? incomingFocus = winner?.Content as IFocusableContent;

    outgoingFocus?.OnUnfocusing(context);
    incomingFocus?.OnFocusing(context);

    _focused = winner;

    outgoingFocus?.OnUnfocused(context);
    incomingFocus?.OnFocused(context);
  }

  /// <summary>Fires the focus-lost pair for a lease that leaves the stack while focused, then clears the holder.</summary>
  private void DepartFocus(ILayerLease lease)
  {
    if (!ReferenceEquals(_focused, lease) || lease.Content is not IFocusableContent focusable)
      return;

    var context = new LayerFocusContext(LayerFocusCause.Departed);
    focusable.OnUnfocusing(context);
    _focused = null;
    focusable.OnUnfocused(context);
  }

  /// <summary>Evicts every live lease, topmost first; a failing tenant callback is reported and the cascade continues.</summary>
  private async ValueTask EvictAllAsync()
  {
    _focused = null;
    foreach (var entry in BottomUp().ToList())
    {
      if (RemoveEntry(entry.Handle.Lease) is null)
        continue;
      _stage?.UnmountLease(entry.Handle);
      try
      {
        await entry.Tenant.OnEvictedAsync(entry.Handle.Lease);
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

  /// <summary>Ends a lease (tenant-initiated): removed, slot unmounted, focus re-elected, no notice.</summary>
  private void DropLease(ILayerLease lease)
  {
    var entry = RemoveEntry(lease);
    if (entry is null)
      return;
    _stage?.UnmountLease(entry.Handle);
    DepartFocus(lease);
    ReelectFocus(LayerFocusCause.Departed);
  }

  private LeaseEntry? RemoveEntry(ILayerLease lease)
  {
    var entry = _entries.FirstOrDefault(e => ReferenceEquals(e.Handle.Lease, lease));
    if (entry is null)
      return null;
    _entries.Remove(entry);
    return entry;
  }

  /// <summary>The intent dispatch order: z desc, latest grant first within a z.</summary>
  private IEnumerable<LeaseEntry> BottomUp()
    => _entries.Select((e, index) => (e, index))
      .OrderByDescending(x => x.e.Handle.Lease.Z)
      .ThenByDescending(x => x.index)
      .Select(x => x.e);

  /// <summary>Live leases in intent-dispatch order.</summary>
  internal IEnumerable<ILayerLease> LeaseOrder() => BottomUp().Select(e => e.Handle.Lease);

  /// <summary>Intent dispatch over the live leases (z desc, latest grant first within a z) — each lease's intent handler gets first refusal.</summary>
  protected async ValueTask<bool> TryDispatchToLayers(IntentContext context)
  {
    var handlers = new List<IIntentHandler>();
    foreach (var entry in BottomUp())
      handlers.Add(entry.Handle.Lease.IntentHandler);
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
    // disposal (the window-close re-entry path: OnClosing → CloseIntent →
    // CloseAsync → window.Close → OnClosing) dispatches pass — the second
    // pass falls through immediately.
    if (_lifetime.Lifecycle != ShellLifecycle.Started || _shellPipeline is not { } pipeline)
      return IntentResult.Pass;

    var ctx = new IntentContext(intent, sender);
    await pipeline(ctx);
    return ctx.Result;
  }
}
