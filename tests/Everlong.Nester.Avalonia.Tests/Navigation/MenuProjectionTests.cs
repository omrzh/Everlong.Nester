using Everlong.Nester.RouteSync;
using Everlong.Nester.Routing;
using NesterApp.Pages.Admin;
using NesterApp.Pages.Labs;
using NesterApp.Pages.Landing;
using NesterApp.Pages.Posts;
using NesterApp.Pages.Profile;
using NesterApp.Pages.Settings;
using NesterApp.Pages.Shell;
using Xunit;

namespace Everlong.Nester.Tests.Navigation;

/// <summary>
///   The menu's compact projection.  A rail can express destinations and not
///   containment, so the projection keeps every destination and drops the headings —
///   an entry a rail shows must be one it can act on, and a heading's children are
///   promoted rather than stranded behind an icon that does nothing.
/// </summary>
public class MenuProjectionTests
{
  private static RouteItem Destination(ILocator locator, params IRouteItem[] children)
    => new() { Title = locator.GetType().Name, Destination = locator, Children = children };

  private static RouteItem Heading(params IRouteItem[] children)
    => new() { Title = "heading", Children = children };

  [Fact]
  public void Destinations_KeepsEveryDestinationAndDropsTheHeadings()
  {
    RouteItem heading = Heading(
      Destination(new LandingLocator()),
      Destination(new PostsLocator()));
    RouteItem section = Destination(new SettingsLocator(), Destination(new AccountSettingsLocator()));
    RouteItem leaf = Destination(new AdminLocator());

    IEnumerable<Type> projected = MainLayoutModel.Destinations([heading, section, leaf])
      .Select(item => item.Destination!.GetType());

    Assert.Equal(
      [typeof(LandingLocator), typeof(PostsLocator), typeof(SettingsLocator), typeof(AccountSettingsLocator), typeof(AdminLocator)],
      projected);
  }

  [Fact]
  public void Destinations_EveryProjectedItemIsAnAction()
  {
    Assert.All(MainLayoutModel.Destinations(MainLayoutModel.CreateMenu()), item => Assert.NotNull(item.Destination));
  }

  [Fact]
  public void Menu_ProjectsToTheRailsContents()
  {
    // The real menu: Main is the only heading, so only its three children are
    // promoted; Settings is itself a destination and keeps its place ahead of its
    // own children.
    IEnumerable<Type> projected = MainLayoutModel.Destinations(MainLayoutModel.CreateMenu())
      .Select(item => item.Destination!.GetType());

    Assert.Equal(
      [
        typeof(LandingLocator),
        typeof(InteractionLabLocator),
        typeof(PostsLocator),
        typeof(SettingsLocator),
        typeof(AccountSettingsLocator),
        typeof(SecuritySettingsLocator),
        typeof(ProfileLocator),
        typeof(AdminLocator),
        typeof(RouteItemLabLocator),
        typeof(VideoPlayerLocator),
      ],
      projected);
  }
}
