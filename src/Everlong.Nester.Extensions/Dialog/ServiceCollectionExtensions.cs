using Microsoft.Extensions.DependencyInjection;

namespace Everlong.Nester.Dialog;

/// <summary>
///   Extension methods for registering the built-in dialog domain into an
///   <see cref="IServiceCollection" />.
/// </summary>
public static class ServiceCollectionExtensions
{
  /// <summary>
  ///   Registers the dimmer chrome model for container resolution.
  /// </summary>
  public static IServiceCollection AddNesterDialog(this IServiceCollection services)
  {
    services.AddTransient<DefaultDimmerModel>();
    return services;
  }
}
