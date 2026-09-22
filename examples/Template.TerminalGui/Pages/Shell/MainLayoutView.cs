using Everlong.Nester.Presentation;
using NesterApp.Pages.Landing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace NesterApp.Pages.Shell;

/// <summary>
///   The main layout — the chrome (title + nav buttons) around the layout's
///   <c>Body</c> slot, which hosts the inner chain.
/// </summary>
public sealed class MainLayoutView : NesterView, ILayoutControl
{
  private readonly Button _backButton;
  private readonly Button _forwardButton;
  private readonly Button _refreshButton;
  private readonly Button _landingButton;
  private readonly TuiLayoutBody _body;

  public MainLayoutView()
  {
    _backButton = new Button { Text = "← Back", X = 1, Y = 2 };
    _forwardButton = new Button { Text = "Forward →", X = Pos.Right(_backButton) + 2, Y = 2 };
    _refreshButton = new Button { Text = "F5 Refresh", X = Pos.Right(_forwardButton) + 2, Y = 2 };
    _landingButton = new Button { Text = "Landing", X = Pos.Right(_refreshButton) + 2, Y = 2 };

    _body = new TuiLayoutBody { X = 0, Y = 4 };

    Add(
      new Label { Text = "Nester · Terminal.Gui (MainLayout)", X = 1, Y = 0 },
      new Label { Text = "────────────────────────────", X = 1, Y = 1 },
      _backButton,
      _forwardButton,
      _refreshButton,
      _landingButton,
      _body);

    _backButton.Accepted += (_, _) => Act(vm => vm.GoBackCommand.Execute(null));
    _forwardButton.Accepted += (_, _) => Act(vm => vm.GoForwardCommand.Execute(null));
    _refreshButton.Accepted += (_, _) => Act(vm => vm.RefreshCommand.Execute(null));
    _landingButton.Accepted += (_, _) => Act(vm => vm.Router.RouteAsync(new LandingLocator()));
  }

  /// <inheritdoc />
  public ILayoutBody GetLayoutBody() => _body;

  private void Act(Action<MainLayoutModel> action)
  {
    if (DataContext is MainLayoutModel vm)
      action(vm);
  }

  protected override void OnDataContextChanged()
  {
    bool wired = DataContext is MainLayoutModel;
    _backButton.Enabled = wired;
    _forwardButton.Enabled = wired;
    _refreshButton.Enabled = wired;
    _landingButton.Enabled = wired;
  }
}
