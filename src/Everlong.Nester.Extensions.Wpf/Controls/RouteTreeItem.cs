using System.Windows;
using System.Windows.Controls;
using Everlong.Nester.Presentation;
using Everlong.Nester.RouteSync;
using Everlong.Nester.Routing;

namespace Everlong.Nester.Controls;

/// <summary>
///   A navigation tree item whose DataContext is an <see cref="IRouteItem" /> — a leaf when
///   its child list is empty, a group otherwise. Binds to its routing surface (the nearest
///   <see cref="IRoutingView" /> host) and follows the surface router's
///   <see cref="IRouterStack" /> site changes to keep <see cref="IsRouteHighlighted" />,
///   <see cref="HasHighlightedDescendant" />, <see cref="TreeItem.IsExpanded" />, and child
///   rendering in sync with the presented chain.
/// </summary>
public partial class RouteTreeItem : TreeItem
{
  /// <summary>
  ///   Identifies the <see cref="IsCollapsible" /> dependency property.
  /// </summary>
  public static readonly DependencyProperty IsCollapsibleProperty =
    DependencyProperty.Register(nameof(IsCollapsible), typeof(bool), typeof(RouteTreeItem),
                                new PropertyMetadata(true));

  /// <summary>
  ///   Identifies the <see cref="IsActive" /> dependency property.
  /// </summary>
  public static readonly DependencyProperty IsActiveProperty =
    DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(RouteTreeItem),
                                new PropertyMetadata(false));

  /// <summary>
  ///   Identifies the <see cref="IsRouteHighlighted" /> dependency property.
  /// </summary>
  public static readonly DependencyProperty IsRouteHighlightedProperty =
    DependencyProperty.Register(nameof(IsRouteHighlighted), typeof(bool), typeof(RouteTreeItem),
                                new PropertyMetadata(false,
                                  static (d, _) => ((RouteTreeItem)d).RefreshEffective()));

  /// <summary>
  ///   Identifies the <see cref="IsHighlighted" /> dependency property.
  /// </summary>
  public static readonly DependencyProperty IsHighlightedProperty =
    DependencyProperty.Register(nameof(IsHighlighted), typeof(bool?), typeof(RouteTreeItem),
                                new PropertyMetadata(null,
                                  static (d, _) => ((RouteTreeItem)d).RefreshEffective()));

  /// <summary>
  ///   Identifies the <see cref="HasHighlightedDescendant" /> dependency property.
  /// </summary>
  public static readonly DependencyProperty HasHighlightedDescendantProperty =
    DependencyProperty.Register(nameof(HasHighlightedDescendant), typeof(bool), typeof(RouteTreeItem),
                                new PropertyMetadata(false));

  /// <summary>
  ///   Identifies the <see cref="Title" /> dependency property.
  /// </summary>
  public static readonly DependencyProperty TitleProperty =
    DependencyProperty.Register(nameof(Title), typeof(string), typeof(RouteTreeItem),
                                new PropertyMetadata(string.Empty, OnTitleChanged));

  /// <summary>
  ///   Identifies the <see cref="Icon" /> dependency property.
  /// </summary>
  public static readonly DependencyProperty IconProperty =
    DependencyProperty.Register(nameof(Icon), typeof(object), typeof(RouteTreeItem),
                                new PropertyMetadata(null));

  /// <summary>
  ///   Identifies the <see cref="HasIcon" /> dependency property.
  /// </summary>
  public static readonly DependencyProperty HasIconProperty =
    DependencyProperty.Register(nameof(HasIcon), typeof(bool), typeof(RouteTreeItem),
                                new PropertyMetadata(false));

  static RouteTreeItem()
  {
    DefaultStyleKeyProperty.OverrideMetadata(typeof(RouteTreeItem),
      new FrameworkPropertyMetadata(typeof(RouteTreeItem)));
  }

  /// <summary>Gets or sets whether this item can be collapsed by clicking its header.</summary>
  public bool IsCollapsible
  {
    get => (bool)GetValue(IsCollapsibleProperty);
    set => SetValue(IsCollapsibleProperty, value);
  }

  /// <summary>Gets the effective highlight — the app override when set, otherwise the routing answer.</summary>
  public bool IsActive
  {
    get => (bool)GetValue(IsActiveProperty);
    protected set => SetValue(IsActiveProperty, value);
  }

  /// <summary>Gets whether the routing surface highlights this item.</summary>
  public bool IsRouteHighlighted
  {
    get => (bool)GetValue(IsRouteHighlightedProperty);
    protected set => SetValue(IsRouteHighlightedProperty, value);
  }

  /// <summary>
  ///   Gets or sets the app-supplied highlight override.
  ///   <see langword="null" /> follows <see cref="IsRouteHighlighted" />.
  /// </summary>
  public bool? IsHighlighted
  {
    get => (bool?)GetValue(IsHighlightedProperty);
    set => SetValue(IsHighlightedProperty, value);
  }

  /// <summary>Gets whether any descendant of this group item matches the current route.</summary>
  public bool HasHighlightedDescendant
  {
    get => (bool)GetValue(HasHighlightedDescendantProperty);
    protected set => SetValue(HasHighlightedDescendantProperty, value);
  }

  /// <summary>Gets the display title sourced from the DataContext.</summary>
  public string Title
  {
    get => (string)GetValue(TitleProperty);
    protected set => SetValue(TitleProperty, value);
  }

  /// <summary>Gets the icon sourced from the DataContext.</summary>
  public object? Icon
  {
    get => GetValue(IconProperty);
    protected set => SetValue(IconProperty, value);
  }

  /// <summary>Gets whether the DataContext provides a non-null icon.</summary>
  public bool HasIcon
  {
    get => (bool)GetValue(HasIconProperty);
    protected set => SetValue(HasIconProperty, value);
  }

  /// <summary>Sets the item's tooltip from its title.</summary>
  private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    => ToolTipService.SetToolTip(d, e.NewValue);

  /// <summary>
  ///   Initializes a new instance of the <see cref="RouteTreeItem" /> class.
  /// </summary>
  public RouteTreeItem()
  {
    // WPF's Loaded broadcast only reaches elements with instance handlers
    // (class handlers alone do not arm BroadcastEventHelper) — the surface
    // bind must run from instance hooks.
    Loaded += OnSurfaceLoaded;
    Unloaded += OnSurfaceUnloaded;

    // The item states the width its row needs; the theme's compact trigger reads
    // the classification the framework writes onto this element (Responsive.IsCompact).
    Responsive.SetCompactBelow(this, RailWidth);
  }

  private void OnSurfaceLoaded(object sender, RoutedEventArgs e) => BindSurface();

  private void OnSurfaceUnloaded(object sender, RoutedEventArgs e) => UnbindSurface();

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

    if (e.Property == DataContextProperty)
    {
      Refresh();
    }
  }
}

