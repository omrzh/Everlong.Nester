using Everlong.Nester.ComponentModel;
using Everlong.DI;
using Everlong.Nester.Routing;
using NesterApp.Pages.Shell;

namespace NesterApp.Pages.Auth;

[Transient]
[Layout<MainLayoutModel>]
public class AuthLayoutModel : RoutableModel;
