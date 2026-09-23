using Avalonia.Controls;
namespace Everlong.Nester.Presentation;

/// <summary>
///   A panel that hosts page content within a layout and handles content transitions.
///   The mount point a view exposes through <see cref="IBodyHolder" />.
///   All visited page views remain in the Visual Tree; non-active pages are hidden with
///   <see cref="Avalonia.Visual.IsVisible"/> = false so inactive pages are fully removed from
///   layout, triggering a full remeasure cycle when they are reactivated.
/// </summary>
public class BodyPanel : Panel, IBodyPanel
{
  private readonly BodyPanelController<IViewLocation<PControl>> _controller = new();

  /// <summary>
  ///   Initializes a new instance of the <see cref="BodyPanel" /> class.
  /// </summary>
  public BodyPanel()
  {
    VerifyAccess();
    ClipToBounds = true;
    HorizontalAlignment = PHorizontalAlignment.Stretch;
    VerticalAlignment = PVerticalAlignment.Stretch;
  }

  // ── Change-set application ───────────────────────────────────────────────
  //
  // BodyPanel never touches opacity, hit-test, transform or z-index, and it
  // does not decide visibility on its own: SetActiveChild only updates the
  // Active pointer, and the visible/hidden state is written by SettleActive
  // when the platform reveal asks for it (per-body mount/activate/settle) —
  // plus whatever the user's ISceneTransition director does.

  private void ApplyChangeSet(BodyPanelChangeSet<IViewLocation<PControl>> cs)
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

  IReadOnlyList<IViewLocation<PControl>> IBodyPanel<PControl>.Children
    => _controller.Children;

  IViewLocation<PControl>? IBodyPanel<PControl>.ActiveChild
    => _controller.Active;

  void IBodyPanel<PControl>.SetActiveChild(IViewLocation<PControl> node)
  {
    _controller.Switch(node);
  }

  void IBodyPanel<PControl>.Add(IViewLocation<PControl> node)
  {
    ApplyChangeSet(_controller.EnsureAdded(node));
  }

  void IBodyPanel<PControl>.Remove(IViewLocation<PControl> node)
  {
    ApplyChangeSet(_controller.Release(node));
  }

  void IBodyPanel<PControl>.SetVisible(bool isVisible) => IsVisible = isVisible;

  void IBodyPanel<PControl>.SettleActive()
  {
    IViewLocation<PControl>? active = _controller.Active;
    foreach (IViewLocation<PControl> node in _controller.Children)
    {
      if (node.View is PControl view)
        view.IsVisible = ReferenceEquals(node, active);
    }
  }
}
