using Avalonia;
using Avalonia.Interactivity;

namespace Everlong.Nester.Controls;

public partial class RouteTreeControl : TreeControl
{
  static RouteTreeControl()
  {
    ItemsSourceProperty.Changed.AddClassHandler<RouteTreeControl>(
      static (control, _) => control.RecomputeHighlights());
  }

  /// <summary>
  ///   Initializes a new instance of the <see cref="RouteTreeControl" /> class.
  /// </summary>
  public RouteTreeControl()
  {
    AddHandler(TreeItem.ItemClickedEvent, OnNavItemClickedHandler);
  }

  /// <inheritdoc />
  protected override PControl CreateContainerForItemOverride(object? item, int index, object? recycleKey)
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

  private void OnNavItemClickedHandler(object? sender, RoutedEventArgs e)
  {
    if (e.Source is TreeItem item)
      OnChildItemClicked(item);
  }
}
