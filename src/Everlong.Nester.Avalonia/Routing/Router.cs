// NOTE: Single-source file — the WPF project compiles this exact file via
// <Compile Include> in Everlong.Nester.Wpf.csproj.  Edit it here only;
// never create a WPF-side copy (the two builds would drift).
using Everlong.Nester.Presentation;

namespace Everlong.Nester.Routing;

/// <summary>
///   The platform's router — its routing view is a
///   <see cref="RoutingView" /> that assembles the resolved chain into
///   real views and switches them per body at the reveal.
/// </summary>
public class Router(IServiceProvider services) : RouterBase(services, new PlatformStackModel());
