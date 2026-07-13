using Everlong.Nester.Presentation;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace NesterApp.Pages.Login;

/// <summary>
///   The login page — the single-view first page.  Demo quick logins prove
///   the auth + routing flow (Admin / User accounts).
/// </summary>
public sealed class LoginPageView : NesterView
{
  private readonly Button _adminButton;
  private readonly Button _userButton;

  public LoginPageView()
  {
    _adminButton = new Button { Text = "Login as admin", X = Pos.Center(), Y = 7 };
    _userButton = new Button { Text = "Login as user", X = Pos.Center(), Y = 9 };

    Add(
      new Label { Text = "Nester · Terminal.Gui template", X = Pos.Center(), Y = 3 },
      new Label { Text = "Pick a demo account:", X = Pos.Center(), Y = 5 },
      _adminButton,
      _userButton,
      new Label { Text = "Ctrl+Q quits — F5 refreshes — Alt+←/→ back & forward", X = Pos.Center(), Y = 13 });

    _adminButton.Accepted += (_, _) => LoginAs(vm => vm.LoginAsAdminCommand.Execute(null));
    _userButton.Accepted += (_, _) => LoginAs(vm => vm.LoginAsUserCommand.Execute(null));
  }

  private void LoginAs(Action<LoginPageModel> action)
  {
    if (DataContext is LoginPageModel vm)
      action(vm);
  }

  protected override void OnDataContextChanged()
  {
    // The buttons act on the attached participant — nothing to wire until
    // the participant arrives.
    _adminButton.Enabled = DataContext is LoginPageModel;
    _userButton.Enabled = DataContext is LoginPageModel;
  }
}
