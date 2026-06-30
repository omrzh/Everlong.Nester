using Everlong.Nester.ComponentModel;
using Everlong.DI;
using Everlong.Nester.Routing;
using NesterApp.Properties;

namespace NesterApp.Pages.Settings;

[Routable]
[Layout<SettingsLayoutModel>]
[Transient]
public partial class AccountSettingsPageModel : RoutableModel
{
  public AccountSettings Account => AppSettings.Default.Account;
  public PagesStrings PagesStrings => Lang.Pages;
}
