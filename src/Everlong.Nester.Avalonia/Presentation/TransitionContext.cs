// NOTE: Single-source file — the WPF project compiles this exact file via
// <Compile Include> in Everlong.Nester.Wpf.csproj.  Edit it here only;
// never create a WPF-side copy (the two builds would drift).

namespace Everlong.Nester.Presentation;

/// <summary>
///   The snapshot of a scene transition — the change's view chains and
///   the transition's kind.
/// </summary>
/// <param name="FlyingCanvas">
///   The overlay canvas for ghost visuals; the coordinate reference for
///   <c>CaptureRelativeRect</c> and <c>TranslatePoint</c>.
///   <see langword="null"/> outside page composition (notices).
/// </param>
/// <param name="Kind">The kind of this transition.</param>
public sealed partial record TransitionContext(
  PCanvas FlyingCanvas,
  TransitionKind Kind)
{
  /// <summary>
  ///   The arriving views, outermost first — the fresh part of the change.
  ///   Laid out and invisible when the director runs.  Empty on dismiss.
  /// </summary>
  public IReadOnlyList<PControl> ArrivingChain { get; init; } = [];

  /// <summary>
  ///   The departing views, outermost first — the stale part of the change.
  ///   Visible when the director runs.  Empty on refresh and on first
  ///   presentation.
  /// </summary>
  public IReadOnlyList<PControl> DepartingChain { get; init; } = [];

  /// <summary>The outermost arriving view, or <see langword="null"/> when none arrives.</summary>
  public PControl? ArrivingHead => ArrivingChain.Count > 0 ? ArrivingChain[0] : null;

  /// <summary>The outermost departing view, or <see langword="null"/> when none departs.</summary>
  public PControl? DepartingHead => DepartingChain.Count > 0 ? DepartingChain[0] : null;

  /// <summary>
  ///   Hides the arriving chain — <c>Opacity</c> and hit-test only; layout
  ///   and coordinates are unchanged.
  /// </summary>
  public void HideArriving()
  {
    foreach (PControl c in ArrivingChain)
    {
      c.Opacity = 0;
      c.IsHitTestVisible = false;
    }
  }

  /// <summary>
  ///   Reveals the arriving chain — the counterpart of
  ///   <see cref="HideArriving"/>.
  /// </summary>
  public void ShowArriving()
  {
    foreach (PControl c in ArrivingChain)
    {
      c.Opacity = 1;
      c.IsHitTestVisible = true;
    }
  }

  /// <summary>
  ///   Hides the departing chain — same <c>Opacity</c>-only contract as
  ///   <see cref="HideArriving"/>.
  /// </summary>
  public void HideDeparting()
  {
    foreach (PControl c in DepartingChain)
    {
      c.Opacity = 0;
      c.IsHitTestVisible = false;
    }
  }

  /// <summary>
  ///   Reveals the arriving views before <paramref name="director"/> — the
  ///   outer frames the director does not animate.  No-op when
  ///   <paramref name="director"/> is not in <see cref="ArrivingChain"/> or
  ///   is its head.
  /// </summary>
  public void RevealBefore(PControl director)
  {
    int end = IndexOf(ArrivingChain, director);
    for (int i = 0; i < end; i++)
    {
      ArrivingChain[i].Opacity = 1;
      ArrivingChain[i].IsHitTestVisible = true;
    }
  }

  /// <summary>
  ///   The first view strictly after <paramref name="director"/> in the
  ///   change's own chain that implements <see cref="ISceneTransition"/>,
  ///   or <see langword="null"/> when there is none.
  /// </summary>
  public ISceneTransition? NextDirectorAfter(PControl director)
  {
    int start = IndexOf(CurrentChain, director) + 1;
    for (int i = start; i < CurrentChain.Count; i++)
      if (CurrentChain[i] is ISceneTransition next)
        return next;
    return null;
  }

  /// <summary>
  ///   The context scoped to the views below <paramref name="director"/>:
  ///   the change's own chain starts after the director; the other chain
  ///   starts at the director's level.  Returns the same context when
  ///   <paramref name="director"/> is not in the change's own chain.
  /// </summary>
  public TransitionContext ScopedFrom(PControl director)
  {
    int self = IndexOf(CurrentChain, director);
    if (self < 0)
      return this;

    bool exiting = Kind is TransitionKind.Exit or TransitionKind.Dismiss;
    return this with
    {
      ArrivingChain = exiting ? Slice(OtherChain, self) : Slice(CurrentChain, self + 1),
      DepartingChain = exiting ? Slice(CurrentChain, self + 1) : Slice(OtherChain, self),
    };
  }

  private IReadOnlyList<PControl> CurrentChain
    => Kind is TransitionKind.Exit or TransitionKind.Dismiss ? DepartingChain : ArrivingChain;

  private IReadOnlyList<PControl> OtherChain
    => Kind is TransitionKind.Exit or TransitionKind.Dismiss ? ArrivingChain : DepartingChain;

  private static int IndexOf(IReadOnlyList<PControl> chain, PControl view)
  {
    for (int i = 0; i < chain.Count; i++)
      if (ReferenceEquals(chain[i], view))
        return i;
    return -1;
  }

  private static IReadOnlyList<PControl> Slice(IReadOnlyList<PControl> chain, int from)
  {
    if (from >= chain.Count)
      return [];
    var list = new List<PControl>(chain.Count - from);
    for (int i = from; i < chain.Count; i++)
      list.Add(chain[i]);
    return list;
  }
}
