using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Everlong.Nester.Presentation;

/// <summary>
///   A general-purpose hierarchical item control equivalent to the platform <see cref="TreeViewItem" />
///   but without selection semantics. Supports <see cref="HierarchicalDataTemplate" /> for recursive binding.
/// </summary>
public partial class TreeItem : HeaderedItemsControl
{
  /// <summary>
  ///   Identifies the <see cref="ItemClicked" /> event.
  /// </summary>
  public static readonly RoutedEvent ItemClickedEvent =
    EventManager.RegisterRoutedEvent(nameof(ItemClicked), RoutingStrategy.Bubble,
                                     typeof(RoutedEventHandler), typeof(TreeItem));

  /// <summary>
  ///   Identifies the <see cref="IsExpanded" /> dependency property.
  /// </summary>
  public static readonly DependencyProperty IsExpandedProperty =
    DependencyProperty.Register(nameof(IsExpanded), typeof(bool), typeof(TreeItem),
                                new PropertyMetadata(false));

  /// <summary>
  ///   Identifies the <see cref="Command" /> dependency property.
  /// </summary>
  public static readonly DependencyProperty CommandProperty =
    DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(TreeItem),
                                new PropertyMetadata(null));

  /// <summary>
  ///   Identifies the <see cref="CommandParameter" /> dependency property.
  /// </summary>
  public static readonly DependencyProperty CommandParameterProperty =
    DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(TreeItem),
                                new PropertyMetadata(null));

  private Button? _headerButton;
  private Button? _expandButton;

  static TreeItem()
  {
    DefaultStyleKeyProperty.OverrideMetadata(typeof(TreeItem),
      new FrameworkPropertyMetadata(typeof(TreeItem)));

    // A WPF Control is focusable and a tab stop by default, so the container
    // would take a Tab of its own ahead of the header button in its template.
    // The item is a container, not an affordance: staying focusable but out of
    // the tab order leaves its descendants reachable by one Tab.
    IsTabStopProperty.OverrideMetadata(typeof(TreeItem),
      new FrameworkPropertyMetadata(false));
  }

  /// <summary>Raised when the item header is clicked. Bubbles up the visual tree.</summary>
  public event RoutedEventHandler ItemClicked
  {
    add => AddHandler(ItemClickedEvent, value);
    remove => RemoveHandler(ItemClickedEvent, value);
  }

  /// <summary>Gets or sets whether this item's children are currently visible.</summary>
  public bool IsExpanded
  {
    get => (bool)GetValue(IsExpandedProperty);
    set => SetValue(IsExpandedProperty, value);
  }

  /// <summary>
  ///   Gets or sets the command to execute when the item header is clicked.
  ///   The command receives <see cref="CommandParameter" /> as its argument.
  /// </summary>
  public ICommand? Command
  {
    get => (ICommand?)GetValue(CommandProperty);
    set => SetValue(CommandProperty, value);
  }

  /// <summary>Gets or sets the parameter passed to <see cref="Command" /> when executed.</summary>
  public object? CommandParameter
  {
    get => GetValue(CommandParameterProperty);
    set => SetValue(CommandParameterProperty, value);
  }

  /// <inheritdoc />
  protected override DependencyObject GetContainerForItemOverride()
    => new TreeItem();

  /// <inheritdoc />
  protected override bool IsItemItsOwnContainerOverride(object item)
    => item is TreeItem;

  /// <inheritdoc />
  public override void OnApplyTemplate()
  {
    base.OnApplyTemplate();

    _headerButton?.Click -= OnHeaderButtonClicked;
    _expandButton?.Click -= OnExpandButtonClicked;

    _headerButton = GetTemplateChild("PART_HeaderButton") as Button;
    _headerButton?.Click += OnHeaderButtonClicked;

    _expandButton = GetTemplateChild("PART_ExpandButton") as Button;
    _expandButton?.Click += OnExpandButtonClicked;
  }

  private void OnHeaderButtonClicked(object sender, RoutedEventArgs e)
  {
    OnHeaderClicked();
    RaiseEvent(new RoutedEventArgs(ItemClickedEvent, this));
    OnItemClicked();
  }

  private void OnExpandButtonClicked(object sender, RoutedEventArgs e)
    => OnExpandClicked();
}
