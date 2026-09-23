using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Everlong.Nester.Presentation;
using Everlong.Nester.RouteSync;
using Everlong.Nester.Routing;
using Everlong.Nester.Tests.Routing;
using Xunit;

using static Everlong.Nester.Tests.Navigation.ChainBuilder;

namespace Everlong.Nester.Tests.Navigation;

/// <summary>
///   The tree coordinator folds the whole item tree once per landing, so a
///   group's descendant highlight is not recomputed by every ancestor.
/// </summary>
public class RouteTreeFoldTests
{
  private sealed class MainLayout { }

  private sealed class PageRoot { }

  private sealed class PageChild { }

  private sealed class PageOne { }

  private sealed class PageTwo { }

  private sealed class PageThree { }

  /// <summary>Counts how often the matcher reads <see cref="Destination" />.</summary>
  private sealed class CountingItem(string title, ILocator destination, params IRouteItem[] children) : IRouteItem
  {
    internal static int DestinationReads;

    public string Title => title;

    public object? Icon => null;

    public IReadOnlyList<IRouteItem> Children => children;

    public ILocator? Destination
    {
      get
      {
        DestinationReads++;
        return destination;
      }
    }
  }

  [AvaloniaFact]
  public async Task Control_FoldsEachNodeOncePerLanding()
  {
    CountingItem.DestinationReads = 0;

    // Five nodes, every one carrying a destination so each fold evaluation
    // reads it exactly twice (guard + target).
    var one = new CountingItem("One", RouteTo(typeof(PageOne), typeof(MainLayout)));
    var two = new CountingItem("Two", RouteTo(typeof(PageTwo), typeof(MainLayout)));
    var three = new CountingItem("Three", RouteTo(typeof(PageThree), typeof(MainLayout)));
    var child = new CountingItem("Child", RouteTo(typeof(PageChild), typeof(MainLayout)), one, two);
    var root = new CountingItem("Root", RouteTo(typeof(PageRoot), typeof(MainLayout)), child, three);

    var shell = new FakeShell();
    var host = new RoutingView();
    var router = new TestRouter(shell, host);
    var control = new RouteTreeControl { ItemsSource = new IRouteItem[] { root } };
    host.Children.Add(control);
    var window = new Window { Content = host, Width = 400, Height = 300 };
    window.Show();
    window.UpdateLayout();
    Dispatcher.UIThread.RunJobs();
    control.UpdateLayout();

    try
    {
      int before = CountingItem.DestinationReads;

      await router.RouteAsync(RouteTo(typeof(PageTwo), typeof(MainLayout)));

      // One fold over five nodes × two reads.  A per-ancestor recomputation
      // (N × H) would read far more.
      Assert.Equal(10, CountingItem.DestinationReads - before);
    }
    finally
    {
      window.Close();
    }
  }
}
