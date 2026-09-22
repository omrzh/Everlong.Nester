using System.Windows;

namespace Everlong.Nester.Presentation;

public partial class RouteTreeControl : TreeControl
{
  static RouteTreeControl()
  {
    DefaultStyleKeyProperty.OverrideMetadata(typeof(RouteTreeControl),
      new FrameworkPropertyMetadata(typeof(RouteTreeControl)));

    // The control is the tree's frame, not an affordance in it: WPF would
    // otherwise stop a Tab here before the first item's header.  Staying
    // focusable but out of the tab order sends Tab straight into the items.
    IsTabStopProperty.OverrideMetadata(typeof(RouteTreeControl),
      new FrameworkPropertyMetadata(false));

    EventManager.RegisterClassHandler(typeof(RouteTreeControl), TreeItem.ItemClickedEvent,
      new RoutedEventHandler(static (s, e) =>
      {
        if (e.OriginalSource is TreeItem item)
          ((RouteTreeControl)s).OnChildItemClicked(item);
      }));
  }

  /// <summary>
  ///   Initializes a new instance of the <see cref="RouteTreeControl" /> class.
  /// </summary>
  public RouteTreeControl()
  {
    // WPF's Loaded broadcast only reaches elements with instance handlers
    // (class handlers alone do not arm BroadcastEventHelper) — the surface
    // bind must run from instance hooks.
    Loaded += OnSurfaceLoaded;
    Unloaded += OnSurfaceUnloaded;
  }

  /// <inheritdoc />
  protected override DependencyObject GetContainerForItemOverride()
    => new RouteTreeItem();

  /// <inheritdoc />
  protected override bool IsItemItsOwnContainerOverride(object item)
    => item is RouteTreeItem;

  /// <inheritdoc />
  protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
  {
    base.OnPropertyChanged(e);
    if (e.Property == ItemsSourceProperty)
      RecomputeHighlights();
  }

  private void OnSurfaceLoaded(object sender, RoutedEventArgs e) => BindSurface();

  private void OnSurfaceUnloaded(object sender, RoutedEventArgs e) => UnbindSurface();
}
