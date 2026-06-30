using Everlong.Nester.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Everlong.DI;
using Everlong.Nester.Routing;
using NesterApp.Pages.Admin;
using NesterApp.Pages.Labs;
using NesterApp.Pages.Posts;
using NesterApp.Pages.Profile;
using NesterApp.Pages.Settings;
using NesterApp.Pages.Shell;
using NesterApp.Properties;

namespace NesterApp.Pages.Landing;

/// <summary>
///   The smallest complete page — a routed model plus its view.  Copy these
///   three files (<c>LandingPageModel.cs</c>, <c>LandingPage.axaml</c>,
///   <c>LandingPage.axaml.cs</c>) for a new page: the model keeps
///   <c>[Routable]</c> + <c>[Layout&lt;MainLayoutModel&gt;]</c>, the view keeps
///   <c>[ViewFor&lt;LandingPageModel&gt;]</c>, and that is the whole wiring.
/// </summary>
[Routable]
[Layout<MainLayoutModel>]
[Transient]
public partial class LandingPageModel : RoutableModel
{
  public PagesStrings PagesStrings => Lang.Pages;

  /// <summary>The workspace palette's hint line — the feature, named where a user lands first.</summary>
  public WorkspaceStrings WorkspaceStrings => Lang.Workspace;

  [RelayCommand]
  private Task OpenInteractionLab()
  {
    return Router.RouteAsync(new InteractionLabLocator());
  }

  [RelayCommand]
  private Task OpenPosts()
  {
    return Router.RouteAsync(new PostsLocator());
  }

  [RelayCommand]
  private Task OpenProfile()
  {
    return Router.RouteAsync(new ProfileLocator());
  }

  [RelayCommand]
  private Task OpenSettings()
  {
    return Router.RouteAsync(new SettingsLocator());
  }

  [RelayCommand]
  private Task OpenAdmin()
  {
    return Router.RouteAsync(new AdminLocator());
  }
}
