using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Windows.Input;

namespace Everlong.Nester.Presentation;

partial class TreeControl : ItemsControl
{
  /// <summary>
  ///   Defines the <see cref="ItemCommand" /> property.
  /// </summary>
  public static readonly StyledProperty<ICommand?> ItemCommandProperty =
    AvaloniaProperty.Register<TreeControl, ICommand?>(nameof(ItemCommand));

  /// <summary>
  ///   Initializes a new instance of the <see cref="TreeControl" /> class.
  /// </summary>
  public TreeControl()
  {
    AddHandler(TreeItem.ItemClickedEvent, OnItemClickedHandler);
  }

  /// <inheritdoc />
  protected override PControl CreateContainerForItemOverride(object? item, int index, object? recycleKey)
    => new TreeItem();

  /// <inheritdoc />
  protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    => NeedsContainer<TreeItem>(item, out recycleKey);

  private void OnChildItemClicked(RoutedEventArgs e)
  {
    if (e.Source is not TreeItem item || ItemCommand is not { } cmd)
      return;
    object? param = item.DataContext;
    if (cmd.CanExecute(param))
      cmd.Execute(param);
  }

  private void OnItemClickedHandler(object? sender, RoutedEventArgs e) => OnChildItemClicked(e);
}
