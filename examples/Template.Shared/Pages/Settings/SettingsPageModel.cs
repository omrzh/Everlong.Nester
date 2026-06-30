using Everlong.Nester.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Everlong.DI;
using Everlong.Nester.Dialog;
using Everlong.Nester.Hosting;
using Everlong.Nester.Routing;
using Everlong.Nester.Shell;
using Microsoft.Extensions.DependencyInjection;
using NesterApp.Pages.Shell;
using NesterApp.Properties;
using NesterApp.Services;

namespace NesterApp.Pages.Settings;

[Routable]
[Layout<SettingsLayoutModel>]
[Transient]
public partial class SettingsPageModel : RoutableModel
{
  /// <summary>The theme applier; <see langword="null" /> in a host that registers none.</summary>
  [Inject] protected partial IThemeService? ThemeService { get; }

  public AppSettings AppSettings => AppSettings.Default;

  public SettingsStrings SettingsStrings => Lang.Settings;

  public static SettingsPageModel Design { get; } = new();

  /// <summary>Applies the theme choice and persists it.</summary>
  [RelayCommand]
  private void SetTheme(AppTheme theme)
  {
    AppSettings.Theme = theme;
    ThemeService?.Apply(theme);
  }

  [RelayCommand]
  private async Task UseLanguage(string lang)
  {
    if (lang == AppSettings.Language)
    {
      return;
    }

    AppSettings.Language = lang;
    // Switch all registered assemblies to the best matching locale.
    Lang.UseLocale(lang);
    // Live view refresh is not reliable for ViewModels holding hardcoded
    // strings — the clean way is to restart the window; note that
    // Lang.Settings.* already resolves to the new locale immediately.
    bool confirm = await Router.ConfirmAsync(Lang.Settings.RestartConfirmMessage,
                                             new ConfirmOptions { Title = Lang.Settings.RestartConfirmTitle });
    if (confirm)
    {
      // Rebuild the shell — the same assembly path as the App startup,
      // written out.  The one Director drives both environments; only the
      // window flow needs hide/close around the rebuild (single-view: Show
      // reconnects the MainView).
      if (!AppLifetime.IsSingleView)
      {
        await Shell.DispatchIntent(this, new HideIntent());
      }

      var shell = new UiShell { DirectorType = typeof(MainViewModel) };
      shell.Start();
      AppLifetime.SetMainShell(shell);   // the main-shell declaration is explicit (Start never promotes)
      await shell.Lifetime.Startup;
      await shell.Services!.GetRequiredService<IRouter>()
        .RouteAsync(new SettingsLocator());

      if (!AppLifetime.IsSingleView)
      {
        await Shell.DispatchIntent(this, new CloseIntent());
      }
    }
  }
}
