using Everlong.DI;
using Everlong.Nester.Activation;
using Everlong.Nester.Hosting;
using Everlong.Nester.Intent;
using Everlong.Nester.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Everlong.Nester.Shell;

partial class ShellBase
{
  // ── Startup ceremonies ──

  /// <summary>
  ///   Runs the shell's startup flow synchronously through the ready anchor:
  ///   assembly (container → agent → Director → stage → host → surface), the
  ///   ready anchor <see cref="OnAssembled"/> and the Director's
  ///   <see cref="IShellDirector.OnAssembled"/>, then intent-pipeline
  ///   activation and the startup dispatch.  The presentation chain
  ///   (<see cref="OnStarted"/>) runs fire-and-forget; its completion is
  ///   observable via <see cref="IShellLifetime.Startup"/>.
  /// </summary>
  public void Start()
  {
    // ── Assembly (framework-driven) ─────────────────────────────────────────
    EnsureAssembled(); // ① the container: InitializeServices → window scope (idempotent — the constructor path may have run it)
    BindAgent(); // ② the activation agent: declared ?? container-resolved (absent = no activation surface)

    // ── The ceremony (Director → stage → host → surface) ────────────────────
    PrepareDirector(); // ③ the decision maker: hand-assigned Director ?? DirectorType resolution
    PrepareStage(); // ④ the platform's stage: StagePanel + broker-ledger connection
    PrepareHost(); // ⑤ the host: produced by the platform; null → single-view direct mount / desktop fail-fast (platform decides)
    ConnectHost(); // ⑥ the surface: attach to the visual root, mount the stage into the host (or direct-mount), present, promote
    AppLifetime.Current?.Track(this); // tracked from mount: released at shutdown (idempotent)

    // ── Ready anchor (user-facing) ──────────────────────────────────────────
    _lifetime.Advance(ShellLifecycle.Assembled); // container + ceremony done — Services is live
    OnAssembled(); // ⑦ the shell is fully assembled — present the window (invisible-presentation convention) and run pre-flight assembly
    Director!.OnAssembled(this); // ⑦′ the Director's half of the ready anchor — per-director assembly (services live, host connected; the pipeline is not yet active)

    // ── Activation + run (framework-driven) ─────────────────────────────────
    _lifetime.Advance(ShellLifecycle.Started); // the intent pipeline is about to admit dispatches
    Activate(); // ⑧ pre-builds the intent pipeline (layers → Director → host → fallback)

    // ⑨ the startup dispatch (agent drain → own intent → default) runs
    //    fire-and-forget — the presentation chain awaits it; failures route
    //    to the error handler and fault the Startup signal.
    ObserveStartup(OnStarted(RunStartupDispatch()));
  }

  /// <summary>Assigns the provider returned by <see cref="InitializeServices"/> (the scope cut happens in the <see cref="RootProvider"/> setter).</summary>
  private void AssembleShell()
  {
    RootProvider = InitializeServices();
  }

  /// <summary>
  ///   The ready anchor — runs synchronously once the surface is connected,
  ///   before the Director's startup coroutine launches.  The host window
  ///   presents itself here; run the shell's pre-flight assembly (background
  ///   services bound to <see cref="IHostLifetime.Stopping"/> and any
  ///   dependency the first navigation needs) in this override.  The
  ///   Director's <see cref="IShellDirector.OnAssembled"/> runs immediately
  ///   after this hook.
  /// </summary>
  protected virtual void OnAssembled()
  {
  }

  /// <summary>
  ///   The shell's startup presentation chain, run fire-and-forget:
  ///   completes when the startup presentation finishes.
  ///   <paramref name="startupDispatch"/> completes when the startup
  ///   dispatch settles; the default awaits it.
  /// </summary>
  protected virtual Task OnStarted(Task startupDispatch) => startupDispatch;

  /// <summary>
  ///   Bridges the startup chain into the startup signal and the error
  ///   handler: success completes the signal; failure routes to
  ///   <see cref="ReportError"/> and faults the signal (waiters never
  ///   hang).  The fault is observed internally.
  /// </summary>
  protected void ObserveStartup(Task settle)
  {
    _ = settle.ContinueWith(t =>
    {
      if (t.IsFaulted)
      {
        Exception? cause = t.Exception?.GetBaseException() ?? new InvalidOperationException("Shell startup failed.");
        _lifetime.FaultStartup(cause);
        RouteStartupError(cause);
      }
      else
      {
        _lifetime.CompleteStartup();
      }
    }, TaskScheduler.Default);
  }

  /// <summary>
  ///   Routes a startup error to <see cref="ReportError"/> on the UI
  ///   thread — the settle continuation runs on the thread pool and the
  ///   Director's error handler is UI work; without a bound dispatcher the
  ///   report runs inline.
  /// </summary>
  private void RouteStartupError(Exception cause)
  {
    MainDispatcher.TryPost(() => ReportError(cause));
  }

  /// <summary>
  ///   Runs the shell's startup dispatch: drains the bound agent's startup
  ///   input, dispatches the shell's own startup intent (if any), then —
  ///   when nothing decided the first navigation — dispatches an empty
  ///   <see cref="ShellActivationIntent" /> the Director interprets as the
  ///   default page.  All dispatches go through the shell's intent pipeline;
  ///   failures route to the error handler via <see cref="ObserveStartup" />.
  /// </summary>
  protected async Task RunStartupDispatch()
  {
    bool decided = false;
    if (_boundAgent is { } agent)
    {
      decided = await agent.FlushAsync();
    }

    if (StartupIntent is { } intent)
    {
      decided |= (await DispatchIntent(this, intent)) != IntentResult.Pass;
    }

    if (!decided)
    {
      await DispatchIntent(this, new ShellActivationIntent());
    }
  }

  // ── Shell assembly stages (the framework's ceremony; the platform
  //    supplies the typed leaves via PrepareStage / PrepareHost /
  //    ConnectHost) ──

  /// <summary>
  ///   The user shell's assembly job: register the shell's services and
  ///   return the built provider.  Runs once by <see cref="Start"/>; the
  ///   base assigns it and cuts the window scope.
  /// </summary>
  protected abstract IServiceProvider InitializeServices();

  /// <summary>
  ///   The Director: a hand-assigned <see cref="Director"/> wins; otherwise
  ///   <see cref="DirectorType"/> is resolved from the window scope through
  ///   the Everlong.DI injector (member injection included).
  /// </summary>
  private void PrepareDirector()
  {
    if (Director is null && DirectorType is not null)
    {
      Director = (IShellDirector)ShellServiceScope!.ServiceProvider
        .GetRequiredService<IInjectorServiceProvider>()
        .GetRequiredService(DirectorType);
    }

    if (Director is null)
    {
      throw new InvalidOperationException(
        "Shell not assembled — no Director: set DirectorType before Start (e.g. " +
        "new MyShell { DirectorType = typeof(MainViewModel) }) or assign " +
        "Director in the shell's InitializeServices before Start.");
    }
  }

  /// <summary>
  ///   Pre-builds the shell's intent pipeline (layers → Director → host →
  ///   fallback) — dispatches are admitted only after the pipeline exists.
  /// </summary>
  private void Activate()
  {
    BuildShellPipeline();
  }
}
