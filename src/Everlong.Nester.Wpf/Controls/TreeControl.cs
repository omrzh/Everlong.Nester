using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Everlong.Nester.Controls;

partial class TreeControl : ItemsControl
{
  /// <summary>
  ///   Identifies the <see cref="ItemCommand" /> dependency property.
  /// </summary>
  public static readonly DependencyProperty ItemCommandProperty =
    DependencyProperty.Register(nameof(ItemCommand), typeof(ICommand), typeof(TreeControl),
                                new PropertyMetadata(null));

  static TreeControl()
  {
    DefaultStyleKeyProperty.OverrideMetadata(typeof(TreeControl),
      new FrameworkPropertyMetadata(typeof(TreeControl)));

    EventManager.RegisterClassHandler(typeof(TreeControl), TreeItem.ItemClickedEvent,
      new RoutedEventHandler(static (s, e) => ((TreeControl)s).OnChildItemClicked(e)));
  }

  /// <inheritdoc />
  protected override DependencyObject GetContainerForItemOverride()
    => new TreeItem();

  /// <inheritdoc />
  protected override bool IsItemItsOwnContainerOverride(object item)
    => item is TreeItem;

  private void OnChildItemClicked(RoutedEventArgs e)
  {
    if (e.OriginalSource is not TreeItem item || ItemCommand is not { } cmd)
      return;
    object? param = item.DataContext;
    if (cmd.CanExecute(param))
      cmd.Execute(param);
  }

}
