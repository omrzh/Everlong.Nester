using Avalonia;
using Avalonia.Controls;
using Everlong.Nester.Presentation;
namespace Everlong.Nester.Controls;

/// <summary>
///   A panel that hosts page content within a layout and handles content transitions.
///   The named mount point (x:Name="Body") of a layout's nested content.
///   All visited page views remain in the Visual Tree; non-active pages are hidden with
///   <see cref="Avalonia.Visual.IsVisible"/> = false so inactive pages are fully removed from
///   layout, triggering a full remeasure cycle when they are reactivated.
/// </summary>
public class LayoutBody : Panel, ILayoutBody
{

  private readonly LayoutBodyController<IViewLocation<PlatformControl>> _controller = new();

  bool ILayoutBody<PlatformControl>.IsAttachedToVisualTree => _isAttached;
  private volatile bool _isAttached;

  /// <summary>
  ///   Initializes a new instance of the <see cref="LayoutBody" /> class.
  /// </summary>
  public LayoutBody()
  {
    VerifyAccess();
    ClipToBounds = true;
    HorizontalAlignment = HorizontalAlignment.Stretch;
    VerticalAlignment = VerticalAlignment.Stretch;
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
  // LayoutBody does not manage visibility/opacity/hit-test.
  // SetActiveChild only updates the Active pointer; the visible/hidden state
  // is owned by the platform reveal stage (PlatformRevealStage — per-body
  // mount/activate/settle) and by the user's ISceneTransition director.

  private void ApplyChangeSet(LayoutBodyChangeSet<IViewLocation<PlatformControl>> cs)
  {
    if (cs.IsRefresh)
      return;

    foreach (IViewLocation<PlatformControl> node in cs.ToAdd)
    {
      if (node.View is { } child)
        Children.Add(child);
    }

    foreach (IViewLocation<PlatformControl> node in cs.ToRemove)
    {
      if (node.View is { } child)
        Children.Remove(child);
    }
  }

  IReadOnlyList<IViewLocation<PlatformControl>> ILayoutBody<PlatformControl>.Children
    => _controller.Children;

  IViewLocation<PlatformControl>? ILayoutBody<PlatformControl>.ActiveChild
    => _controller.Active;

  void ILayoutBody<PlatformControl>.SetActiveChild(IViewLocation<PlatformControl> node)
  {
    _controller.Switch(node);
  }

  void ILayoutBody<PlatformControl>.Add(IViewLocation<PlatformControl> node)
  {
    ApplyChangeSet(_controller.EnsureAdded(node));
  }

  void ILayoutBody<PlatformControl>.Remove(IViewLocation<PlatformControl> node)
  {
    ApplyChangeSet(_controller.Release(node));
  }

  void ILayoutBody<PlatformControl>.SetVisible(bool isVisible) => IsVisible = isVisible;

  void ILayoutBody<PlatformControl>.SettleActive()
  {
    IViewLocation<PlatformControl>? active = _controller.Active;
    foreach (IViewLocation<PlatformControl> node in _controller.Children)
    {
      if (node.View is PlatformControl view)
        view.IsVisible = ReferenceEquals(node, active);
    }
  }
}
