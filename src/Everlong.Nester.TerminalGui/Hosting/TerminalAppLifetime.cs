using Everlong.Nester.Shell;
using Terminal.Gui.App;

namespace Everlong.Nester.Hosting;

/// <summary>
///   The Terminal.Gui lifetime — a single-view host: the stage is the
///   whole surface; the main loop is the process's only window.
/// </summary>
internal sealed class TerminalAppLifetime : AppLifetimeBase
{
  private readonly IApplication _app;

  internal TerminalAppLifetime(IApplication app, AppLifetimeOptions options)
  {
    _app = app;
    TerminalRuntime.App = app;
    Dispatcher = new TerminalMainDispatcher(app);
    Services = options.Services;
    InitializeHost(options.ErrorHandler, options.ErrorHandling);
  }

  /// <inheritdoc />
  public override bool IsSingleView => true;

  /// <inheritdoc />
  protected override void ReplaceSingleViewShell(IShell shellContext)
  {
    if (MainShell is { } oldShell && !ReferenceEquals(oldShell, shellContext))
    {
      Untrack(oldShell);
      _ = (oldShell as IAsyncDisposable)?.DisposeAsync().AsTask() ?? Task.CompletedTask;
    }
  }

  /// <inheritdoc />
  protected override void ShutdownCore(int exitCode)
  {
    try
    {
      _app.RequestStop();
    }
    catch
    {
      // best-effort — the loop may already be unwinding
    }
  }
}
