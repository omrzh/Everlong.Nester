using Everlong.DI;

namespace NesterApp;

/// <summary>
///   The project's <c>[ServiceRegistrar]</c>: the generated
///   <c>AddServices</c> collects every
///   <c>[Singleton&lt;T&gt;]/[Transient]/[Scoped&lt;T&gt;]</c> in the project.
///   Every window shell calls <c>services.AddServices(new AppServices())</c>
///   in its <c>Initialize</c>.  Auth is NOT here — it lives in the
///   process-level container (wired via <c>AppLifetimeOptions.Services</c>,
///   built inline in each platform App) and is bridged into every window by
///   the shells (<see cref="ServiceBridgeExtensions.BridgeSingleton{T}" />).
/// </summary>
[ServiceRegistrar]
internal partial class AppServices;
