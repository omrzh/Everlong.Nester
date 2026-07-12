using Everlong.Nester.Activation;
using Everlong.Nester.Controls;
using Everlong.Nester.Hosting;
using Everlong.Nester.Intent;
using Everlong.Nester.Layer;
using Terminal.Gui.Views;

namespace Everlong.Nester.Shell;

/// <summary>
///   The Terminal.Gui platform shell — a single-view shell: the stage is
///   the whole surface, run directly by the application loop.
/// </summary>
public abstract class TuiShell : ShellBase
{
  /// <summary>The shell's stage — created at <see cref="PrepareStage" />, the surface handed to the app loop.</summary>
  internal TuiStage? Stage { get; private set; }

  /// <summary>The surface to run — the stage itself.</summary>
  public Runnable Surface => Stage ?? throw new InvalidOperationException(
    "The shell has no surface yet — Start() must run before the surface is used.");

  /// <summary>Creates the shell with the optional startup intent this shell's dispatch starts from.</summary>
  protected TuiShell(IActivationIntent? startupIntent = null)
    : base(startupIntent)
  {
  }

  /// <inheritdoc />
  public override T? GetPlatformService<T>() where T : class => null;

  /// <inheritdoc />
  public override nint HostHandle => 0;

  /// <inheritdoc />
  protected override ILayerLease CreateLease(ILayerLedger ledger, int z)
    => new TuiLayerLease(new TuiLayer(), ledger, z);

  /// <inheritdoc />
  protected override void PrepareStage()
  {
    Stage ??= new TuiStage();
    ConnectLedger(Stage);
  }

  /// <inheritdoc />
  protected override void PrepareHost()
  {
  }

  /// <inheritdoc />
  protected override void ConnectHost()
  {
  }

  /// <inheritdoc />
  protected override IIntentHandler? HostHandler => null;

  /// <inheritdoc />
  protected override async ValueTask HandleFallbackIntentAsync(IntentContext context, IntentDelegate next)
  {
    await next(context);
    if (context.IsTerminated)
      return;

    if (context.Intent is not IShellIntent)
      return;

    switch (context.Intent)
    {
      case CloseIntent:
      case TryCloseIntent:
        // The intent reached the last link = nobody vetoed: destroy the
        // shell, then end the app session.
        await DisposeAsync();
        TerminalRuntime.App?.RequestStop();
        context.Handle(this);
        break;
      case HideIntent:
      case ShowIntent:
      case MutateShellStateIntent:
      case RestoreShellStateIntent:
      case TopmostIntent:
      case CenterOnScreenIntent:
      case CenterOnOwnerIntent:
        // The terminal surface has no window chrome — the family is consumed.
        context.Handle(this);
        break;
    }
  }
}
