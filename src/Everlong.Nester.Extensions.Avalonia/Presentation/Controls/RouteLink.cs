using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Everlong.Nester.RouteSync;

namespace Everlong.Nester.Presentation;

/// <summary>
///   A navigation link whose DataContext is an <see cref="IRouteItem" />.
///   Activating the link issues the item's navigation request; the control
///   carries no command.
/// </summary>
[TemplatePart("PART_LinkButton", typeof(Button))]
public partial class RouteLink : PContentControl
{
  /// <summary>
  ///   Defines the <see cref="IsActive" /> property.
  /// </summary>
  public static readonly StyledProperty<bool> IsActiveProperty =
    AvaloniaProperty.Register<RouteLink, bool>(nameof(IsActive));

  /// <summary>
  ///   Defines the <see cref="IsRouteHighlighted" /> property.
  /// </summary>
  public static readonly StyledProperty<bool> IsRouteHighlightedProperty =
    AvaloniaProperty.Register<RouteLink, bool>(nameof(IsRouteHighlighted));

  /// <summary>
  ///   Defines the <see cref="IsHighlighted" /> property.
  /// </summary>
  public static readonly StyledProperty<bool?> IsHighlightedProperty =
    AvaloniaProperty.Register<RouteLink, bool?>(nameof(IsHighlighted));

  /// <summary>
  ///   Defines the <see cref="Title" /> property.
  /// </summary>
  public static readonly StyledProperty<string> TitleProperty =
    AvaloniaProperty.Register<RouteLink, string>(nameof(Title), string.Empty);

  /// <summary>
  ///   Defines the <see cref="Icon" /> property.
  /// </summary>
  public static readonly StyledProperty<object?> IconProperty =
    AvaloniaProperty.Register<RouteLink, object?>(nameof(Icon));

  /// <summary>
  ///   Defines the <see cref="HasIcon" /> property.
  /// </summary>
  public static readonly StyledProperty<bool> HasIconProperty =
    AvaloniaProperty.Register<RouteLink, bool>(nameof(HasIcon));

  /// <summary>
  ///   Defines the <see cref="UseDefaultContent" /> property.
  /// </summary>
  public static readonly StyledProperty<bool> UseDefaultContentProperty =
    AvaloniaProperty.Register<RouteLink, bool>(nameof(UseDefaultContent), true);

  private Button? _linkButton;
  private bool _pointerPressed;

  static RouteLink()
  {
    DataContextProperty.Changed.AddClassHandler<RouteLink>((link, _) => link.OnDataContextChanged());
    ContentProperty.Changed.AddClassHandler<RouteLink>((link, _) => link.OnContentChanged());
    IsRouteHighlightedProperty.Changed.AddClassHandler<RouteLink>((link, _) => link.RefreshEffective());
    IsHighlightedProperty.Changed.AddClassHandler<RouteLink>((link, _) => link.RefreshEffective());
  }

  /// <summary>
  ///   Gets the effective highlight — the app override when set, otherwise the
  ///   routing answer.
  /// </summary>
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

  /// <summary>Gets whether the link renders its own title and icon content — <see langword="false" /> once content is supplied.</summary>
  public bool UseDefaultContent
  {
    get => GetValue(UseDefaultContentProperty);
    protected set => SetValue(UseDefaultContentProperty, value);
  }

  /// <inheritdoc />
  protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
  {
    base.OnApplyTemplate(e);

    _linkButton?.Click -= OnLinkButtonClicked;

    _linkButton = e.NameScope.Find<Button>("PART_LinkButton");
    if (_linkButton is not null)
    {
      _linkButton.Click += OnLinkButtonClicked;
    }
    else
    {
      // No template button: the link handles its own pointer and keyboard
      // activation, so it must be reachable by keyboard.
      SetCurrentValue(FocusableProperty, true);
    }
  }

  /// <inheritdoc />
  protected override void OnPointerPressed(PointerPressedEventArgs e)
  {
    base.OnPointerPressed(e);

    if (_linkButton is null && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
    {
      _pointerPressed = true;
    }
  }

  /// <inheritdoc />
  protected override void OnPointerReleased(PointerReleasedEventArgs e)
  {
    base.OnPointerReleased(e);

    if (_linkButton is not null || !_pointerPressed)
    {
      return;
    }

    _pointerPressed = false;
    if (e.InitialPressMouseButton == MouseButton.Left && Bounds.Contains(e.GetPosition(this)))
    {
      HandleClickLogic();
    }
  }

  /// <inheritdoc />
  protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
  {
    base.OnPointerCaptureLost(e);
    _pointerPressed = false;
  }

  /// <inheritdoc />
  protected override void OnKeyDown(KeyEventArgs e)
  {
    base.OnKeyDown(e);

    if (_linkButton is not null)
    {
      return;
    }

    if (e.Key is Key.Enter or Key.Space)
    {
      e.Handled = true;
      HandleClickLogic();
    }
  }

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

  private void OnLinkButtonClicked(object? sender, RoutedEventArgs e)
    => HandleClickLogic();
}
