using Everlong.Nester.Dialog;
using Everlong.Nester.Presentation;
using Terminal.Gui.Views;

namespace NesterApp.Pages.Landing;

/// <summary>
///   The landing page — the inner page under <see cref="MainLayoutView" />.
///   Minimal content: the routing + layout-body slice it proves is real,
///   plus a confirm-dialog trigger for the dialog domain.
/// </summary>
public sealed class LandingPageView : NesterView
{
  private readonly Label _resultLabel;

  public LandingPageView()
  {
    var dialogButton = new Button { Text = "Try a confirm dialog", X = 2, Y = 6 };
    _resultLabel = new Label { X = 2, Y = 8, Width = 80 };

    Add(
      new Label { Text = "🚀 Landing", X = 2, Y = 1 },
      new Label { Text = "The login → MainLayout → Landing chain is alive.", X = 2, Y = 3 },
      new Label { Text = "Press F5 to refresh, Alt+← to go back (history).", X = 2, Y = 4 },
      dialogButton,
      _resultLabel);

    dialogButton.Accepted += async (_, _) =>
    {
      if (DataContext is not LandingPageModel vm)
        return;

      var session = new ConfirmDialogSession
      {
        Title = "Demo",
        Message = "Proceed with the demo?",
        ConfirmText = "Yes, proceed",
        CancelText = "No, stay",
        IsConfirmEnabled = true
      };

      bool? result = await vm.Router.ShowAsync<bool?>(session);
      _resultLabel.Text = result is true ? "Confirmed ✓" : "Cancelled";
    };
  }

  protected override void OnDataContextChanged()
  {
    if (DataContext is LandingPageModel vm)
    {
      Title = $"Landing · {vm.GetType().Name}";
    }
  }
}
