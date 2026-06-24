namespace Everlong.Nester.Auth;

/// <summary>
///   Provides access to authorization policies.
/// </summary>
public interface IAuthorizationPolicyProvider
{
  /// <summary>
  ///   Gets the policy with the specified name.
  /// </summary>
  /// <param name="policyName">The name of the policy to return.</param>
  /// <returns>The policy, or null if not found.</returns>
  AuthorizationPolicy? GetPolicy(string policyName);

  /// <summary>
  ///   Gets the default authorization policy.
  /// </summary>
  /// <returns>The default policy.</returns>
  AuthorizationPolicy GetDefaultPolicy();

  /// <summary>
  ///   Gets the fallback authorization policy.
  /// </summary>
  /// <returns>The fallback policy.</returns>
  AuthorizationPolicy? GetFallbackPolicy();
}

/// <summary>
///   The default policy provider.
/// </summary>
/// <param name="options">The authorization options.</param>
internal class DefaultAuthorizationPolicyProvider(AuthorizationOptions options) : IAuthorizationPolicyProvider
{
  private readonly AuthorizationOptions _options = options ?? throw new ArgumentNullException(nameof(options));

  /// <inheritdoc />
  public AuthorizationPolicy? GetPolicy(string policyName)
  {
    return _options.GetPolicy(policyName);
  }

  /// <inheritdoc />
  public AuthorizationPolicy GetDefaultPolicy()
  {
    return _options.DefaultPolicy;
  }

  /// <inheritdoc />
  public AuthorizationPolicy? GetFallbackPolicy()
  {
    return _options.FallbackPolicy;
  }
}
