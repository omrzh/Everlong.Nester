using Everlong.Nester.Dialog;
using Everlong.Nester.Presentation;
using NesterApp.Dialogs;
using NesterApp.Pages.Landing;
using NesterApp.Pages.Login;
using NesterApp.Pages.Shell;
using NesterApp.Views;
using Terminal.Gui.ViewBase;

namespace NesterApp;

/// <summary>
///   The Terminal.Gui view locator — the model→view translation table:
///   every participant type resolves to a <see cref="View" />; unmapped
///   types fall back to a placeholder so navigation never hits a missing
///   view.
/// </summary>
public sealed class PageViewLocator : IViewLocator
{
  /// <inheritdoc />
  /// <remarks>
  ///   The locator is total: every non-null participant resolves — a mapped
  ///   type to its view, anything else to the placeholder.
  /// </remarks>
  public bool Match(object? data) => data is not null;

  /// <inheritdoc />
  public View? Build(object? data) => data switch
  {
    LoginPageModel => new LoginPageView(),
    MainLayoutModel => new MainLayoutView(),
    LandingPageModel => new LandingPageView(),
    DefaultDimmerModel => new DialogDimmerView(),
    ConfirmDialogSession => new ConfirmDialogView(),
    _ => data is null ? null : new PlaceholderView(data.GetType().Name)
  };
}
