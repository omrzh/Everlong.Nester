using Everlong.Nester.Auth;
using Everlong.Nester.ComponentModel;
using Everlong.DI;
using Everlong.Nester.Routing;
using NesterApp.Pages.Auth;
using NesterApp.Properties;

namespace NesterApp.Pages.Admin;

// Simple ViewModels
[Routable]
[Layout<AuthLayoutModel>]
[Authorize(Policy = "Admin")]
[Transient]
public partial class AdminPageModel : RoutableModel
{
  public PagesStrings PagesStrings => Lang.Pages;
}
