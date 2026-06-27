// NOTE: Single-source file — the Extensions WPF project compiles this exact
// file via <Compile Include> in Everlong.Nester.Extensions.Wpf.csproj.
// Edit it here only; never create a WPF-side copy (the two builds would drift).

namespace Everlong.Nester.Presentation;

/// <summary>
///   The literal visual direction of an element's slide movement: the value
///   names the movement direction, so entering and exiting with the same
///   value produce the same motion.  A symmetric return ("back where it came
///   from") uses the opposite value on exit.
/// </summary>
public enum SlideDirection
{
  /// <summary>Moves upward — entering starts below the natural position.</summary>
  BottomToTop,

  /// <summary>Moves downward — entering starts above the natural position.</summary>
  TopToBottom,

  /// <summary>Moves rightward — entering starts left of the natural position.</summary>
  LeftToRight,

  /// <summary>Moves leftward — entering starts right of the natural position.</summary>
  RightToLeft,
}
