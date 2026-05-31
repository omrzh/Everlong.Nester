using Avalonia.Controls;

namespace Everlong.Nester.Shell;

/// <summary>
///   Platform ZIndex accessor — the lease's z IS its anchored content
///   control's ZIndex (the physical fact behind the z-band model).  Neutral
///   helper: both the lease implementation (Shell domain) and the stage
///   panel (Presentation domain) use it; ZIndex is a platform property, not
///   a panel behavior.
/// </summary>
internal static class ZOrder
{
  internal static void Set(Control c, int z) => c.ZIndex = z;
  internal static int Get(Control c) => c.ZIndex;
}
