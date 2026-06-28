using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Everlong.Nester.RouteSync;

namespace Everlong.Nester.Controls;

/// <summary>
///   A navigation link whose DataContext is an <see cref="IRouteItem" />.
///   Activating the link issues the item's navigation request; the control
///   carries no command.
/// </summary>
[TemplatePart(Name = "PART_LinkButton", Type = typeof(Button))]
public partial class RouteLink : PContentControl
{
  /// <summary>
  ///   Identifies the <see cref="IsActive" /> dependency property.
  /// </summary>
  public static readonly DependencyProperty IsActiveProperty =
    DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(RouteLink),
                                new PropertyMetadata(false));

  /// <summary>
  ///   Identifies the <see cref="IsRouteHighlighted" /> dependency property.
  /// </summary>
  public static readonly DependencyProperty IsRouteHighlightedProperty =
    DependencyProperty.Register(nameof(IsRouteHighlighted), typeof(bool), typeof(RouteLink),
                                new PropertyMetadata(false,
                                  static (d, _) => ((RouteLink)d).RefreshEffective()));

  /// <summary>
  ///   Identifies the <see cref="IsHighlighted" /> dependency property.
  /// </summary>
  public static readonly DependencyProperty IsHighlightedProperty =
    DependencyProperty.Register(nameof(IsHighlighted), typeof(bool?), typeof(RouteLink),
                                new PropertyMetadata(null,
                                  static (d, _) => ((RouteLink)d).RefreshEffective()));

  /// <summary>
  ///   Identifies the <see cref="Title" /> dependency property.
  /// </summary>
  public static readonly DependencyProperty TitleProperty =
    DependencyProperty.Register(nameof(Title), typeof(string), typeof(RouteLink),
                                new PropertyMetadata(string.Empty));

  /// <summary>
  ///   Identifies the <see cref="Icon" /> dependency property.
  /// </summary>
  public static readonly DependencyProperty IconProperty =
    DependencyProperty.Register(nameof(Icon), typeof(object), typeof(RouteLink),
                                new PropertyMetadata(null));

  /// <summary>
  ///   Identifies the <see cref="HasIcon" /> dependency property.
  /// </summary>
  public static readonly DependencyProperty HasIconProperty =
    DependencyProperty.Register(nameof(HasIcon), typeof(bool), typeof(RouteLink),
                                new PropertyMetadata(false));

  /// <summary>
  ///   Identifies the <see cref="UseDefaultContent" /> dependency property.
  /// </summary>
  public static readonly DependencyProperty UseDefaultContentProperty =
    DependencyProperty.Register(nameof(UseDefaultContent), typeof(bool), typeof(RouteLink),
                                new PropertyMetadata(true));

  private Button? _linkButton;
  private bool _pointerPressed;

  static RouteLink()
  {
    DefaultStyleKeyProperty.OverrideMetadata(typeof(RouteLink),
      new FrameworkPropertyMetadata(typeof(RouteLink)));
  }

  /// <summary>
  ///   Gets the effective highlight — the app override when set, otherwise the
  ///   routing answer.
  /// </summary>
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

  /// <summary>Gets whether the link renders its own title and icon content — <see langword="false" /> once content is supplied.</summary>
  public bool UseDefaultContent
  {
    get => (bool)GetValue(UseDefaultContentProperty);
    protected set => SetValue(UseDefaultContentProperty, value);
  }

  /// <summary>
  ///   Initializes a new instance of the <see cref="RouteLink" /> class.
  /// </summary>
  public RouteLink()
  {
    // WPF's Loaded broadcast only reaches elements with instance handlers
    // (class handlers alone do not arm BroadcastEventHelper) — the surface
    // bind must run from instance hooks.
    Loaded += OnSurfaceLoaded;
    Unloaded += OnSurfaceUnloaded;
  }

  /// <inheritdoc />
  public override void OnApplyTemplate()
  {
    base.OnApplyTemplate();

    _linkButton?.Click -= OnLinkButtonClicked;

    _linkButton = GetTemplateChild("PART_LinkButton") as Button;
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
  protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
  {
    base.OnMouseLeftButtonDown(e);

    if (_linkButton is null)
    {
      _pointerPressed = true;
    }
  }

  /// <inheritdoc />
  protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
  {
    base.OnMouseLeftButtonUp(e);

    if (_linkButton is not null || !_pointerPressed)
    {
      return;
    }

    _pointerPressed = false;
    if (IsMouseOver)
    {
      HandleClickLogic();
    }
  }

  /// <inheritdoc />
  protected override void OnLostMouseCapture(MouseEventArgs e)
  {
    base.OnLostMouseCapture(e);
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
  protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
  {
    base.OnPropertyChanged(e);

    if (e.Property == DataContextProperty)
    {
      OnDataContextChanged();
    }
    else if (e.Property == ContentProperty)
    {
      OnContentChanged();
    }
  }

  private void OnLinkButtonClicked(object sender, RoutedEventArgs e)
    => HandleClickLogic();

  private void OnSurfaceLoaded(object sender, RoutedEventArgs e) => BindSurface();

  private void OnSurfaceUnloaded(object sender, RoutedEventArgs e) => UnbindSurface();
}
