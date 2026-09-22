using Terminal.Gui.ViewBase;

namespace Everlong.Nester.Presentation;

/// <summary>
///   The body panel of the Terminal.Gui surface — a <see cref="View" /> the
///   routing stages mount chain views into; only the active child is
///   visible.
/// </summary>
public class TuiBodyPanel : View, IBodyPanel
{
  private readonly BodyPanelController<IViewLocation<View>> _controller = new();

  /// <summary>
  ///   Initializes a new instance of the <see cref="TuiBodyPanel" /> class.
  /// </summary>
  public TuiBodyPanel()
  {
    Width = Dim.Fill();
    Height = Dim.Fill();
  }

  // ── Change-set application ───────────────────────────────────────────────

  private void ApplyChangeSet(BodyPanelChangeSet<IViewLocation<View>> cs)
  {
    foreach (IViewLocation<View> node in cs.ToAdd)
      if (node.View is { } view)
        AddChild(view);

    foreach (IViewLocation<View> node in cs.ToRemove)
      if (node.View is { } view)
        RemoveChild(view);
  }

  private void AddChild(View view)
  {
    if (view.SuperView is null)
      Add(view);
    if (view is not NesterView { StretchToBody: false })
      SizeToFill(view);
  }

  private void RemoveChild(View view)
  {
    if (ReferenceEquals(view.SuperView, this))
      Remove(view);
  }

  /// <summary>Mounts every chain view at full size — content is laid out by the pages themselves.</summary>
  private static void SizeToFill(View view)
  {
    view.X = 0;
    view.Y = 0;
    view.Width = Dim.Fill();
    view.Height = Dim.Fill();
  }

  // ── IBodyPanel<View> ────────────────────────────────────────────────────

  IReadOnlyList<IViewLocation<View>> IBodyPanel<View>.Children => _controller.Children;

  IViewLocation<View>? IBodyPanel<View>.ActiveChild => _controller.Active;

  bool IBodyPanel<View>.IsAttachedToVisualTree => SuperView is not null;

  void IBodyPanel<View>.SetActiveChild(IViewLocation<View> node)
  {
    _controller.Switch(node);
  }

  void IBodyPanel<View>.Add(IViewLocation<View> node)
  {
    ApplyChangeSet(_controller.EnsureAdded(node));
  }

  void IBodyPanel<View>.Remove(IViewLocation<View> node)
  {
    ApplyChangeSet(_controller.Release(node));
  }

  void IBodyPanel<View>.SetVisible(bool isVisible) => Visible = isVisible;

  void IBodyPanel<View>.SettleActive()
  {
    IViewLocation<View>? active = _controller.Active;
    foreach (IViewLocation<View> node in _controller.Children)
    {
      if (node.View is View view)
        view.Visible = ReferenceEquals(node, active);
    }
  }
}
