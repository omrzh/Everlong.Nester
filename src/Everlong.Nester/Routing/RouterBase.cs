using System.ComponentModel;
using Everlong.DI;
using Everlong.Nester.Diagnostics;
using Everlong.Nester.Intent;
using Everlong.Nester.Layer;
using Everlong.Nester.Presentation;
using Everlong.Nester.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Everlong.Nester.Routing;

/// <summary>
///   The router behind <see cref="IRouter" /> — drives its navigation
///   model, its intake and its supervision state.  The base router
///   navigates the shell's main stack; a derived router (created by
///   <see cref="Derive" />) presents an overlay stack and
///   carries the result channel.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public class RouterBase : IRouter, IIntentHandler, ILayerTenant
{
  /// <inheritdoc />
  public RouterRole Role { get; }

  private readonly ILayerBroker _broker;

  private readonly IServiceScopeFactory _scopeFactory;
  private readonly IServiceScope? _ownScope;

  /// <summary>The router's own scope provider.</summary>
  protected IServiceProvider _services;

  private readonly ILayerLease _lease;
  private readonly ResultChannel? _completion;
  private readonly Location? _borrowed;
  private readonly IIntentDispatcher _dispatcher;
  private readonly bool _ephemeral;

  /// <summary>0 while a one-shot router has not accepted a route yet.</summary>
  private int _accepted;

  // ── the folded pipes — the router's own stage lists, folded once at
  //    construction; each stage owns a file, per-transaction state travels
  //    on the contexts ────────────────────────────────────────────────────────

  private readonly TransactionPipeline _transactPipe;
  private readonly ConvergencePipeline _convergence;

  private readonly Queue<TransactionContext> _pending = new();
  private bool _pumping;

  private readonly RunSupervision _runs;

  private int _closed;

  private bool IsStopRouting => _closed != 0;

  /// <summary>
  ///   Builds the router — its role and lease z come from the scope's seed.
  /// </summary>
  /// <param name="services">The router's own scope provider.</param>
  /// <param name="model">The router's navigation model.</param>
  protected RouterBase(IServiceProvider services, RouterStack model)
  {
    _services = services;
    _broker = services.GetRequiredService<ILayerBroker>();
    ErrorReporter = services.GetRequiredService<IErrorReporter>();
    _runs = new RunSupervision(ReportFailures);
    _dispatcher = services.GetRequiredService<IIntentDispatcher>();
    _scopeFactory = services.GetRequiredService<IServiceScopeFactory>();
    IRouterSeed seed = services.GetRequiredService<IRouterSeed>();
    Role = seed.Role;
    _ownScope = seed.OwnScope;
    _borrowed = seed.Borrowed;
    _ephemeral = seed.IsEphemeral;

    Model = model;
    View.SetRouter(this);

    // The member connection — the router mounts its routing view into
    // its own lease slot.
    _lease = _broker.Acquire(this, View, seed.Band, seed.Policy);
    _lease.IntentHandler = this;

    if (Role == RouterRole.Derived)
    {
      // The modal overlay declares its focus policy on its own content —
      // the stage maps the declared policy and the stack position onto
      // the layer's actual Tab mode.
      if (View is IFocusPolicySurface focusPolicy)
        focusPolicy.FocusPolicy = FocusPolicy.Trapped;
      // The derived router's completion channel — the write end its
      // participants capture and the creator's result surface forwards to.
      _completion = new ResultChannel(this);
    }

    // The router builds its two pipes once — the transaction pipe is a shared
    // prefix, a sub-pipe per direction and a shared suffix; the two subclass
    // hooks sit in the pipes natively, and not calling next short-circuits the
    // rest of the pipe.
    _transactPipe = new TransactionPipeline(this);

    // The convergence pipe — the observation hook wraps the whole convergence:
    // the run's supervision, the run scope, the convergence phases (assemble,
    // gate, reveal, completion) and the release-drain endpoint.  The pipeline
    // instance also serves the transaction-level drains, which settle outside
    // a run.
    _convergence = new ConvergencePipeline(this);

    // A trim feeds the ledger — outside a run the drain settles it now; a
    // run in flight holds it for the run's endpoint.
    Model.TrailTrimmed += DrainOrphansIfIdle;
  }

  /// <summary>The router's navigation model — the base's main stack or a derived router's overlay stack; the role discriminates which.</summary>
  internal RouterStack Model { get; }

  /// <summary>The router's routing view — the surface this router mounts as its lease body.</summary>
  internal IRoutingView View => Model.View;

  /// <summary>The presented site this overlay router borrowed at its derivation, or <see langword="null" /> for the base router.</summary>
  protected internal Location? Borrowed => _borrowed;

  /// <summary>Creates the convergence context of a landed transaction.</summary>
  protected internal virtual IConvergenceContext CreateConvergenceContext(TransactionContext context)
    => new ConvergenceContext(context, _borrowed);

  /// <summary>The error channel the pipes' stages report through.</summary>
  internal IErrorReporter ErrorReporter { get; }

  /// <summary>The router's transaction and convergence-run supervision.</summary>
  internal RunSupervision Runs => _runs;

  /// <inheritdoc />
  public IRouterCompletion? Completion => _completion;

  /// <inheritdoc />
  public IRouterStack Stack => Model;

  // ── ① intake ─────────────────────────────────────────────────────────────────

  /// <inheritdoc />
  public Task RouteAsync(ILocator location)
  {
    if (IsStopRouting)
    {
      // a closed router no longer routes
      _completion?.Complete(null);
      return Task.CompletedTask;
    }

    // A one-shot surface accepts its first request; every later one leaves
    // it as a hand-off instead of stacking on it.
    if (_ephemeral && Interlocked.Exchange(ref _accepted, 1) != 0)
      return HandOffAsync(location);

    var context = new TransactionContext(RoutingDirection.Route, location);

    if (MainDispatcher.TryGet(out var dispatcher) && !dispatcher.CheckAccess())
    {
      // Off the UI thread — bridge the whole route to it: the transaction
      // (chain resolution, commit, convergences) runs on the main thread.
      // The invoke lands the run inline on the dispatcher's side; the
      // caller observes the landing through the transaction's relay.
      dispatcher.InvokeAsync(() => PipeRequest(context));
      return context.Result.Task;
    }

    PipeRequest(context);
    return context.Result.Task;
  }

  /// <inheritdoc />
  public Task<bool> JumpAsync(ILocation site)
  {
    if (IsStopRouting)
      return Task.FromResult(false);

    var context = new TransactionContext(RoutingDirection.Jump, site: site);

    if (MainDispatcher.TryGet(out var dispatcher) && !dispatcher.CheckAccess())
    {
      // Off the UI thread — bridge the whole jump to it: the transaction
      // (commit, convergences) runs on the main thread.
      dispatcher.InvokeAsync(() => PipeRequest(context));
      return context.Result.Task;
    }

    PipeRequest(context);
    return context.Result.Task;
  }

  /// <inheritdoc />
  public IRouter Derive(bool isEphemeral = false)
  {
    IServiceScope scope = _scopeFactory.CreateScope();
    IRouterSeed seed = scope.ServiceProvider.GetRequiredService<IRouterSeed>();
    var band = isEphemeral ? KnownLayers.Dialog : KnownLayers.Navigation;

    seed.Initialize(RouterRole.Derived, band, LayerPolicy.AboveHighest, scope, BorrowedEnvironment(),
                    isEphemeral);

    IRouter wrapped = scope.ServiceProvider.GetRequiredService<IRouter>();
    try
    {
      // A created router is a derived router whose result channel is always
      // present — a registration that yields anything else violates the
      // protocol and explodes at the handing site.
      if (wrapped.Role != RouterRole.Derived || wrapped.Completion is null)
        throw new InvalidOperationException(
          "A created router must be a derived router that provides a result channel.");
      return wrapped;
    }
    catch (Exception)
    {
      scope.Dispose();
      throw;
    }
  }

  /// <summary>
  ///   Re-issues a request this one-shot surface cannot take as the route
  ///   consultation it never answers — the request falls through to a
  ///   navigable surface.
  /// </summary>
  private Task HandOffAsync(ILocator location)
    => _dispatcher.DispatchIntent(this, new RouteIntent(location)).AsTask();

  /// <summary>The presented site a derived router borrows — the presented content at the derivation, or <see langword="null" /> when nothing is presented.</summary>
  private Location? BorrowedEnvironment()
    => Model.CurrentChain is { Length: > 0 } chain ? chain[^1] : null;

  /// <summary>
  ///   Runs a navigation transaction — queued and pumped: its transaction pipe runs
  ///   when the pump reaches it, and only the last queued transaction runs its
  ///   convergence.  The caller observes the landing through the transaction's
  ///   relay (<see cref="TransactionContext.Result" />).
  /// </summary>
  private async void PipeRequest(TransactionContext context)
  {
    // Stamps the router's role fact — a derived router's every transaction is elevated.
    context.IsDerived = Role == RouterRole.Derived;
    _pending.Enqueue(context);
    await Pump();
  }

  /// <summary>
  ///   Pumps the queued transactions — each runs its transaction pipe; a landed transaction
  ///   whose queue is not yet empty is superseded before its convergence
  ///   starts (its evictions stay ledgered and drain with the final run).
  ///   Stops when the queue empties; a new arrival restarts it.
  /// </summary>
  private async Task Pump()
  {
    if (_pumping)
      return;

    _pumping = true;
    try
    {
      while (_pending.Count > 0)
      {
        TransactionContext context = _pending.Dequeue();
        _transactPipe.Handle(context);

        // The pipe's ensure settled the relay: a landed transaction carries its
        // convergence context; a settled-without-landing transaction completed
        // its caller with false and a faulted one faults the caller's await —
        // nothing runs behind either.
        if (context.Convergence is not { } convergence)
          continue;

        if (_pending.Count > 0)
        {
          // A newer transaction is queued — this convergence is superseded before
          // it starts; its run's observation channel and the transfer's lifetime
          // close here.
          _runs.Skip(convergence);
          continue;
        }

        // Only the last queued transaction runs its convergence — launched
        // detached: the pump must keep draining the queue so a newer
        // transaction's commit can supersede (cancel) a stalled run; awaiting
        // the convergence here would deadlock a run that waits on its own
        // lifetime token.
        LaunchConvergence(convergence);
      }
    }
    finally
    {
      _pumping = false;
    }

    // The transaction-level release obligation — a pump that ran no convergence
    // drained nothing (the convergence endpoint drains otherwise); the drain
    // defers while another run is still converging.
    DrainOrphansIfIdle();
  }

  /// <summary>Launches the convergence behind a landed transaction, detached from the pump.</summary>
  // The convergence's own ensure contains its failures — the async-void launch
  // keeps the pipe's task rooted and lets any escape land on the dispatcher's
  // unhandled-exception channel instead of dying as an unobserved task exception.
  private async void LaunchConvergence(IConvergenceContext convergence)
    => await _convergence.Launch(convergence);

  /// <summary>
  ///   The routing pipeline's user stage — rewrites the locator of a
  ///   <see cref="RoutingDirection.Route" /> transaction, or consumes it.
  /// </summary>
  /// <remarks>
  ///   Asked only for <see cref="RoutingDirection.Route" /> — the one
  ///   direction that materializes a chain from a descriptor, so the only one
  ///   with a request to rewrite (<see cref="TransactionContext.UpdateLocator" />)
  ///   or consume.  Calling <paramref name="next" /> runs the transaction;
  ///   returning without it consumes the request, and the pipeline's ensure
  ///   settles a derived router that never committed.
  /// </remarks>
  protected virtual void HandleRouteRequest(TransactionContext ctx, TransactNext next) => next(ctx);

  /// <summary>The pipeline's entry to the route request hook — dispatches to the virtual <see cref="HandleRouteRequest" />, which the pipeline cannot reach directly.</summary>
  internal void InvokeHandleRouteRequest(TransactionContext ctx, TransactNext next) => HandleRouteRequest(ctx, next);

  /// <summary>
  ///   Resolves a chain participant neither the request nor the container
  ///   supplied — the default gives nothing (the framework resolves and
  ///   injects, it never reflects); an override constructs what the
  ///   container does not know.
  /// </summary>
  protected internal virtual object? ResolveParticipant(Type type) => null;

  /// <summary>Resolves a chain participant's instance — the container, then the subclass resolve seam; faults when neither supplies one.</summary>
  internal object ResolveInstance(Type type)
    => _services.GetService(type)
       ?? ResolveParticipant(type)
       ?? throw new InvalidOperationException($"No instance resolved for chain node '{type}'.");

  /// <summary>Runs the member injection — an <see cref="IInjectable" /> instance receives the container's <see cref="IInjector" />; a claim without an injector faults.</summary>
  internal void Inject(object instance)
  {
    if (instance is not IInjectable injectable)
      return;
    if (_services.GetService<IInjector>() is not { } injector)
      throw new InvalidOperationException(
        $"The participant '{instance.GetType()}' implements IInjectable — the container provides no IInjector to honor the protocol.");
    injector.Inject(injectable);
  }

  /// <summary>
  ///   The async pipeline's user stage — the observation surface over the
  ///   committed transaction.  Calling <paramref name="next" /> runs the convergence
  ///   stages; the default passes straight through.  Not calling it skips
  ///   the convergence — an observer that overrides the convergence and refuses
  ///   its own <paramref name="next" /> owns what it skips.
  /// </summary>
  protected virtual Task OnConvergenceAsync(IConvergenceContext ctx, ConvergeNext next) => next(ctx);

  /// <summary>The pipeline's entry to the convergence hook — dispatches to the virtual <see cref="OnConvergenceAsync" />, which the pipeline cannot reach directly.</summary>
  internal Task InvokeOnConvergenceAsync(IConvergenceContext ctx, ConvergeNext next) => OnConvergenceAsync(ctx, next);

  /// <summary>
  ///   Waits until no convergence run is in flight.
  /// </summary>
  /// <remarks>
  ///   The only aggregate awaitable over a detached convergence, the
  ///   <see cref="OnConvergenceAsync" /> hook included — <see cref="RouteAsync" />
  ///   settles at the truth commit.
  /// </remarks>
  internal Task WaitIdleAsync() => _runs.Idle;

  /// <summary>Faults the derived router's result channel — the closing convergence runs, the lease returns, the result faults.</summary>
  internal void FaultRouterCompletion(Exception exception)
  {
    if (Role != RouterRole.Derived || Model.IsClosed)
      return;
    if (Interlocked.CompareExchange(ref _closed, 1, 0) != 0)
      return;
    DepartForTeardown();
    ReturnLeaseAndSettle(() => Model.SettleFault(exception));
  }

  /// <summary>
  ///   The sync teardown — the release sequence, then the result settles.
  ///   No-op on an already-closed model.
  /// </summary>
  internal void CloseSync(object? result)
  {
    if (Model.IsClosed)
      return;
    DepartForTeardown();
    Model.Settle(result);
  }

  /// <summary>The sync release sequence for teardown — a fault or a reclaim; isolated hook failures report through.  A thread-affinity violation escapes it — a caller bug is not a hook failure.</summary>
  private void DepartForTeardown()
  {
    if (MainDispatcher.TryGet(out var dispatcher))
      dispatcher.VerifyAccess();

    // The router is going away: the active transfer's lifetime ends first, so a
    // run still in flight stops at its next gate instead of racing the release sequence.
    _runs.EndActive();

    try
    {
      _convergence.ReleaseDeparting();
    }
    catch (OperationCanceledException)
    {
      // ignored
    }
    catch (Exception e)
    {
      ReportFailures([e]);
    }
  }

  /// <summary>Drains the ledgered orphans outside a run — a pump's transaction-level obligation and a trim's immediate settle; a run in flight holds them for its endpoint.</summary>
  private void DrainOrphansIfIdle()
  {
    if (!Model.IsConvergenceRunning)
      _convergence.ReleaseOrphans();
  }

  /// <summary>
  ///   Starts the derived router's close — the close transaction, its exit
  ///   convergence and the release drain run detached; the result settles
  ///   after the lease return so an awaiting creator observes a free layer
  ///   (completion is observed through the <see cref="Completion" />
  ///   channel).  A close-phase fault reports through the error channel.
  ///   The first entry wins; later entries no-op.
  /// </summary>
  internal void RunDerivedClose(object? result) => _ = CloseCoreAsync(result);

  /// <summary>
  ///   The close core — runs the close transaction through the intake (its
  ///   convergence departs the presented chain, reveals the exit and drains
  ///   every held member), then returns the lease and settles the result.
  /// </summary>
  private async Task CloseCoreAsync(object? result)
  {
    if (Interlocked.CompareExchange(ref _closed, 1, 0) != 0)
      return;

    var context = new TransactionContext(RoutingDirection.Close);
    if (MainDispatcher.TryGet(out var dispatcher) && !dispatcher.CheckAccess())
    {
      // Off the UI thread — bridge the close transaction to it: the convergence and
      // its release drain run on the main thread.
      await dispatcher.InvokeAsync(() => PipeRequest(context));
    }
    else
    {
      PipeRequest(context);
    }

    try
    {
      // The release sequence finishes before the lease returns — the exit reveal
      // runs against the still-attached layer.
      await context.Result.Task;
      if (context.Convergence is { } convergence)
        await convergence.Completion;
    }
    catch (Exception e)
    {
      ErrorReporter.ReportError(e);
    }
    finally
    {
      ReturnLeaseAndSettle(() => Model.Settle(result));
    }
  }

  /// <summary>Returns the lease and disposes the owned scope — the tenant's act, once per router.</summary>
  private void ReturnLease()
  {
    _lease.Release();
    _ownScope?.Dispose();
  }

  /// <summary>
  ///   Returns the lease and settles the layer's result — the settle is
  ///   never lost: a failing lease return (release or scope dispose) is
  ///   reported through the error channel after the result lands.
  /// </summary>
  private void ReturnLeaseAndSettle(Action settle)
  {
    Exception? failure = null;
    try
    {
      ReturnLease();
    }
    catch (Exception e)
    {
      failure = e;
    }

    try
    {
      settle();
    }
    finally
    {
      if (failure is not null)
        ErrorReporter.ReportError(failure);
    }
  }

  ValueTask ILayerTenant.OnEvictedAsync(ILayerLease lease)
  {
    if (Role == RouterRole.Derived)
    {
      _completion?.Complete(null);
      return ValueTask.CompletedTask;
    }

    CloseSync(null);
    return ValueTask.CompletedTask;
  }

  // ── intent handling — the router's own domain commands ───────────────────────

  private Location[]? _nodePipelineChain;

  /// <summary>The presented chain's pre-built node pipeline, rebuilt when the chain changes.</summary>
  private IntentDelegate LatestPipeline
  {
    get
    {
      Location[] chain = Model.CurrentChain;
      if (!ReferenceEquals(_nodePipelineChain, chain))
      {
        _nodePipelineChain = chain;
        field = BuildNodePipeline(chain);
      }

      return field;
    }
  } = NoOpIntentHandler.NoOpTerminal;

  /// <summary>
  ///   Builds a chain-node pipeline — each node's view (outer) then
  ///   instance (inner), the outermost node first; an empty chain or a
  ///   chain without participating nodes gets the no-op terminal.
  /// </summary>
  private static IntentDelegate BuildNodePipeline(Location[] chain)
  {
    if (chain.Length == 0)
      return NoOpIntentHandler.NoOpTerminal;

    var handlers = new List<IIntentHandler>(chain.Length * 2);
    foreach (Location node in chain)
    {
      if (node.Presenter is IIntentHandler viewHandler)
        handlers.Add(viewHandler);
      if (node.Instance is IIntentHandler vmHandler)
        handlers.Add(vmHandler);
    }

    return IntentPipelineHelper.Build(handlers);
  }

  /// <inheritdoc />
  public async ValueTask HandleAsync(IntentContext context, IntentDelegate next)
  {
    if (IsStopRouting || context.IsTerminated)
    {
      await next(context);
      return;
    }

    // A one-shot surface answers no route consultation: it accepted its one
    // route and never navigates again, so the request falls through to a
    // navigable surface (an open derived navigation surface, or the base
    // router).  Skipping before the chain consultation is deliberate — the
    // derived router's own pages do not arbitrate a request that is not for them,
    // and this is what keeps stacked one-shot surfaces from handing the same
    // request to each other forever.  Back stays answered: it is about this
    // surface's own content.
    if (_ephemeral && context.Intent is RouteIntent)
    {
      await next(context);
      return;
    }

    // The presented chain's outside-in say — every intent is asked of the
    // chain's own pipeline (an overlay chain's for a derived router, the
    // main chain's for the base router) before the router interprets its
    // domain commands; a page can veto shell probes.
    await LatestPipeline(context);
    if (context.IsTerminated)
      return;

    // Only the routing domain's private commands reach the interpretation —
    // any other intent continues up the chain untouched.
    if (context.Intent is not IRouteIntent)
    {
      await next(context);
      return;
    }

    // Route/Back/Forward/Refresh are the router's domain commands.  A fault
    // in their transaction is user code throwing in the transaction pipe —
    // the router does not report it nor consume the command: the exception
    // propagates out of the dispatch into the intent chain's error
    // handling (the shell's layers segment reports through the Director).

    // Route — the routing domain's route command: a dispatched
    // RouteIntent consults the chain (the pages above may veto); the
    // router answers by routing the carried location.
    if (context.Intent is RouteIntent { Locator: var location })
    {
      await RouteAsync(location);
      context.Handle(this);
      return;
    }

    // Traversal — a back/forward traverses when the stack has room,
    // whether the stack is the base's main stack or an overlay's.  The
    // landing is observed (RunToLandingAsync awaits it) so a transaction
    // fault propagates into the intent chain.
    if (context.Intent is BackIntent && Model.CanGoBack)
    {
      await RunToLandingAsync(context, RoutingDirection.Back);
      return;
    }

    if (context.Intent is ForwardIntent && Model.CanGoForward)
    {
      await RunToLandingAsync(context, RoutingDirection.Forward);
      return;
    }

    // A back at the stack's foot — an overlay foot closes the derived
    // router and the back's return value settles its result channel; the
    // base foot consumes the back, never letting it leave the base.
    if (context.Intent is BackIntent back)
    {
      if (Role == RouterRole.Derived)
        _completion!.Complete(back.RetValue);
      context.Handle(this);
      return;
    }

    // Refresh replays the base's current chain in place — the derived
    // router answers no refresh; the command continues up the chain.
    if (Role == RouterRole.Base && context.Intent is RefreshIntent)
    {
      await RunToLandingAsync(context, RoutingDirection.Refresh);
      return;
    }

    // A routing-domain command the router does not answer — a forward at
    // the foot, a derived router's refresh — continues up the chain.
    await next(context);
  }

  /// <summary>
  ///   Runs a command transaction to its landing and settles the intent —
  ///   the intent path awaits the landing so a transaction fault rethrows
  ///   out of the dispatch (an unobserved landing would swallow the fault).
  ///   The transaction settles within the pump when it is idle, so the
  ///   await continues synchronously in that case.
  /// </summary>
  private async ValueTask RunToLandingAsync(IntentContext intent, RoutingDirection direction)
  {
    var transaction = new TransactionContext(direction);
    PipeRequest(transaction);
    await transaction.Result.Task;
    intent.Handle(this);
  }

  /// <summary>Reports collected failures through the error channel — a single failure as-is, multiple failures aggregated.</summary>
  internal void ReportFailures(IReadOnlyList<Exception> failures)
  {
    if (failures.Count == 0)
      return;
    if (failures.Count == 1)
      ErrorReporter.ReportError(failures[0]);
    else
      ErrorReporter.ReportError(new ConvergenceException(failures));
  }
}
