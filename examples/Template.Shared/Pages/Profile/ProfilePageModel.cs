using Everlong.Nester.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Everlong.DI;
using Everlong.Nester.Dialog;
using Everlong.Nester.Intent;
using Everlong.Nester.Routing;
using NesterApp.Pages.Shell;
using NesterApp.Properties;

namespace NesterApp.Pages.Profile;

[Routable]
[Layout<MainLayoutModel>]
[Transient]
public partial class ProfilePageModel : RoutableModel, IIntentHandler
{
  public PagesStrings PagesStrings => Lang.Pages;

  [ObservableProperty] public partial string Bio { get; set; } = "Software Developer";
  [ObservableProperty] public partial string DisplayName { get; set; } = "John Doe";

  public async ValueTask HandleAsync(IntentContext context, IntentDelegate next)
  {
    if (context.Intent is not BackIntent)
    {
      await next(context);
      return;
    }

    // The dirty check is skipped: the confirmation shows unconditionally.
    bool confirmed = await Router.ConfirmAsync(
                       Lang.Pages.ProfileLeaveConfirmMessage,
                       new ConfirmOptions
                       {
                         ConfirmText = Lang.Pages.ProfileLeaveConfirmYes,
                         CancelText = Lang.Pages.ProfileLeaveConfirmStay
                       });
    // confirmed = user wants to leave → not blocked → continue
    // canceled  = user wants to stay  → blocked → veto
    if (!confirmed)
    {
      context.Veto(this);
      return;
    }
    await next(context);
  }

  partial void OnDisplayNameChanged(string value)
  {
  }

  partial void OnBioChanged(string value)
  {
  }

  [RelayCommand]
  private void Save()
  {
    // fake logic
  }

  [RelayCommand]
  private void Cancel()
  {
    // Revert or just clear dirty flag
  }
}
