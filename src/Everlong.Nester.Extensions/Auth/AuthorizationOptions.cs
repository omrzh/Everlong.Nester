using Microsoft.Extensions.DependencyInjection;

namespace Everlong.Nester.Auth;

/// <summary>
///   Authorization service configuration options
/// </summary>
public class AuthorizationOptions
{
  private readonly Dictionary<string, AuthorizationPolicy> _policyMap = new(StringComparer.OrdinalIgnoreCase);

  /// <summary>
  ///   Cache expiration time (default 10 minutes)
  /// </summary>
  public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromMinutes(10);

  /// <summary>
  ///   Whether to enable caching (enabled by default)
  /// </summary>
  public bool EnableCache { get; set; } = true;

  /// <summary>
  ///   Gets or sets the default authorization policy.
  /// </summary>
  public AuthorizationPolicy DefaultPolicy { get; set; } = new PolicyBuilder().RequireAuthenticatedUser().Build();

  /// <summary>
  ///   Gets or sets the policy used when no policy is specified.
  /// </summary>
  public AuthorizationPolicy? FallbackPolicy { get; set; }

  /// <summary>
  ///   Gets the list of registered authorization handler types.
  /// </summary>
  public List<Type> Handlers { get; } = [];

  /// <summary>
  ///   Pre-built service descriptors for handler registrations; carries proper DAM annotations
  ///   so the AOT linker can preserve the required constructors.
  /// </summary>
  internal List<ServiceDescriptor> HandlerDescriptors { get; } = [];

  /// <summary>
  ///   Adds an authorization policy.
  /// </summary>
  /// <param name="name">The name of the policy.</param>
  /// <param name="policy">The authorization policy.</param>
  public void AddPolicy(string name, AuthorizationPolicy policy)
  {
    ArgumentNullException.ThrowIfNull(name);

    ArgumentNullException.ThrowIfNull(policy);

    _policyMap[name] = policy;
  }

  /// <summary>
  ///   Adds an authorization handler type.
  /// </summary>
  /// <typeparam name="T">The type of the handler.</typeparam>
  public void AddHandler<T>() where T : class, IAuthorizationHandler
  {
    Handlers.Add(typeof(T));
    HandlerDescriptors.Add(ServiceDescriptor.Singleton<IAuthorizationHandler, T>());
  }

  /// <summary>
  ///   Adds an authorization policy.
  /// </summary>
  /// <param name="name">The name of the policy.</param>
  /// <param name="configurePolicy">The delegate that configures the policy.</param>
  public void AddPolicy(string name, Action<PolicyBuilder> configurePolicy)
  {
    ArgumentNullException.ThrowIfNull(name);

    ArgumentNullException.ThrowIfNull(configurePolicy);

    PolicyBuilder builder = new();
    configurePolicy(builder);
    AddPolicy(name, builder.Build());
  }

  /// <summary>
  ///   Returns the policy for the specified name, or null if a policy with the specified name does not exist.
  /// </summary>
  /// <param name="name">The name of the policy to return.</param>
  /// <returns>The policy for the specified name, or null if a policy with the specified name does not exist.</returns>
  public AuthorizationPolicy? GetPolicy(string name)
  {
    return _policyMap.GetValueOrDefault(name);
  }
}
