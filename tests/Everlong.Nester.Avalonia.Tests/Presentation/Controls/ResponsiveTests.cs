using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Everlong.Nester.Presentation;
using Everlong.Nester.RouteSync;
using NesterApp.Pages.Shell;
using Xunit;

namespace Everlong.Nester.Tests.Presentation;

/// <summary>
///   The <c>n:Responsive.CompactBelow</c> classification: an element carries
///   the <c>compact</c> class while the slot the layout arranged it into is
///   narrower than its own threshold.  The class is measured per element from
///   its own bounds, so it follows the slot and not the window.
/// </summary>
public class ResponsiveTests
{
  /// <summary>Mounts a stretch element in a host whose width the test drives — the slot.</summary>
  private static (Border Element, Panel Host, Window Window) Mount(double slotWidth)
  {
    var element = new Border();
    var host = new Panel { Width = slotWidth };
    host.Children.Add(element);
    var window = new Window { Content = host, Width = 800, Height = 300 };
    window.Show();
    window.UpdateLayout();
    return (element, host, window);
  }

  [AvaloniaFact]
  public void CompactBelow_NarrowSlot_CarriesTheClass()
  {
    (Border element, _, Window window) = Mount(100);
    try
    {
      Responsive.SetCompactBelow(element, 200);

      Assert.Contains(Responsive.CompactClass, element.Classes);
    }
    finally
    {
      window.Close();
    }
  }

  [AvaloniaFact]
  public void CompactBelow_WideSlot_DoesNotCarryTheClass()
  {
    (Border element, _, Window window) = Mount(400);
    try
    {
      Responsive.SetCompactBelow(element, 200);

      Assert.DoesNotContain(Responsive.CompactClass, element.Classes);
    }
    finally
    {
      window.Close();
    }
  }

  [AvaloniaFact]
  public void CompactBelow_SlotWidens_ClassGoesAway()
  {
    (Border element, Panel host, Window window) = Mount(100);
    try
    {
      Responsive.SetCompactBelow(element, 200);
      Assert.Contains(Responsive.CompactClass, element.Classes);

      host.Width = 400;
      window.UpdateLayout();

      Assert.DoesNotContain(Responsive.CompactClass, element.Classes);
    }
    finally
    {
      window.Close();
    }
  }

  [AvaloniaFact]
  public void CompactBelow_UnmeasuredElement_KeepsTheFullForm()
  {
    // The anti-flash rule: an element with no slot yet is not a narrow one,
    // so a control armed before its first layout pass is not classified.
    var element = new Border();

    Responsive.SetCompactBelow(element, 200);

    Assert.DoesNotContain(Responsive.CompactClass, element.Classes);
  }

  [AvaloniaFact]
  public void CompactBelow_Zero_DoesNotClassify()
  {
    (Border element, _, Window window) = Mount(100);
    try
    {
      Responsive.SetCompactBelow(element, 0);

      Assert.DoesNotContain(Responsive.CompactClass, element.Classes);
    }
    finally
    {
      window.Close();
    }
  }

  [AvaloniaFact]
  public void RouteTreeItem_DeclaresItsOwnSlotNeed()
  {
    // The framework's own chrome states its threshold where its content is,
    // so a tree in a rail-width slot classifies without the app saying so.
    (RouteTreeItem item, Window window) = MountItem(64);
    try
    {
      Assert.Contains(Responsive.CompactClass, item.Classes);
    }
    finally
    {
      window.Close();
    }
  }

  [AvaloniaFact]
  public void RouteTreeItem_CompactSlot_TheThemeDropsTheRowAndTheChildren()
  {
    // The class is only half the mechanism — the theme's reaction is the other
    // half, and only a themed headless run proves the two meet.  Every part the
    // rail drops is asserted: a selector that stops matching fails silently.
    (RouteTreeItem item, Window window) = MountItem(64, Group());
    try
    {
      Assert.Contains(Responsive.CompactClass, item.Classes);

      Assert.False(Part<TextBlock>(item, "TitleText").IsVisible, "a rail entry renders no title");
      Assert.False(Part<Panel>(item, "IndicatorGutter").IsVisible, "the indicator gutter goes");
      Assert.False(Part<Button>(item, "PART_ExpandButton").IsVisible, "a rail entry offers no chevron");
      Assert.False(Part<Border>(item, "ChildrenHost").IsVisible, "a rail entry never lays out children");
      Assert.Equal(HorizontalAlignment.Center, Part<Grid>(item, "HeaderRow").HorizontalAlignment);
    }
    finally
    {
      window.Close();
    }
  }

