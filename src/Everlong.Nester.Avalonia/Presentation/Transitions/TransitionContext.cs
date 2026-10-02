// NOTE: Single-source file — the WPF project compiles this exact file via
// <Compile Include> in Everlong.Nester.Wpf.csproj.  Edit it here only;
// never create a WPF-side copy (the two builds would drift).

using Everlong.Nester.Layer;

namespace Everlong.Nester.Presentation;

/// <summary>
///   The snapshot of a scene transition — the change's moving heads, the
///   transition's kind and the layer it crosses to or from.
/// </summary>
/// <param name="FlyingCanvas">
///   The window's flying plane — where ghost visuals land, and the frame
///   <c>CaptureRelativeRect</c> and <c>TranslatePoint</c> measure in.
///   <see langword="null"/> outside page composition (notices).
/// </param>
/// <param name="Kind">The kind of this transition.</param>
public sealed partial record TransitionContext(
  FlyingCanvas? FlyingCanvas,
  TransitionKind Kind)
{
  /// <summary>
  ///   The arriving head — the moving side's outermost view.  An entering
  ///   head is laid out and invisible when the director runs; a head the
  ///   change re-engages in place is visible — it never left the
  ///   presentation.  <see langword="null"/> when nothing arrives.
  /// </summary>
  public PControl? Arriving { get; init; }

  /// <summary>
  ///   The departing head — the moving side's outermost departing view.
  ///   Visible when the director runs.  <see langword="null"/> on refresh
  ///   and on first presentation.
  /// </summary>
  public PControl? Departing { get; init; }

  /// <summary>
  ///   The other layer this transition crosses to or from — the layer the
  ///   transitioning router was derived from — or <see langword="null"/> when
  ///   the transition moves inside one layer.  A cross-layer director reads the
  ///   counterpart's interaction origin through it.
  /// </summary>
  public ILayerLease? Counterpart { get; init; }

  /// <summary>Whether this transition crosses between two layers rather than moving inside one.</summary>
  public bool IsCrossLayer => Counterpart is not null;

  /// <summary>
  ///   Reveals the arriving head — <c>Opacity</c> and hit-test only; layout
  ///   and coordinates are unchanged.
  /// </summary>
  public void ShowArriving()
  {
    if (Arriving is { } arriving)
    {
      arriving.Opacity = 1;
      arriving.IsHitTestVisible = true;
    }
  }

  /// <summary>
  ///   Hides the departing head — same <c>Opacity</c>-only contract as
  ///   <see cref="ShowArriving"/>.
  /// </summary>
  public void HideDeparting()
  {
    if (Departing is { } departing)
    {
      departing.Opacity = 0;
      departing.IsHitTestVisible = false;
    }
  }
}

/// <summary>
///   The direction of a view transition.
/// </summary>
public enum TransitionKind
{
  /// <summary>A view arrives.</summary>
  Enter = 0,

  /// <summary>The current view departs.</summary>
  Exit = 1,

  /// <summary>The current view is re-presented in place.</summary>
  Refresh = 2,

  /// <summary>The current view departs with no arriving view.</summary>
  Dismiss = 3
}
