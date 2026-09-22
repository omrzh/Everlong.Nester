using Terminal.Gui.ViewBase;

namespace Everlong.Nester.Presentation;

/// <summary>
///   The layout body of the Terminal.Gui surface — a <see cref="View" /> the
///   routing stages mount chain views into; only the active child is
///   visible.
/// </summary>
public class TuiLayoutBody : View, ILayoutBody
{
  private readonly LayoutBodyController<IViewLocation<View>> _controller = new();

  /// <summary>
  ///   Initializes a new instance of the <see cref="TuiLayoutBody" /> class.
  /// </summary>
  public TuiLayoutBody()
  {
    Width = Dim.Fill();
    Height = Dim.Fill();
  }

  // ── Change-set application ───────────────────────────────────────────────

  private void ApplyChangeSet(LayoutBodyChangeSet<IViewLocation<View>> cs)
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

  // ── ILayoutBody<View> ────────────────────────────────────────────────────

  IReadOnlyList<IViewLocation<View>> ILayoutBody<View>.Children => _controller.Children;

  IViewLocation<View>? ILayoutBody<View>.ActiveChild => _controller.Active;

  bool ILayoutBody<View>.IsAttachedToVisualTree => SuperView is not null;

  void ILayoutBody<View>.SetActiveChild(IViewLocation<View> node)
  {
    _controller.Switch(node);
  }

  void ILayoutBody<View>.Add(IViewLocation<View> node)
  {
    ApplyChangeSet(_controller.EnsureAdded(node));
  }

  void ILayoutBody<View>.Remove(IViewLocation<View> node)
  {
    ApplyChangeSet(_controller.Release(node));
  }

  void ILayoutBody<View>.SetVisible(bool isVisible) => Visible = isVisible;

  void ILayoutBody<View>.SettleActive()
  {
    IViewLocation<View>? active = _controller.Active;
    foreach (IViewLocation<View> node in _controller.Children)
    {
      if (node.View is View view)
        view.Visible = ReferenceEquals(node, active);
    }
  }
}
