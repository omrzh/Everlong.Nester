using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Everlong.Nester.Controls;
using Everlong.Nester.RouteSync;
using Everlong.Nester.Routing;
using Everlong.Nester.Tests.Routing;
using Xunit;

namespace Everlong.Nester.Tests.Navigation;

/// <summary>
///   Route-derived highlight and app-derived highlight are separate channels:
///   <see cref="RouteLink.IsRouteHighlighted" /> reports the routing answer,
///   <see cref="RouteLink.IsHighlighted" /> overrides it, and
///   <see cref="RouteLink.IsActive" /> is the effective state.
/// </summary>
public class HighlightOverrideTests
{
  private sealed class MainLayout { }

  private sealed class PostsPage { }

  private sealed class ProfilePage { }

  private static RouteItem PostsItem()
    => new()
    {
      Title = "Posts",
      Destination = new Locator([Target.Of(typeof(MainLayout)), Target.Of(typeof(PostsPage))]),
    };

  private static Locator PostsRoute()
    => new([Target.Of(typeof(MainLayout)), Target.Of(typeof(PostsPage))]);

  private static Locator ProfileRoute()
    => new([Target.Of(typeof(MainLayout)), Target.Of(typeof(ProfilePage))]);

  private static (RouteLink Link, Window Window, TestRouter Router) MountLink()
  {
    var shell = new FakeShell();
    var host = new NavigationHost();
    var router = new TestRouter(shell, host);
    var link = new RouteLink { DataContext = PostsItem() };
    host.Children.Add(link);
    var window = new Window { Content = host, Width = 300, Height = 200 };
    window.Show();
    return (link, window, router);
  }

  [AvaloniaFact]
  public async Task Link_FollowsTheRoute_WithoutAnOverride()
  {
    (RouteLink link, Window window, TestRouter router) = MountLink();
    try
    {
      await router.RouteAsync(PostsRoute());
      Assert.True(link.IsRouteHighlighted);
      Assert.Null(link.IsHighlighted);
      Assert.True(link.IsActive);

      await router.RouteAsync(ProfileRoute());
      Assert.False(link.IsRouteHighlighted);
      Assert.False(link.IsActive);
    }
    finally
    {
      window.Close();
    }
  }

  [AvaloniaFact]
  public async Task Link_OverrideTrue_WinsOverTheRoute()
  {
    (RouteLink link, Window window, TestRouter router) = MountLink();
    try
    {
      link.IsHighlighted = true;

      await router.RouteAsync(ProfileRoute());

      Assert.False(link.IsRouteHighlighted);
      Assert.True(link.IsActive);
    }
    finally
    {
      window.Close();
    }
  }

  [AvaloniaFact]
  public async Task Link_OverrideFalse_WinsOverTheRoute()
  {
    (RouteLink link, Window window, TestRouter router) = MountLink();
    try
    {
      link.IsHighlighted = false;

      await router.RouteAsync(PostsRoute());

      Assert.True(link.IsRouteHighlighted);
      Assert.False(link.IsActive);
    }
    finally
    {
      window.Close();
    }
  }

  [AvaloniaFact]
  public async Task Link_ClearingTheOverride_FollowsTheRouteAgain()
  {
    (RouteLink link, Window window, TestRouter router) = MountLink();
    try
    {
      link.IsHighlighted = false;
      await router.RouteAsync(PostsRoute());
      Assert.False(link.IsActive);

      link.IsHighlighted = null;

      Assert.True(link.IsActive);
    }
    finally
    {
      window.Close();
    }
  }

  [AvaloniaFact]
  public async Task TreeItem_Override_WinsOverTheRoute()
  {
    var shell = new FakeShell();
    var host = new NavigationHost();
    var router = new TestRouter(shell, host);
    var item = new RouteTreeItem { DataContext = PostsItem(), IsHighlighted = true };
    host.Children.Add(item);
    var window = new Window { Content = host, Width = 300, Height = 200 };
    window.Show();
    try
    {
      await router.RouteAsync(ProfileRoute());

      Assert.False(item.IsRouteHighlighted);
      Assert.True(item.IsActive);
    }
    finally
    {
      window.Close();
    }
  }
}