  [AvaloniaFact]
  public void RouteTreeItem_WideSlot_TheThemeKeepsTheRow()
  {
    (RouteTreeItem item, Window window) = MountItem(400, Group());
    try
    {
      Assert.DoesNotContain(Responsive.CompactClass, item.Classes);

      Assert.True(Part<TextBlock>(item, "TitleText").IsVisible, "a docked entry renders its title");
      Assert.True(Part<Panel>(item, "IndicatorGutter").IsVisible, "the gutter holds the icon's x");
      Assert.True(Part<Button>(item, "PART_ExpandButton").IsVisible, "a group entry offers its chevron");
      Assert.True(Part<Border>(item, "ChildrenHost").IsVisible, "an expanded group lays out its children");
    }
    finally
    {
      window.Close();
    }
  }

  /// <summary>A group whose row carries every part the rail form drops.</summary>
  private static RouteItem Group()
    => new() { Title = "section", Children = [new RouteItem { Title = "leaf" }] };

  private static (RouteTreeItem Item, Window Window) MountItem(double slotWidth, IRouteItem? context = null)
  {
    var item = new RouteTreeItem { DataContext = context };
    var host = new Panel { Width = slotWidth };
    host.Children.Add(item);
    var window = new Window { Content = host, Width = 800, Height = 300 };
    window.Show();
    window.UpdateLayout();
    return (item, window);
  }

  /// <summary>The themed part by name — the parts the rail form reacts to.</summary>
  private static T Part<T>(RouteTreeItem item, string name)
    where T : Control
  {
    item.ApplyTemplate();
    T? part = item.GetVisualDescendants().OfType<T>().FirstOrDefault(x => x.Name == name);
    Assert.NotNull(part);
    return part;
  }

  // ── the template's chrome, in miniature ──────────────────────────────────
  //
  // Two levels, and the reason for the second one: a root element whose class
  // narrows a pane, and the pane whose own slot then hides the labels it can no
  // longer fit.  The pane's width has to be a style and never a local value —
  // LocalValue outranks Style, so `Width="250"` in markup would outrank every
  // setter that tries to narrow it (see the trap test below).

  [AvaloniaFact]
  public void Chrome_WideSlot_KeepsTheFullSidebar()
  {
    (Grid root, Border sidebar, TextBlock label, _, Window window) = MountChrome(900, sidebarWidthIsLocal: false);
    try
    {
      Assert.DoesNotContain(Responsive.CompactClass, root.Classes);
      Assert.Equal(250, sidebar.Bounds.Width);
      Assert.True(label.IsVisible);
    }
    finally
    {
      window.Close();
    }
  }

  [AvaloniaFact]
  public void Chrome_NarrowSlot_NarrowsTheSidebarAndHidesItsLabels()
  {
    (Grid root, Border sidebar, TextBlock label, Panel host, Window window) = MountChrome(900, sidebarWidthIsLocal: false);
    try
    {
      Assert.Equal(250, sidebar.Bounds.Width);

      host.Width = 600;
      window.UpdateLayout();

      Assert.Contains(Responsive.CompactClass, root.Classes);
      Assert.Equal(64, sidebar.Bounds.Width);
      Assert.Contains(Responsive.CompactClass, sidebar.Classes);
      Assert.False(label.IsVisible);
    }
    finally
    {
      window.Close();
    }
  }

  [AvaloniaFact]
  public void Chrome_WidthSetLocally_OutranksTheStyleThatWouldNarrowIt()
  {
    // The trap this template shape has to avoid: a local value outranks a
    // style setter (BindingPriority.LocalValue is below Style), so a sidebar
    // whose width is written in markup never narrows — the pane keeps its
    // width and the star column absorbs the whole resize.
    (Grid root, Border sidebar, _, Panel host, Window window) = MountChrome(900, sidebarWidthIsLocal: true);
    try
    {
      host.Width = 600;
      window.UpdateLayout();

      Assert.Contains(Responsive.CompactClass, root.Classes);
      Assert.Equal(250, sidebar.Bounds.Width);
    }
    finally
    {
      window.Close();
    }
  }

