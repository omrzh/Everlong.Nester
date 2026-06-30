using Everlong.Nester.ComponentModel;
using Everlong.DI;
using Everlong.Nester.Routing;
using Everlong.Nester.RouteSync;
using NesterApp.Pages.Shell;
using NesterApp.Properties;
using System.Collections.ObjectModel;
using NesterApp.Models;

namespace NesterApp.Pages.Settings;

[Layout<MainLayoutModel>]
[Transient]
public class SettingsLayoutModel : RoutableModel
{
  public string Header => Lang.Pages.SettingsHeader;

  public ObservableCollection<IRouteItem> NavItems { get; } =
  [
    new RouteItem { Title = Lang.Navigation.General, Icon = new EmojiIcon("⚙️"), Destination = new SettingsLocator() },
    new RouteItem
      { Title = Lang.Navigation.Account, Icon = new EmojiIcon("👤"), Destination = new AccountSettingsLocator() },
    new RouteItem
      { Title = Lang.Navigation.Security, Icon = new EmojiIcon("🔐"), Destination = new SecuritySettingsLocator() }
  ];
}

