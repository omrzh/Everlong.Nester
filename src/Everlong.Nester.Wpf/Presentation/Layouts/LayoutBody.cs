using System.Windows;
using System.Windows.Controls;

namespace Everlong.Nester.Presentation;

/// <summary>
///   A panel that hosts page content within a layout and handles content transitions.
///   The named mount point (x:Name="Body") of a layout's nested content.
///   All visited page views remain in the Visual Tree; non-active pages are hidden with
///   <see cref="UIElement.Visibility"/> = Collapsed so they are fully removed from layout,
///   triggering a full remeasure cycle when they are reactivated.
/// </summary>
public class LayoutBody : Panel, ILayoutBody
{
  private readonly LayoutBodyController<IViewLocation<PControl>> _controller = new();

  bool ILayoutBody<PControl>.IsAttachedToVisualTree => _isAttached;
  private volatile bool _isAttached;

  /// <summary>
  ///   Initializes a new instance of the <see cref="LayoutBody" /> class.
  /// </summary>
  public LayoutBody()
  {
    ClipToBounds = true;
    HorizontalAlignment = PHorizontalAlignment.Stretch;
    VerticalAlignment = PVerticalAlignment.Stretch;
    PresentationSource.AddSourceChangedHandler(this, OnSourceChanged);
  }

  // 1. Measure: each child measures at full constraint, the panel's desired size
  //    is the maximum width/height across all children.
  /// <summary>Measures every child at the full constraint and returns the largest desired size across them.</summary>
  protected override Size MeasureOverride(Size constraint)
  {
    double maxWidth = 0;
    double maxHeight = 0;

    foreach (UIElement child in InternalChildren)
    {
      if (child == null)
        continue;

      // Each child measures against the full available space.
      child.Measure(constraint);

      // Track the maximum desired size across children.
      maxWidth = Math.Max(maxWidth, child.DesiredSize.Width);
      maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
    }

    return new Size(maxWidth, maxHeight);
  }

  // 2. Arrange: every child fills the entire available area.
  /// <summary>Arranges every child across the whole area; alignment stays the child's own.</summary>
  protected override Size ArrangeOverride(Size arrangeSize)
  {
    PRect finalRect = new(arrangeSize);

    foreach (UIElement child in InternalChildren)
    {
      // Each child occupies the full rect; alignment is driven by the
      // child's own HorizontalAlignment / VerticalAlignment.
      child?.Arrange(finalRect);
    }

    return arrangeSize;
  }


  private void OnSourceChanged(object? sender, SourceChangedEventArgs e)
  {
    _isAttached = e.NewSource is not null;
  }

  // ── Change-set application ───────────────────────────────────────────────
  //
  // LayoutBody never touches opacity, hit-test, transform or z-index, and it
  // does not decide visibility on its own: SetActiveChild only updates the
  // Active pointer, and the visible/hidden state is written by SettleActive
  // when the platform reveal asks for it (per-body mount/activate/settle) —
  // plus whatever the user's ISceneTransition director does.

  private void ApplyChangeSet(LayoutBodyChangeSet<IViewLocation<PControl>> cs)
  {
    if (cs.IsRefresh)
      return;

    foreach (IViewLocation<PControl> node in cs.ToAdd)
    {
      if (node.View is { } child)
        Children.Add(child);
    }

    foreach (IViewLocation<PControl> node in cs.ToRemove)
    {
      if (node.View is { } child)
        Children.Remove(child);
    }
  }

  IReadOnlyList<IViewLocation<PControl>> ILayoutBody<PControl>.Children
    => _controller.Children;

  IViewLocation<PControl>? ILayoutBody<PControl>.ActiveChild
    => _controller.Active;

  void ILayoutBody<PControl>.SetActiveChild(IViewLocation<PControl> node)
  {
    _controller.Switch(node);
  }

  void ILayoutBody<PControl>.Add(IViewLocation<PControl> node)
  {
    ApplyChangeSet(_controller.EnsureAdded(node));
  }

  void ILayoutBody<PControl>.Remove(IViewLocation<PControl> node)
  {
    ApplyChangeSet(_controller.Release(node));
  }

  void ILayoutBody<PControl>.SetVisible(bool isVisible) =>
    Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;

  void ILayoutBody<PControl>.SettleActive()
  {
    IViewLocation<PControl>? active = _controller.Active;
    foreach (IViewLocation<PControl> node in _controller.Children)
    {
      if (node.View is { } view)
        view.Visibility = ReferenceEquals(node, active) ? Visibility.Visible : Visibility.Collapsed;
    }
  }
}
