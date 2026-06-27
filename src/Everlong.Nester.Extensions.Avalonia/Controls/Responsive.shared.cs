// NOTE: Single-source file — the Extensions.Wpf project compiles this exact
// file via <Compile Include> in Everlong.Nester.Extensions.Wpf.csproj.
// Edit it here only; never create a WPF-side copy (the two builds would drift).
namespace Everlong.Nester.Controls;

/// <summary>
///   The platform-neutral half of <see cref="Responsive" />: the rule both
///   platforms classify by, so the two cannot answer the same slot differently.
/// </summary>
public static partial class Responsive
{
  /// <summary>
  ///   Whether a slot of <paramref name="width" /> is classified compact under a
  ///   threshold of <paramref name="threshold" />: the threshold is live, the
  ///   element has been measured, and the slot is narrower than the threshold.
  /// </summary>
  internal static bool IsCompactSlot(double threshold, double width)
    => threshold > 0 && width > 0 && width < threshold;
}
