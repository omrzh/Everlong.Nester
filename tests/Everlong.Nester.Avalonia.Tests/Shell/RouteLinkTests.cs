using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Everlong.Nester.Controls;
using Everlong.Nester.RouteSync;
using Everlong.Nester.Routing;
using Everlong.Nester.Tests.Routing;
using Xunit;

namespace Everlong.Nester.Tests.Shell;

/// <summary>
///   RouteLink is the command-free nav chrome: the type carries no command
///   surface, and activation issues the item's navigation request.
/// </summary>
public class RouteLinkTests
{
  private sealed class MainLayout { }

  private sealed class PostsPage { }

  private static RouteItem PostsItem()
    => new()
    {
      Title = "Posts",
      Destination = new Locator([Target.Of(typeof(MainLayout)), Target.Of(typeof(PostsPage))]),
    };

  private static RouteItem PureGroupItem() => new() { Title = "Group" };

  private static (RouteLink Link, Window Window, TestRouter Router) Mount(IRouteItem item)
  {
    var shell = new FakeShell();
    var host = new NavigationHost();
    var router = new TestRouter(shell, host);
    var link = new RouteLink { DataContext = item };
    host.Children.Add(link);
    var window = new Window { Content = host, Width = 300, Height = 200 };
    window.Show();
    window.UpdateLayout();
    return (link, window, router);
  }

  /// <summary>Raises the template button's click — the link's own activation path.</summary>
  private static void Activate(RouteLink link)
  {
    link.ApplyTemplate();
    var button = link.GetVisualDescendants().OfType<Button>().FirstOrDefault();
    Assert.NotNull(button);
    button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
  }

  private static async Task WaitAsync(Func<bool> condition)
  {
    for (int i = 0; i < 200 && !condition(); i++)
    {
      Dispatcher.UIThread.RunJobs();
      await Task.Delay(5);
    }
  }

  /// <summary>Replaces the control template with one that carries no <c>PART_LinkButton</c>.</summary>
  private static void Retemplate(RouteLink link)
    => link.Template = new FuncControlTemplate<RouteLink>((_, _) => new Border
    {
      Background = Brushes.Transparent,
      Height = 40,
    });

  /// <summary>Clicks the link's center with real pointer input.</summary>
  private static void Click(Window window, RouteLink link)
  {
    Point center = link.TranslatePoint(new Point(link.Bounds.Width / 2, link.Bounds.Height / 2), window)!.Value;
    window.MouseDown(center, MouseButton.Left);
    window.MouseUp(center, MouseButton.Left);
  }

  [Fact]
  public void Link_CarriesNoCommandSurface()
  {
    Assert.Null(typeof(RouteLink).GetProperty("Command"));
    Assert.Null(typeof(RouteLink).GetProperty("CommandParameter"));
  }

  [AvaloniaFact]
  public async Task Link_Activation_Navigates()
  {
    (RouteLink link, Window window, TestRouter router) = Mount(PostsItem());
    try
    {
      Assert.Null(router.Stack.Location);

      Activate(link);
      await WaitAsync(() => router.Stack.Location?.Type == typeof(PostsPage));

      Assert.Equal(typeof(PostsPage), router.Stack.Location?.Type);
      Assert.True(link.IsRouteHighlighted);
    }
    finally
    {
      window.Close();
    }
  }

  [AvaloniaFact]
  public async Task Link_Activation_RaisesClickAfterNavigation()
  {
    (RouteLink link, Window window, TestRouter router) = Mount(PostsItem());
    try
    {
      Type? typeAtClick = null;
      link.Click += (_, _) => typeAtClick = router.Stack.Location?.Type;

      Activate(link);
      await WaitAsync(() => typeAtClick is not null);

      Assert.Equal(typeof(PostsPage), typeAtClick);
    }
    finally
    {
      window.Close();
    }
  }

  [AvaloniaFact]
  public async Task Link_WithoutDestination_RaisesClickWithoutNavigating()
  {
    (RouteLink link, Window window, TestRouter router) = Mount(PureGroupItem());
    try
    {
      bool clicked = false;
      link.Click += (_, _) => clicked = true;

      Activate(link);
      await WaitAsync(() => clicked);

      Assert.True(clicked);
      Assert.Null(router.Stack.Location);
    }
    finally
    {
      window.Close();
    }
  }

  [AvaloniaFact]
  public async Task Link_Activation_WithRetemplatedControl_Navigates()
  {
    (RouteLink link, Window window, TestRouter router) = Mount(PostsItem());
    try
    {
      Retemplate(link);
      window.UpdateLayout();

      Click(window, link);
      await WaitAsync(() => router.Stack.Location?.Type == typeof(PostsPage));

      Assert.Equal(typeof(PostsPage), router.Stack.Location?.Type);
      Assert.True(link.IsRouteHighlighted);
    }
    finally
    {
      window.Close();
    }
  }

  [AvaloniaFact]
  public async Task Link_Activation_WithRetemplatedControl_KeyboardNavigates()
  {
    (RouteLink link, Window window, TestRouter router) = Mount(PostsItem());
    try
    {
      Retemplate(link);
      window.UpdateLayout();

      Assert.True(link.Focus());
      window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
      await WaitAsync(() => router.Stack.Location?.Type == typeof(PostsPage));

      Assert.Equal(typeof(PostsPage), router.Stack.Location?.Type);
    }
    finally
    {
      window.Close();
    }
  }

  [AvaloniaFact]
  public async Task Link_Activation_DefaultTemplate_RaisesClickOnce()
  {
    (RouteLink link, Window window, TestRouter router) = Mount(PostsItem());
    try
    {
      int clicks = 0;
      link.Click += (_, _) => clicks++;

      Click(window, link);
      await WaitAsync(() => clicks > 0);

      Assert.Equal(typeof(PostsPage), router.Stack.Location?.Type);
      Assert.Equal(1, clicks);
    }
    finally
    {
      window.Close();
    }
  }
}
