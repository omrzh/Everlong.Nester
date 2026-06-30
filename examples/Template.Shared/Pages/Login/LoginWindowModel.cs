using Everlong.Nester.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Everlong.Nester.Auth;
using Everlong.Nester.Intent;
using Everlong.Nester.Shell;
using Microsoft.Extensions.DependencyInjection;
using NesterApp.Properties;
using Everlong.Nester.Activation;

namespace NesterApp.Pages.Login;

/// <summary>
///   The login-window Director — fully shared: both platforms run the exact
///   same type (no platform partial needed).  The window-status strings,
///   the startup coroutine and the intent handling all live here.  The
///   login window hosts its page bare — no layout chrome.
/// </summary>
public partial class LoginWindowModel : RoutableModel, IShellDirector
{
  public static LoginStrings LoginStrings => Lang.Login;

  [RelayCommand]
  private Task Close()
  {
    return Shell.DispatchIntent(this, new CloseIntent()).AsTask();
  }

  public async ValueTask HandleAsync(IntentContext context, IntentDelegate next)
  {
    switch (context.Intent)
    {
      case ShellActivationIntent:
        // The empty activation — the login window's first navigation is
        // fixed: the login form.  Launch intents are interpreted by the
        // main window's Director after login.
        await Router.RouteAsync(new LoginLocator());
        context.Handle(this);
        return;
      case NegotiateActivationIntent:
        // means we are performing login while a new process started
        context.Handle(this);
        return;
    }

    await next(context);
  }

  /// <summary>Runs once the shell is assembled — before the startup dispatch.</summary>
  public void OnAssembled(IShell shell)
  {
    // Pre-flight: the login form's authorization surface resolves before
    // any intent is dispatched — the first navigation loads against a
    // valid graph.
    _ = shell.Services.GetRequiredService<IAuthService>();
  }

  public bool HandleError(Exception exception)
  {
    // Trace before routing on — false hands the failure to the host-level
    // error handler.  Debug output + the console host.
    try
    {
      System.Diagnostics.Debug.WriteLine(exception);
      Console.Error.WriteLine(exception);
    }
    catch
    {
      // logging must never break the error path
    }

    return false;
  }
}
