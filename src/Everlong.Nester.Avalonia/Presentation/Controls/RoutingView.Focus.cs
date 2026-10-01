// NOTE: Single-source file — the WPF project compiles this exact file via
// <Compile Include> in Everlong.Nester.Wpf.csproj.  Edit it here only;
// never create a WPF-side copy (the two builds would drift).

#if AVALONIA
using Avalonia.Controls;
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

  /// <inheritdoc />
  object? IFocusAnchor.Anchor => _elementFocus;

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
  ///   surface can put it back when it takes the foreground again.  Focus
  ///   that already moved outside the surface is not this surface's to keep.
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
