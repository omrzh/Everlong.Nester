using System.Windows;
using System.Windows.Controls;

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
  internal static void Set(FrameworkElement c, int z) => Canvas.SetZIndex(c, z);
  internal static int Get(FrameworkElement c) => Canvas.GetZIndex(c);
}
