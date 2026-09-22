using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Everlong.Nester.Presentation;
using Everlong.Nester.RouteSync;
using Everlong.Nester.Tests.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using Everlong.Nester.Intent;
using Everlong.Nester.Routing;

namespace Everlong.Nester.Tests.Routing;

/// <summary>
///   The nav chrome highlight channel: a RouteLink / RouteTreeItem under a
///   routing surface (the RoutingView) binds to the surface's router at
///   attach and follows the router stack's site notifications — the chrome
///   lights up and dims as the surface navigates, without any tree-wide
///   property broadcast.
/// </summary>
public class NavChromeHighlightTests
{
  private sealed class MainLayout { }

  private sealed class PostsPage { }

  private sealed class ProfilePage { }

  private sealed class SettingsPage { }

  private sealed class AccountPage { }

  /// <summary>Presents a detail page while claiming the Posts destination.</summary>
  private sealed class PostDetailPage : IRouteHighlight
  {
    public bool? Represents(ILocator destination)
      => destination.Path[^1].Type == typeof(PostsPage) ? true : null;
  }

  private static RouteItem PostsItem()
    => new()
    {
      Title = "Posts",
      Destination = new Locator([Target.Of(typeof(MainLayout)), Target.Of(typeof(PostsPage))]),
    };

  [AvaloniaFact]
  public async Task LinkUnderSurface_FollowsRouterStack_OnNavigation()
  {
    var shell = new FakeShell();
    var host = new RoutingView();
    var router = new TestRouter(shell, host);

    var link = new RouteLink { DataContext = PostsItem() };
    host.Children.Add(link);

    var window = new Window { Content = host, Width = 400, Height = 300 };
    window.Show();
    try
    {
      // Nothing presented yet — the link bound on attach must read the
      // stack's (empty) site and stay inactive.
      Assert.False(link.IsActive);

      // Navigate away first — the Posts item must not light.
      await router.RouteAsync(new Locator([Target.Of(typeof(MainLayout)), Target.Of(typeof(ProfilePage))]));
      Assert.False(link.IsActive);

      // Navigate to the item's target — the stack raise refreshes the chrome.
      await router.RouteAsync(new Locator([Target.Of(typeof(MainLayout)), Target.Of(typeof(PostsPage))]));
      Assert.True(link.IsActive);

      // Navigate away again — the highlight follows.
      await router.RouteAsync(new Locator([Target.Of(typeof(MainLayout)), Target.Of(typeof(ProfilePage))]));
      Assert.False(link.IsActive);
    }
    finally
    {
      window.Close();
    }
  }

  [AvaloniaFact]
  public async Task LinkUnderSurface_ActiveViaParticipantVote()
  {
    var shell = new FakeShell();
    var host = new RoutingView();
    var router = new TestRouter(shell, host);

    var link = new RouteLink { DataContext = PostsItem() };
    host.Children.Add(link);

    var window = new Window { Content = host, Width = 400, Height = 300 };
    window.Show();
    try
    {
      // The detail page claims the Posts destination — the Posts link lights
      // up even though the Posts page itself is not in the chain.
      await router.RouteAsync(new Locator(
        [Target.Of(typeof(MainLayout)), Target.Of(typeof(PostDetailPage), instance: new PostDetailPage())]));
      Assert.True(link.IsActive);

      await router.RouteAsync(new Locator([Target.Of(typeof(MainLayout)), Target.Of(typeof(ProfilePage))]));
      Assert.False(link.IsActive);
    }
    finally
    {
      window.Close();
    }
  }

  [AvaloniaFact]
  public async Task TreeItem_Group_IsSelfActiveOnItsOwnPage()
  {
    var shell = new FakeShell();
    var host = new RoutingView();
    var router = new TestRouter(shell, host);

    var item = new RouteTreeItem
    {
      DataContext = new RouteItem
      {
        Title = "Settings",
        Destination = new Locator([Target.Of(typeof(MainLayout)), Target.Of(typeof(SettingsPage))]),
        Children =
        [
          new RouteItem
          {
            Title = "Account",
            Destination = new Locator(
              [Target.Of(typeof(MainLayout)), Target.Of(typeof(SettingsPage)), Target.Of(typeof(AccountPage))]),
          },
        ],
      },
    };
    host.Children.Add(item);

    var window = new Window { Content = host, Width = 400, Height = 300 };
    window.Show();
    try
    {
      await router.RouteAsync(new Locator([Target.Of(typeof(MainLayout)), Target.Of(typeof(SettingsPage))]));
      Assert.True(item.IsActive);

      await router.RouteAsync(new Locator([Target.Of(typeof(MainLayout)), Target.Of(typeof(ProfilePage))]));
      Assert.False(item.IsActive);
    }
    finally
    {
      window.Close();
    }
  }

  [AvaloniaFact]
  public async Task TreeItem_Group_HasHighlightedDescendant_WhileAChildIsPresented()
  {
    var shell = new FakeShell();
    var host = new RoutingView();
    var router = new TestRouter(shell, host);

    var child = PostsItem();
    var groupItem = new RouteTreeItem
    {
      DataContext = new RouteItem
      {
        Title = "Main",
        Destination = new Locator([Target.Of(typeof(MainLayout)), Target.Of(typeof(SettingsPage))]),
        Children = [child],
      },
    };
    var leafItem = new RouteTreeItem { DataContext = child };
    host.Children.Add(groupItem);
    host.Children.Add(leafItem);

    var window = new Window { Content = host, Width = 400, Height = 300 };
    window.Show();
    try
    {
      await router.RouteAsync(new Locator([Target.Of(typeof(MainLayout)), Target.Of(typeof(PostsPage))]));

      Assert.False(groupItem.IsActive);
      Assert.True(groupItem.HasHighlightedDescendant);
      Assert.True(leafItem.IsActive);
    }
    finally
    {
      window.Close();
    }
  }
}

/// <summary>
///   A route request dispatched as an intent is consumed by the router —
///   the intent-chain navigation entry the nav chrome uses.
/// </summary>
[Collection("RealShell")]
public class RouteIntentTests
{
  [AvaloniaFact]
  public async Task Route_DispatchedAsIntent_RoutesOnRealShell()
  {
    var shell = TestHost.CreateShell<NoopDirector>(s =>
    {
      s.AddSingleton<TestContent>(_ => new TestContent());
    });
    shell.Start();

    var router = new Router(shell.Services);
    await router.RouteAsync(new Locator(typeof(TestContent)));

    IntentResult handled = await shell.DispatchIntent(null, new RouteIntent(new Locator(typeof(TestContent))));

    Assert.Equal(IntentResult.Handled, handled);
    Assert.NotNull(router.Model);
    Assert.Equal(typeof(TestContent), router.Model!.CurrentChain![^1].Type);
  }
}
