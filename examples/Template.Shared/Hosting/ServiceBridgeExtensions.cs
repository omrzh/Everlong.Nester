using Everlong.DI;
using Everlong.Nester.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace NesterApp;

/// <summary>
///   Bridging the OPTIONAL process-level container
///   (<c>AppLifetimeOptions.Services</c>) into a window's own
///   container — the explicit cross-window sharing pattern: every window
///   still owns its provider; the bridged instances are shared references.
///   Missing process container / missing service = fail fast (never silent).
/// </summary>
public static class ServiceBridgeExtensions
{
  /// <summary>
  ///   Bridges the process-level <typeparamref name="T" /> as a singleton of
  ///   THIS window's container (the shared instance is resolved once and
  ///   registered by reference — the process container owns its lifetime).
  ///   Fails fast when no process container is configured
  ///   (<c>AppLifetimeOptions.Services</c>) or the service is not
  ///   registered there.  Call BEFORE the window's own registrations that
  ///   would TryAdd the same service (first-registration wins).
  /// </summary>
  public static IServiceCollection BridgeSingleton<T>(this IServiceCollection services, IServiceProvider? sp = null)
    where T : class
    => BridgeSingleton(services, typeof(T), sp);

  /// <summary>
  ///   Type-based overload — for services discovered by type (e.g. from a
  ///   generated registrar) instead of a compile-time generic.
  /// </summary>
  public static IServiceCollection BridgeSingleton(this IServiceCollection services, Type type, IServiceProvider? sp = null)
  {
    sp ??= AppLifetime.Current?.Services;
    if (sp is null)
    {
      throw new InvalidOperationException(
        "BridgeSingleton requires the process-level container — configure AppLifetimeOptions.Services " +
        "(or pass the process provider explicitly).");
    }

    object instance = sp.GetRequiredService(type);

    // An Everlong.DI IInjectable instance comes out of a plain provider
    // UNINJECTED (member injection needs an explicit Inject call) — complete
    // it here so window-side resolutions see a whole object.  Its members
    // resolve from the process container (the shared instance's container is
    // process-level).
    if (instance is IInjectable injectable)
      injectable.Inject(sp);

    services.AddSingleton(type, instance);
    return services;
  }
}
