using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Everlong.Nester.Shell;

namespace Everlong.Nester.Hosting;

internal sealed class AppLifetimeImpl : AppLifetimeBase
{
  private readonly bool _isSingleView;

  private static PApp App => PApp.Current ?? throw new InvalidOperationException("App not started??");


  public AppLifetimeImpl(AppLifetimeOptions options, bool isSingleView)
  {
    ArgumentNullException.ThrowIfNull(PApp.Current, nameof(PApp.Current));
    _isSingleView = isSingleView;

    // Host the platform main-thread dispatcher as a managed service (user-facing
    // ViewModel API; framework internals use UIDispatcher instead).
    Dispatcher = new AvaloniaMainDispatcher();

    TryHookAppExit(options);
    Services = options.Services; // the user-built process container — the host only owns (releases) it
    InitializeHost(options.ErrorHandler, options.ErrorHandling);
    if (options.ErrorHandler != null)
    {
      BindDispatcherErrorHook();
    }
  }

  private void TryHookAppExit(AppLifetimeOptions options)
  {
    ArgumentNullException.ThrowIfNull(App);
    if (options.Desktop?.DisposeOnExit == true
        && App.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
    {
      desktopLifetime.Exit += OnDesktopLifetimeExit;
    }
  }

  public override bool IsSingleView => _isSingleView;


  protected override void ReplaceSingleViewShell(IShell shellContext)
  {
    // Close and dispose the old shell.
    if (MainShell is { } oldShell && !ReferenceEquals(oldShell, shellContext))
    {
      Untrack(oldShell);

      // The old shell's single-view surface is detached inside its own
      // DisposeAsync — ownership-guarded (a replaced shell never clears the
      // new shell's view).  The synchronous Untrack drops it from the
      // teardown registry immediately (its own end-of-teardown untrack
      // would land too late — the cascade could re-snapshot it mid-close).
      _ = (oldShell as IAsyncDisposable)?.DisposeAsync().AsTask() ?? Task.CompletedTask;
    }
  }

  protected override void ShutdownCore(int exitCode)
  {
    // DesktopLifetime.Shutdown(exitCode);
  }

  private void BindDispatcherErrorHook()
  {
    Avalonia.Threading.Dispatcher.UIThread.UnhandledException += OnDispatcherUnhandledException;
  }

  private void OnDispatcherUnhandledException(object? sender, DispatcherUnhandledExceptionEventArgs e)
  {
    ReportAppError(e.Exception);
    e.Handled = true;
  }

  private async void OnDesktopLifetimeExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
  {
    try
    {
      await DisposeManagedResources();
    }
    catch (Exception ex)
    {
      ReportAppError(ex);
    }
  }
}
