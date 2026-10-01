// NOTE: Single-source file — the WPF project compiles this exact file via
// <Compile Include> in Everlong.Nester.Wpf.csproj.  Edit it here only;
// never create a WPF-side copy (the two builds would drift).

namespace Everlong.Nester.Presentation;

/// <summary>
///   Whether a transition moves inside one layer or crosses between two —
///   the distinction a director keys its choreography on.
/// </summary>
public enum TransitionScope
{
  /// <summary>
  ///   Source and destination share the layer: the transition's own chains
  ///   carry both sides, and no other layer takes part.
  /// </summary>
  InLayer = 0,

  /// <summary>
  ///   Source and destination sit in different layers: the counterpart is the
  ///   layer the transition crosses to or from, and it outlives the change.
  /// </summary>
  CrossLayer = 1,
}
