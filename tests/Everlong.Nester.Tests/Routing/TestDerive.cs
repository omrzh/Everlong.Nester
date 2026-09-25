using Everlong.Nester.Layer;
using Everlong.Nester.Routing;

namespace Everlong.Nester.Tests;

/// <summary>
///   The overlay shape the routing suites exercise — a derived router at the
///   navigation band with no default layouts.
/// </summary>
internal static class TestDeriveExtensions
{
  /// <summary>Derives a navigable overlay at the navigation band.</summary>
  internal static IRouter Derive(this IRouter router)
    => router.Derive(new DeriveOptions { Band = KnownLayers.Navigation });
}