  private static (Grid Root, Border Sidebar, TextBlock Label, Panel Host, Window Window) MountChrome(
    double slotWidth, bool sidebarWidthIsLocal)
  {
    var label = new TextBlock { Classes = { "collapse" } };
    var sidebar = new Border { Classes = { "sidebar" }, Child = label };
    if (sidebarWidthIsLocal)
      sidebar.Width = 250;

    var root = new Grid { Classes = { "root" }, ColumnDefinitions = new ColumnDefinitions("Auto,*") };
    root.Children.Add(sidebar);
    root.Styles.Add(Set(s => s.OfType<Border>().Class("sidebar"), Layoutable.WidthProperty, 250d));
    root.Styles.Add(Set(s => s.OfType<Grid>().Class("root").Class("compact").Descendant().OfType<Border>().Class("sidebar"),
                       Layoutable.WidthProperty, 64d));
    root.Styles.Add(Set(s => s.OfType<Border>().Class("sidebar").Class("compact").Descendant().OfType<TextBlock>().Class("collapse"),
                       Visual.IsVisibleProperty, false));
    Responsive.SetCompactBelow(root, 700);
    Responsive.SetCompactBelow(sidebar, 150);

    var host = new Panel { Width = slotWidth };
    host.Children.Add(root);
    var window = new Window { Content = host, Width = 1000, Height = 300 };
    window.Show();
    window.UpdateLayout();
    return (root, sidebar, label, host, window);
  }

  private static Style Set(Func<Selector?, Selector> selector, AvaloniaProperty property, object value)
  {
    var style = new Style(selector);
    style.Setters.Add(new Setter(property, value));
    return style;
  }

  [AvaloniaFact]
  public void Chrome_Compact_SwapsTheTreeForTheRailProjection()
  {
    // The template's other half: one menu, two projections, swapped by the root's
    // class.  A rail can act on destinations only, so the compact form renders the
    // flattened menu — every entry live — and never the headings' dead icons.
    IReadOnlyList<IRouteItem> menu = MainLayoutModel.CreateMenu();
    var tree = new RouteTreeControl { Classes = { "navTree" }, ItemsSource = menu };
    var rail = new RouteTreeControl
    {
      Classes = { "navRail" },
      ItemsSource = MainLayoutModel.Destinations(menu).ToList(),
    };
    var sidebarInner = new StackPanel();
    sidebarInner.Children.Add(tree);
    sidebarInner.Children.Add(rail);
    var sidebar = new Border { Classes = { "sidebar" }, Child = sidebarInner };

    var root = new Grid { Classes = { "root" }, ColumnDefinitions = new ColumnDefinitions("Auto,*") };
    root.Children.Add(sidebar);
    root.Styles.Add(Set(s => s.OfType<Border>().Class("sidebar"), Layoutable.WidthProperty, 250d));
    root.Styles.Add(Set(s => s.OfType<Grid>().Class("root").Class("compact").Descendant().OfType<Border>().Class("sidebar"),
                       Layoutable.WidthProperty, 64d));
    root.Styles.Add(Set(s => s.OfType<RouteTreeControl>().Class("navRail"), Visual.IsVisibleProperty, false));
    root.Styles.Add(Set(s => s.OfType<Grid>().Class("root").Class("compact").Descendant().OfType<RouteTreeControl>().Class("navRail"),
                       Visual.IsVisibleProperty, true));
    root.Styles.Add(Set(s => s.OfType<Grid>().Class("root").Class("compact").Descendant().OfType<RouteTreeControl>().Class("navTree"),
                       Visual.IsVisibleProperty, false));
    Responsive.SetCompactBelow(root, 700);
    Responsive.SetCompactBelow(sidebar, 150);

    var host = new Panel { Width = 900 };
    host.Children.Add(root);
    var window = new Window { Content = host, Width = 1000, Height = 420 };
    window.Show();
    window.UpdateLayout();
    try
    {
      Assert.True(tree.IsVisible);
      Assert.False(rail.IsVisible);

      host.Width = 600;
      window.UpdateLayout();

      Assert.False(tree.IsVisible);
      Assert.True(rail.IsVisible);
      Assert.Equal(6, tree.ItemCount);
      Assert.Equal(10, rail.ItemCount);
      Assert.All(rail.GetVisualDescendants().OfType<RouteTreeItem>(), item =>
      {
        Assert.Contains(Responsive.CompactClass, item.Classes);
        Assert.Equal(item.Title, ToolTip.GetTip(item) as string);
      });
    }
    finally
    {
      window.Close();
    }
  }
}
