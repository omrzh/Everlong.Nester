using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
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
[TemplatePart("PART_HeaderButton", typeof(Button))]
public partial class RouteTreeItem : TreeItem
{
  /// <summary>
  ///   Initializes a new instance of the <see cref="RouteTreeItem" /> class.
  /// </summary>
  public RouteTreeItem()
  {
    Responsive.SetCompactBelow(this, RailWidth);
  }

  /// <summary>
  ///   Defines the <see cref="IsCollapsible" /> property.
  /// </summary>
  public static readonly StyledProperty<bool> IsCollapsibleProperty =
    AvaloniaProperty.Register<RouteTreeItem, bool>(nameof(IsCollapsible), defaultValue: true);

  /// <summary>
  ///   Defines the <see cref="IsActive" /> property.
  /// </summary>
  public static readonly StyledProperty<bool> IsActiveProperty =
    AvaloniaProperty.Register<RouteTreeItem, bool>(nameof(IsActive));

  /// <summary>
  ///   Defines the <see cref="IsRouteHighlighted" /> property.
  /// </summary>
  public static readonly StyledProperty<bool> IsRouteHighlightedProperty =
    AvaloniaProperty.Register<RouteTreeItem, bool>(nameof(IsRouteHighlighted));

  /// <summary>
  ///   Defines the <see cref="IsHighlighted" /> property.
  /// </summary>
  public static readonly StyledProperty<bool?> IsHighlightedProperty =
    AvaloniaProperty.Register<RouteTreeItem, bool?>(nameof(IsHighlighted));

  /// <summary>
  ///   Defines the <see cref="HasHighlightedDescendant" /> property.
  /// </summary>
  public static readonly StyledProperty<bool> HasHighlightedDescendantProperty =
    AvaloniaProperty.Register<RouteTreeItem, bool>(nameof(HasHighlightedDescendant));

  /// <summary>
  ///   Defines the <see cref="Title" /> property.
  /// </summary>
  public static readonly StyledProperty<string> TitleProperty =
    AvaloniaProperty.Register<RouteTreeItem, string>(nameof(Title), string.Empty);

  /// <summary>
  ///   Defines the <see cref="Icon" /> property.
  /// </summary>
  public static readonly StyledProperty<object?> IconProperty =
    AvaloniaProperty.Register<RouteTreeItem, object?>(nameof(Icon));

  /// <summary>
  ///   Defines the <see cref="HasIcon" /> property.
  /// </summary>
  public static readonly StyledProperty<bool> HasIconProperty =
    AvaloniaProperty.Register<RouteTreeItem, bool>(nameof(HasIcon));

  static RouteTreeItem()
  {
    DataContextProperty.Changed.AddClassHandler<RouteTreeItem>((item, _) => item.Refresh());
    IsRouteHighlightedProperty.Changed.AddClassHandler<RouteTreeItem>((item, _) => item.RefreshEffective());
    IsHighlightedProperty.Changed.AddClassHandler<RouteTreeItem>((item, _) => item.RefreshEffective());

    // The item names itself — a rail entry carries no visible title, so the title
    // is the tooltip wherever the theme decides not to render it.
    TitleProperty.Changed.AddClassHandler<RouteTreeItem>((item, args) => ToolTip.SetTip(item, args.GetNewValue<string>()));
  }

  /// <summary>Gets or sets whether this item can be collapsed by clicking its header.</summary>
  public bool IsCollapsible
  {
    get => GetValue(IsCollapsibleProperty);
    set => SetValue(IsCollapsibleProperty, value);
  }

  /// <summary>Gets the effective highlight — the app override when set, otherwise the routing answer.</summary>
  public bool IsActive
  {
    get => GetValue(IsActiveProperty);
    protected set => SetValue(IsActiveProperty, value);
  }

  /// <summary>Gets whether the routing surface highlights this item.</summary>
  public bool IsRouteHighlighted
  {
    get => GetValue(IsRouteHighlightedProperty);
    protected set => SetValue(IsRouteHighlightedProperty, value);
  }

  /// <summary>
  ///   Gets or sets the app-supplied highlight override.
  ///   <see langword="null" /> follows <see cref="IsRouteHighlighted" />.
  /// </summary>
  public bool? IsHighlighted
  {
    get => GetValue(IsHighlightedProperty);
    set => SetValue(IsHighlightedProperty, value);
  }

  /// <summary>Gets whether any descendant of this group item matches the current route.</summary>
  public bool HasHighlightedDescendant
  {
    get => GetValue(HasHighlightedDescendantProperty);
    protected set => SetValue(HasHighlightedDescendantProperty, value);
  }

  /// <summary>Gets the display title sourced from the DataContext.</summary>
  public string Title
  {
    get => GetValue(TitleProperty);
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
    get => GetValue(HasIconProperty);
    protected set => SetValue(HasIconProperty, value);
  }

  /// <inheritdoc />
  protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
    => new RouteTreeItem();

  /// <inheritdoc />
  protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    => NeedsContainer<RouteTreeItem>(item, out recycleKey);

  /// <inheritdoc />
  protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
  {
    base.OnAttachedToVisualTree(e);
    BindSurface();
  }

  /// <inheritdoc />
  protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
  {
    base.OnDetachedFromVisualTree(e);
    UnbindSurface();
  }
}

