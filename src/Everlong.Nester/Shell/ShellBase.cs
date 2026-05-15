using System.ComponentModel;
using Everlong.Nester.Activation;
using Everlong.Nester.Intent;
using Microsoft.Extensions.DependencyInjection;

namespace Everlong.Nester.Shell;

/// <summary>
///   Platform-neutral shell core: shell assembly, intent dispatch, error
///   routing and teardown.
/// </summary>
/// <remarks>
///   Platform shells supply the host and stage via three steps
///   (<see cref="PrepareStage"/>, <see cref="PrepareHost"/>, <see cref="ConnectHost"/>);
///   the base guarantees their order and never references host or stage
///   types.  <see cref="InitializeServices"/> returns the shell's own
///   provider; the base cuts the window scope from it.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract partial class ShellBase : IShell, IAsyncDisposable
{
  /// <summary>The shell's decision maker, assigned during initialization (set <see cref="DirectorType"/> or assign <see cref="Director"/>).</summary>
  protected internal IShellDirector? Director { get; protected set; }

  /// <summary>
  ///   The shell's own startup intent — the activation input handed to this
  ///   shell at construction (an intent parked while no host was bound);
  ///   <see langword="null" /> when this shell has none.
  /// </summary>
  protected IActivationIntent? StartupIntent { get; }

  /// <summary>
  ///   Declares the Director's type, can be null if <see cref="Director"/> is assigned directly.
  /// </summary>
  public virtual Type? DirectorType { get; init; }

  /// <summary>
  ///   The shell's root container — returned by <see cref="InitializeServices"/>;
  ///   the base assigns it and cuts the <see cref="ShellServiceScope"/> from
  ///   it (not for direct resolution).  Assignable once — a second non-null
  ///   assignment throws; a null assignment (teardown) is always allowed.
  /// </summary>
  protected internal IServiceProvider? RootProvider
  {
    get;
    private set
    {
      if (value is not null && field is not null)
      {
        throw new InvalidOperationException(
          "RootProvider already assigned — the shell's container is assigned once (in Initialize).");
      }

      field = value;
      if (value is not null)
      {
        ShellServiceScope = value.CreateScope(); // assigning the container cuts the shell scope (the resolution face)
      }
    }
  }

  /// <summary>The window's resolution face — the framework's scope cut from the assigned root (Scoped services resolve per window here); <see cref="IShell.Services"/> derives from it.</summary>
  protected IServiceScope? ShellServiceScope { get; private set; }

  /// <summary>The window container's resolution face — throws before assembly and after disposal (never <see langword="null"/>); see <see cref="ShellBase.EnsureAssembled"/>.</summary>
  public IServiceProvider Services => RequireShellServiceProvider();

  /// <summary>No platform services by default — platform shells override.</summary>
  public abstract T? GetPlatformService<T>() where T : class;

  /// <inheritdoc />
  public abstract nint HostHandle { get; }

  // ── Platform hooks (the platform implementation's contract surface) ──

  /// <summary>Creates the shell's stage — the visual stack the broker ledger mounts onto — and connects the ledger to it.</summary>
  protected abstract void PrepareStage();

  /// <summary>
  ///   Produces the shell's host surface — the window (desktop) or a host
  ///   view / nothing (single-view) — and decides the no-host case
  ///   (single-view direct mount; desktop fail-fast).  The mount itself
  ///   happens in <see cref="ConnectHost"/>.
  /// </summary>
  protected abstract void PrepareHost();

  /// <summary>
  ///   Connects the shell to the surface: attaches the shell to the visual
  ///   root, mounts the stage into the host (or direct-mounts it), presents
  ///   (single-view MainView) and promotes (desktop MainWindow).
  /// </summary>
  protected abstract void ConnectHost();

  /// <summary>
  ///   The host's intent-handling participation — the pipeline's host
  ///   stage (layers → Director → host → fallback).  <see langword="null"/>
  ///   when there is no host or the host does not participate; the base
  ///   substitutes a pass-through so the pipeline folds uniformly.
  /// </summary>
  protected abstract IIntentHandler? HostHandler { get; }

  /// <summary>Platform hook: the shell's fallback stage — walks the tunnel first, then handles what fell through.</summary>
  /// <remarks>Async — the close path awaits <see cref="DisposeAsync"/> deterministically.</remarks>
  protected abstract ValueTask HandleFallbackIntentAsync(IntentContext context, IntentDelegate next);
}
