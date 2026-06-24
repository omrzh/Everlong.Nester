using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Everlong.Nester.Auth;

/// <summary>
///   Extension methods for registering Nester Auth services into an <see cref="IServiceCollection" />.
/// </summary>
public static class ServiceCollectionExtensions
{
  extension(IServiceCollection services)
  {
    /// <summary>
    ///   Configures authorization options, replacing any previously registered auth options.
    ///   Authorization handlers declared in <paramref name="configure" /> are registered in the container.
    /// </summary>
    /// <param name="configure">Optional action to configure <see cref="AuthorizationOptions" />.</param>
    public IServiceCollection AddNesterAuth(
      Action<AuthorizationOptions>? configure = null)
    {
      var options = new AuthorizationOptions();
      configure?.Invoke(options);
      services.RemoveAll<AuthorizationOptions>();
      services.AddSingleton(options);

      foreach (var descriptor in options.HandlerDescriptors)
      {
        services.TryAddEnumerable(descriptor);
      }

      services.TryAddSingleton<IAuthService, AuthService>();

      return services;
    }
  }
}
