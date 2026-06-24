using Everlong.Nester.Routing;

namespace Everlong.Nester.Auth;

/// <summary>
///   Evaluates a route request against an <see cref="IAuthRegistry" /> and
///   an <see cref="IAuthService" />.
/// </summary>
public static class RouteAuth
{
  /// <summary>
  ///   Returns whether every chain node of <paramref name="location" />
  ///   that declares a requirement passes it for the current user of
  ///   <paramref name="authService" />.  A node that declares no
  ///   requirement never denies.
  /// </summary>
  /// <remarks>
  ///   Requirements are evaluated in chain order, outermost first; the
  ///   first failing node denies the request.
  /// </remarks>
  public static bool IsAuthorized(ILocator location, IAuthRegistry registry, IAuthService authService)
  {
    ArgumentNullException.ThrowIfNull(location);
    ArgumentNullException.ThrowIfNull(registry);
    ArgumentNullException.ThrowIfNull(authService);

    foreach (ITarget target in location.Path)
    {
      AuthDescriptor? descriptor = registry.GetDescriptor(target.Type);
      if (descriptor is null)
        continue;
      if (!authService.Authorize(descriptor.Roles, descriptor.Policy).Succeeded)
        return false;
    }

    return true;
  }
}
