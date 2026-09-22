// NOTE: Single-source file — the WPF project compiles this exact file via
// <Compile Include> in Everlong.Nester.Wpf.csproj.  Edit it here only;
// never create a WPF-side copy (the two builds would drift).

using Everlong.Nester.Routing;

namespace Everlong.Nester.Presentation;

internal sealed partial class RoutingView
{
  /// <summary>
  ///   The node's mount point, resolved once on first use — a node that never
  ///   hosts a child never pays for the lookup, and one that only later becomes
  ///   a container resolves then.  A settled <see langword="null" /> answer is
  ///   held: the view declares no mount point.
  /// </summary>
  /// <remarks>
  ///   A control that can host a chain node below it implements
  ///   <see cref="IBodyHolder" />; that contract is the only source for a
  ///   mount point.
  /// </remarks>
  private static IBodyPanel<PControl>? BodyOf(PlatformLocation location)
  {
    if (!location.BodyResolved)
    {
      location.Body = location.View is IBodyHolder holder
                        ? holder.GetBodyPanel()
                        : null;
      location.BodyResolved = true;
    }

    return location.Body;
  }
}
