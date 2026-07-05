using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Everlong.Nester.Controls;
using Everlong.Nester.RouteSync;
using Everlong.Nester.Tests.Routing;
using Xunit;

using static Everlong.Nester.Tests.Navigation.ChainBuilder;

namespace Everlong.Nester.Tests.Navigation;

/// <summary>
///   Single-item-type tree rendering.  Every node — leaves included — is an
///   <see cref="IRouteItem" />; a leaf is a node with an empty child list.
///   The real <see cref="RouteTreeControl" /> nests containers through
///   <see cref="IRouteItem.Children" /> and highlights through the model.
/// </summary>
public class RouteTreeRenderingTests
{
  private sealed class MainLayout;

  private sealed class LandingPage;

  private sealed class PostsPage;

  private sealed class SettingsPage;

  private sealed class AccountPage;

  private static RouteItem Leaf(Type page, params Type[] layouts)
    => new() { Title = page.Name, Destination = RouteTo(page, layouts) };

  private static RouteItem Group(string title, params IRouteItem[] children)
    => new() { Title = title, Children = children };

  private static (RouteTreeControl Control, Window Window, TestRouter Router) Mount(params IRouteItem[] roots)
  {
    var shell = new FakeShell();
    var host = new NavigationHost();
    var router = new TestRouter(shell, host);
    var control = new RouteTreeControl { ItemsSource = roots };
    host.Children.Add(control);
    var window = new Window { Content = host, Width = 400, Height = 300 };
    window.Show();
    window.UpdateLayout();
    Dispatcher.UIThread.RunJobs();
    control.UpdateLayout();
    return (control, window, router);
  }

  private static (RouteItem Main, RouteItem Settings) Tree()
  {
    RouteItem main = Group("Main",
      Leaf(typeof(LandingPage), typeof(MainLayout)),
      Leaf(typeof(PostsPage), typeof(MainLayout)));
    RouteItem settings = new()
    {
      Title = "Settings",
      Destination = RouteTo(typeof(SettingsPage), typeof(MainLayout)),
      Children = [Leaf(typeof(AccountPage), typeof(MainLayout))]
    };
    return (main, settings);
  }

  [AvaloniaFact]
  public void Item_RendersRootsAndNestsChildren()
  {
    (RouteItem main, RouteItem settings) = Tree();

    (RouteTreeControl control, Window window, _) = Mount(main, settings);
    try
    {
      Assert.Equal(2, control.ItemCount);

      var mainItem = Assert.IsType<RouteTreeItem>(control.ContainerFromIndex(0));
      var settingsItem = Assert.IsType<RouteTreeItem>(control.ContainerFromIndex(1));
      Assert.Same(main, mainItem.DataContext);
      Assert.Same(settings, settingsItem.DataContext);

      // The container itself carries the child list — the recursion.
      Assert.Equal(2, mainItem.ItemCount);
      Assert.Equal(1, settingsItem.ItemCount);

      var landingItem = Assert.IsType<RouteTreeItem>(mainItem.ContainerFromIndex(0));
      var postsItem = Assert.IsType<RouteTreeItem>(mainItem.ContainerFromIndex(1));
      Assert.Same(main.Children[0], landingItem.DataContext);
      Assert.Same(main.Children[1], postsItem.DataContext);

      Assert.Equal(0, landingItem.ItemCount);
      Assert.Equal(0, postsItem.ItemCount);
    }
    finally
    {
      window.Close();
    }
  }

  [AvaloniaFact]
  public void Leaf_IsNotCollapsible_AndDoesNotExpand()
  {
    (RouteItem main, RouteItem settings) = Tree();

    (RouteTreeControl control, Window window, _) = Mount(main, settings);
    try
    {
      var mainItem = Assert.IsType<RouteTreeItem>(control.ContainerFromIndex(0));
      var landingItem = Assert.IsType<RouteTreeItem>(mainItem.ContainerFromIndex(0));

      Assert.True(mainItem.IsCollapsible);
      Assert.False(landingItem.IsCollapsible);
      Assert.False(landingItem.IsExpanded);
    }
    finally
    {
      window.Close();
    }
  }

  [AvaloniaFact]
  public async Task Item_HighlightsLeafAndAncestorGroup()
  {
    (RouteItem main, RouteItem settings) = Tree();

    (RouteTreeControl control, Window window, TestRouter router) = Mount(main, settings);
    try
    {
      var mainItem = Assert.IsType<RouteTreeItem>(control.ContainerFromIndex(0));
      var settingsItem = Assert.IsType<RouteTreeItem>(control.ContainerFromIndex(1));
      var landingItem = Assert.IsType<RouteTreeItem>(mainItem.ContainerFromIndex(0));
      var postsItem = Assert.IsType<RouteTreeItem>(mainItem.ContainerFromIndex(1));
      var accountItem = Assert.IsType<RouteTreeItem>(settingsItem.ContainerFromIndex(0));

      await router.RouteAsync(RouteTo(typeof(PostsPage), typeof(MainLayout)));

      Assert.True(postsItem.IsActive);
      Assert.False(landingItem.IsActive);
      Assert.False(accountItem.IsActive);
      Assert.True(mainItem.HasHighlightedDescendant);
      Assert.False(settingsItem.HasHighlightedDescendant);

      await router.RouteAsync(RouteTo(typeof(AccountPage), typeof(MainLayout)));

      Assert.True(accountItem.IsActive);
      Assert.True(settingsItem.HasHighlightedDescendant);
      Assert.False(mainItem.HasHighlightedDescendant);
    }
    finally
    {
      window.Close();
    }
  }

  [AvaloniaFact]
  public void Item_ResolvesNavigationTargets()
  {
    RouteItem pureGroup = Group("Main", Leaf(typeof(PostsPage), typeof(MainLayout)));
    RouteItem navigableGroup = new()
    {
      Title = "Settings",
      Destination = RouteTo(typeof(SettingsPage), typeof(MainLayout)),
      Children = [Leaf(typeof(AccountPage), typeof(MainLayout))]
    };
    RouteItem leaf = Leaf(typeof(LandingPage), typeof(MainLayout));

    (RouteTreeControl control, Window window, _) = Mount(pureGroup, navigableGroup, leaf);
    try
    {
      var pureGroupItem = Assert.IsType<RouteTreeItem>(control.ContainerFromIndex(0));
      var navigableGroupItem = Assert.IsType<RouteTreeItem>(control.ContainerFromIndex(1));
      var leafItem = Assert.IsType<RouteTreeItem>(control.ContainerFromIndex(2));

      Assert.Null(pureGroupItem.GetNavigationRoute());
      Assert.Same(navigableGroup.Destination, navigableGroupItem.GetNavigationRoute());
      Assert.Same(leaf.Destination, leafItem.GetNavigationRoute());
    }
    finally
    {
      window.Close();
    }
  }
}
