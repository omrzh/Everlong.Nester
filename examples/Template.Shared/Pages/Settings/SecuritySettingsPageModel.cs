using Everlong.Nester.ComponentModel;
using Everlong.DI;
using Everlong.Nester.Routing;
using NesterApp.Properties;

namespace NesterApp.Pages.Settings;

[Routable]
[Layout<SettingsLayoutModel>]
[Transient]
public partial class SecuritySettingsPageModel : RoutableModel
{
  public SecuritySettings Security => AppSettings.Default.Security;
  public PagesStrings PagesStrings => Lang.Pages;

  public static SecuritySettingsPageModel Design { get; } = new();
}
