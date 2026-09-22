using Avalonia;
using Avalonia.Controls;
namespace Everlong.Nester.Presentation;

/// <summary>
///   A panel that hosts page content within a layout and handles content transitions.
///   The named mount point (x:Name="Body") of a layout's nested content.
///   All visited page views remain in the Visual Tree; non-active pages are hidden with
///   <see cref="Avalonia.Visual.IsVisible"/> = false so inactive pages are fully removed from
///   layout, triggering a full remeasure cycle when they are reactivated.
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
    VerifyAccess();
    ClipToBounds = true;
    HorizontalAlignment = PHorizontalAlignment.Stretch;
    VerticalAlignment = PVerticalAlignment.Stretch;
  }

  /// <inheritdoc />
  protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
  {
    base.OnAttachedToVisualTree(e);
    _isAttached = true;
  }

  /// <inheritdoc />
  protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
  {
    base.OnDetachedFromVisualTree(e);
    _isAttached = false;
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

  void ILayoutBody<PControl>.SetVisible(bool isVisible) => IsVisible = isVisible;

  void ILayoutBody<PControl>.SettleActive()
  {
    IViewLocation<PControl>? active = _controller.Active;
    foreach (IViewLocation<PControl> node in _controller.Children)
    {
      if (node.View is PControl view)
        view.IsVisible = ReferenceEquals(node, active);
    }
  }
}
