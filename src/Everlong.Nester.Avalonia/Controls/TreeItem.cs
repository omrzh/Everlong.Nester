using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.Templates;

namespace Everlong.Nester.Controls;

/// <summary>
///   A general-purpose hierarchical item control equivalent to the platform <see cref="TreeViewItem" />
///   but without selection semantics. Supports <see cref="TreeDataTemplate" /> for recursive binding.
/// </summary>
[TemplatePart("PART_HeaderButton", typeof(Button))]
[TemplatePart("PART_ExpandButton", typeof(Button))]
public partial class TreeItem : HeaderedItemsControl
{
  /// <summary>
  ///   Defines the <see cref="ItemClicked" /> event.
  /// </summary>
  public static readonly RoutedEvent<RoutedEventArgs> ItemClickedEvent =
    RoutedEvent.Register<TreeItem, RoutedEventArgs>(nameof(ItemClicked), RoutingStrategies.Bubble);

  /// <summary>
  ///   Defines the <see cref="IsExpanded" /> property.
  /// </summary>
  public static readonly StyledProperty<bool> IsExpandedProperty =
    AvaloniaProperty.Register<TreeItem, bool>(nameof(IsExpanded));

  /// <summary>
  ///   Defines the <see cref="Command" /> property.
  /// </summary>
  public static readonly StyledProperty<ICommand?> CommandProperty =
    AvaloniaProperty.Register<TreeItem, ICommand?>(nameof(Command));

  /// <summary>
  ///   Defines the <see cref="CommandParameter" /> property.
  /// </summary>
  public static readonly StyledProperty<object?> CommandParameterProperty =
    AvaloniaProperty.Register<TreeItem, object?>(nameof(CommandParameter));

  private Button? _headerButton;
  private Button? _expandButton;

  /// <summary>Raised when the item header is clicked. Bubbles up the visual tree.</summary>
  public event EventHandler<RoutedEventArgs> ItemClicked
  {
    add => AddHandler(ItemClickedEvent, value);
    remove => RemoveHandler(ItemClickedEvent, value);
  }

  /// <summary>Gets or sets whether this item's children are currently visible.</summary>
  public bool IsExpanded
  {
    get => GetValue(IsExpandedProperty);
    set => SetValue(IsExpandedProperty, value);
  }

  /// <summary>
  ///   Gets or sets the command to execute when the item header is clicked.
  ///   The command receives <see cref="CommandParameter" /> as its argument.
  /// </summary>
  public ICommand? Command
  {
    get => GetValue(CommandProperty);
    set => SetValue(CommandProperty, value);
  }

  /// <summary>Gets or sets the parameter passed to <see cref="Command" /> when executed.</summary>
  public object? CommandParameter
  {
    get => GetValue(CommandParameterProperty);
    set => SetValue(CommandParameterProperty, value);
  }

  /// <summary>Gets whether this item has any children.</summary>
  // Avalonia's ItemsControl exposes ItemCount but not HasItems.
  // Defined here so the shared OnHeaderClicked() can reference it uniformly.
  protected bool HasItems => ItemCount > 0;

  /// <inheritdoc />
  protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
    => new TreeItem();

  /// <inheritdoc />
  protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    => NeedsContainer<TreeItem>(item, out recycleKey);

  /// <inheritdoc />
  protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
  {
    base.OnApplyTemplate(e);

    _headerButton?.Click -= OnHeaderButtonClicked;
    _expandButton?.Click -= OnExpandButtonClicked;

    _headerButton = e.NameScope.Find<Button>("PART_HeaderButton");
    _headerButton?.Click += OnHeaderButtonClicked;

    _expandButton = e.NameScope.Find<Button>("PART_ExpandButton");
    _expandButton?.Click += OnExpandButtonClicked;
  }

  private void OnHeaderButtonClicked(object? sender, RoutedEventArgs e)
  {
    OnHeaderClicked();
    RaiseEvent(new RoutedEventArgs(ItemClickedEvent, this));
    OnItemClicked();
  }

  private void OnExpandButtonClicked(object? sender, RoutedEventArgs e)
    => OnExpandClicked();
}
