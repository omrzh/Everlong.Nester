// NOTE: Single-source file — the WPF project compiles this exact file via
// <Compile Include> in Everlong.Nester.Wpf.csproj.  Edit it here only;
// never create a WPF-side copy (the two builds would drift).

namespace Everlong.Nester.Presentation;

/// <summary>
///   Steps one level down a moving side of a scene change, through the body
///   panels the chain nodes expose.
/// </summary>
/// <remarks>
///   A delegating director hands its transition to the node below it in the
///   tree.  The arriving side descends through the body's active child; the
///   departing side descends through the child that is not active, because a
///   body the change mounted an arriving child into points its active child
///   at the arriving node, and the departing node stays a non-active
///   sibling.  A body no arriving child took — a dismissal, a departing
///   shell — still holds its departing node as the active child, and that
///   node is the answer.
/// </remarks>
internal static class TransitionTree
{
  /// <summary>
  ///   The arriving node below <paramref name="view"/> — its body's active
  ///   child — or <see langword="null"/> when the view exposes no body or
  ///   the body has no active child.
  /// </summary>
  public static PControl? ArrivingBelow(PControl view)
    => BodyOf(view)?.ActiveChild?.View;

  /// <summary>
  ///   The departing node below <paramref name="view"/> — the body child that
  ///   is not active, or the active child when the body holds no other — or
  ///   <see langword="null"/> when the view exposes no body or the body is
  ///   empty.
  /// </summary>
  public static PControl? DepartingBelow(PControl view)
  {
    if (BodyOf(view) is not { } body)
      return null;

    IViewLocation<PControl>? active = body.ActiveChild;
    foreach (IViewLocation<PControl> node in body.Children)
      if (!ReferenceEquals(node, active) && node.View is { } departing)
        return departing;

    return active?.View;
  }

  private static IBodyPanel<PControl>? BodyOf(PControl view)
    => view is IBodyHolder holder ? holder.GetBodyPanel() : null;
}
