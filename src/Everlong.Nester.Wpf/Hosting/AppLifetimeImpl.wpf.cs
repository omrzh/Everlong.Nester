using System.Windows;
using Everlong.Nester.Shell;

namespace Everlong.Nester.Hosting;

internal sealed class AppLifetimeImpl : AppLifetimeBase
{
  private static PApp App =>
    PApp.Current ?? throw new InvalidOperationException("Static app facade not available.");


  public AppLifetimeImpl(AppLifetimeOptions options)
  {
    // Host the platform main-thread dispatcher as a managed service (user-facing
    // ViewModel API; framework internals use UIDispatcher instead).
    Dispatcher = new WpfMainDispatcher();

    if (options.DisposeOnExit)
    {
      App.Exit += DisposeOnExit;
    }

    if (options.UseExplicitShutdown)
    {
      App.ShutdownMode = ShutdownMode.OnExplicitShutdown;
    }

    Services = options.Services; // the user-built process container — the host only owns (releases) it
    InitializeHost(options.ErrorHandler, options.ErrorHandling);
    if (options.ErrorHandler != null)
    {
      BindDispatcherErrorHook();
    }
  }

  private async void DisposeOnExit(object sender, ExitEventArgs e)
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

  public override bool IsSingleView => false;


  protected override void ReplaceSingleViewShell(IShell shellContext)
  {
    // WPF is always desktop — no single-view shell replacement.
  }

  protected override void ShutdownCore(int exitCode)
  {
    App?.Shutdown(exitCode);
  }

  private void BindDispatcherErrorHook()
  {
    App.DispatcherUnhandledException += OnDispatcherUnhandledException;
  }

  private void OnDispatcherUnhandledException(object sender,
                                              System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
  {
    ReportAppError(e.Exception);
    e.Handled = true;
  }
}
