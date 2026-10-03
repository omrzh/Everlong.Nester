// NOTE: Single-source file — the WPF project compiles this exact file via
// <Compile Include> in Everlong.Nester.Wpf.csproj.  Edit it here only;
// never create a WPF-side copy (the two builds would drift).

#if AVALONIA
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
#endif
using Everlong.Nester.Layer;

namespace Everlong.Nester.Presentation;

/// <summary>
///   The layer-focus half of the routing view — every surface competes for
///   the single foreground grant, and the surface is the one place with the
///   visual tree the element-focus save, restore and cross-layer anchor need.
/// </summary>
internal sealed partial class RoutingView
{
  /// <summary>The element focused inside this surface when it last gave up layer focus.</summary>
  private PInputElement? _elementFocus;

  /// <summary>The element the last interaction inside this surface rested on.</summary>
  private PInputElement? _origin;

  /// <summary>
  ///   Initializes a new instance of the <see cref="RoutingView" /> class,
  ///   which watches the two kinds of interaction it can see inside itself.
  /// </summary>
  internal RoutingView()
  {
    // An interaction is knowable when it happens, not when the foreground
    // moves: the command behind it may await, and the element focus at the
    // grant may already have moved on (see IFocusAnchor).  Two witnesses reach
    // this surface by bubbling — focus arriving on a control inside it, which
    // every focusable control raises, and an activation, which a control
    // raises itself and which therefore covers one that takes no focus.  The
    // later one wins: a click and the focus it takes are one interaction, and a
    // later interaction inside the layer is a newer origin than an earlier one.
    // handledEventsToo: an inner handler is free to consume either.
#if AVALONIA
    AddHandler(InputElement.GotFocusEvent,
               (_, _) => RecordFocusedElement(),
               RoutingStrategies.Bubble,
               handledEventsToo: true);
    AddHandler(Button.ClickEvent,
               (_, e) => RecordActivation(e.Source),
               RoutingStrategies.Bubble,
               handledEventsToo: true);
#else
    AddHandler(System.Windows.UIElement.GotFocusEvent,
               new System.Windows.RoutedEventHandler((_, _) => RecordFocusedElement()),
               handledEventsToo: true);
    AddHandler(System.Windows.Controls.Primitives.ButtonBase.ClickEvent,
               new System.Windows.RoutedEventHandler((_, e) => RecordActivation(e.Source)),
               handledEventsToo: true);
#endif
  }

  /// <inheritdoc />
  object? IFocusAnchor.Anchor => _origin ?? _elementFocus;

  /// <summary>Records the control a control inside this surface activated.</summary>
  private void RecordActivation(object? source)
  {
    if (source is PControl control && IsWithin(control))
      _origin = control;
  }

  /// <summary>Records the element focus that landed inside this surface.</summary>
  private void RecordFocusedElement()
  {
    if (FocusedElement() is { } focused && IsWithin(focused))
      _origin = focused;
  }

  /// <inheritdoc />
  bool IFocusableContent.TryFocus(LayerFocusContext context) => true;

  /// <inheritdoc />
  void IFocusableContent.OnFocusing(LayerFocusContext context)
  {
  }

  /// <inheritdoc />
  void IFocusableContent.OnFocused(LayerFocusContext context)
  {
    RestoreElementFocus();
    // The anchor lives exactly as long as the surface is in the background:
    // once the foreground is back, the origin it carried is spent.
    _origin = null;
    _elementFocus = null;
  }

  /// <inheritdoc />
  void IFocusableContent.OnUnfocusing(LayerFocusContext context) => CaptureElementFocus();

  /// <inheritdoc />
  void IFocusableContent.OnUnfocused(LayerFocusContext context)
  {
  }

  /// <summary>
  ///   Snapshots the element focus while this surface still holds it, so the
  ///   surface can put it back when it takes the foreground again, and so a
  ///   change the surface starts with an interaction it never saw — a
  ///   programmatic one, or focus that was already inside before this surface
  ///   watched — has an origin to hand over.  Focus that already moved outside
  ///   the surface is not this surface's to keep.
  /// </summary>
  private void CaptureElementFocus()
  {
    PInputElement? focused = FocusedElement();
    if (focused is not null && IsWithin(focused))
      _elementFocus = focused;
  }

  /// <summary>Returns element focus to the snapshot, when it is still there to take it.</summary>
  private void RestoreElementFocus()
  {
    if (_elementFocus is { } target && IsRestorable(target))
      SetFocusedElement(target);
  }

#if AVALONIA
  private PInputElement? FocusedElement()
    => TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();

  private bool IsWithin(PInputElement element)
    => element is PVisual visual && (ReferenceEquals(visual, this) || this.IsVisualAncestorOf(visual));

  private static bool IsRestorable(PInputElement element)
    => element is PVisual visual && visual.IsAttachedToVisualTree();

  private static void SetFocusedElement(PInputElement element) => element.Focus();
#else
  private static PInputElement? FocusedElement()
    => System.Windows.Input.Keyboard.FocusedElement;

  private bool IsWithin(PInputElement element)
    => element is System.Windows.DependencyObject node
       && (ReferenceEquals(node, this) || this.IsAncestorOf(node));

  private static bool IsRestorable(PInputElement element)
    => element is System.Windows.UIElement visual && visual.IsVisible
       && System.Windows.PresentationSource.FromVisual(visual) is not null;

  private static void SetFocusedElement(PInputElement element)
    => System.Windows.Input.Keyboard.Focus(element);
#endif
}
