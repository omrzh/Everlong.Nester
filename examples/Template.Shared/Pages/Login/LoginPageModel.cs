using Everlong.Nester.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Everlong.DI;
using Everlong.Nester.Activation;
using Everlong.Nester.Auth;
using Everlong.Nester.Hosting;
using Everlong.Nester.Notice;
using Everlong.Nester.Routing;
using Everlong.Nester.Shell;
using Microsoft.Extensions.DependencyInjection;
using NesterApp.Models;
using NesterApp.Pages.Landing;
using NesterApp.Pages.Shell;
using NesterApp.Properties;

namespace NesterApp.Pages.Login;

/// <summary>
///   Abstract base for login-form ViewModels. Contains all shared form logic (form fields,
///   validation, commands). Concrete subclasses implement <see cref="PerformLogin" /> for
///   their specific runtime context: <see cref="LoginWindowModel" /> for desktop,
///   <see cref="LoginPageModel" /> for browser.
/// </summary>
[Routable]
[Transient]
public partial class LoginPageModel : RoutableModel, IDeparted
{
  [Inject] protected partial IAuthService AuthService { get; }
  [Inject] protected partial INoticeService Notice { get; }

  public LoginStrings LoginStrings => Lang.Login;

  [ObservableProperty] public partial string Password { get; set; } = "";
  [ObservableProperty] public partial string Username { get; set; } = "";

  [RelayCommand]
  private async Task Login()
  {
    if (Authenticate(Username, Password, out string[] roles))
    {
      await PerformLogin(Username, roles);
    }
    else
    {
      Notice.Toast(Lang.Login.InvalidCredentials, ToastLevel.Error);
    }
  }

  [RelayCommand]
  private Task LoginAsAdmin()
  {
    return PerformLogin("admin", ["Admin"]);
  }

  [RelayCommand]
  private Task LoginAsUser()
  {
    return PerformLogin("user", ["User"]);
  }

  private async Task PerformLogin(string username, IEnumerable<string> roles)
  {
    if (AppLifetime.IsSingleView) // for wpf this is never true
    {
      DemoUser user = new(username, roles);

      AuthService.Login(user);
      await base.Router.RouteAsync(new LandingLocator());
    }
    else
    {
      DemoUser user = new(username, roles);

      AuthService.Login(user);
      await Shell.DispatchIntent(this, new HideIntent());

      // Main window — the same assembly path as the App startup, written out:
      // the main window is created by the login flow, not by the App.  The
      // activation agent's pending intents (arrived during the login flow)
      // ride along as this shell's startup intent; an empty one means the
      // main window's Director routes its default page.
      var intentX = AppLifetime.Current?.Services?.GetService<IActivationAgent>()
        ?.TakePendingIntents().FirstOrDefault();
      var shell = new UiShell(intentX) { DirectorType = typeof(MainViewModel) };
      shell.Start();   // the main window is synchronously presented
      AppLifetime.SetMainShell(shell);   // the main-shell declaration is explicit (Start never promotes)
      await shell.Lifetime.Startup;   // wait for the first navigation to settle (optional — needed before closing the login window)

      await Shell.DispatchIntent(this, new CloseIntent());
    }
  }

  private bool Authenticate(string username, string password, out string[] roles)
  {
    roles = [];
    if (string.IsNullOrWhiteSpace(username))
    {
      return false;
    }

    if (username.Equals("admin", StringComparison.OrdinalIgnoreCase))
    {
      roles = ["Admin"];
      return true;
    }

    if (username.Equals("user", StringComparison.OrdinalIgnoreCase))
    {
      roles = ["User"];
      return true;
    }

    return false;
  }

  public void OnDeparted(IRoutingContext context)
  {
    if (AppLifetime.IsSingleView)
    {
      Router.Stack.TrimBackward();
    }
  }
}
