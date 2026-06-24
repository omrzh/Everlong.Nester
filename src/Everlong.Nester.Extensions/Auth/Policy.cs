namespace Everlong.Nester.Auth;

/// <summary>
///   Represents a collection of authorization requirements and the scheme they apply to.
/// </summary>
public class AuthorizationPolicy
{
  /// <summary>
  ///   Initializes a new instance of the <see cref="AuthorizationPolicy" /> class.
  /// </summary>
  /// <param name="requirements">The list of requirements.</param>
  /// <param name="authenticationSchemes">The list of authentication schemes.</param>
  public AuthorizationPolicy(IEnumerable<IAuthorizationRequirement> requirements,
                             IEnumerable<string> authenticationSchemes)
  {
    ArgumentNullException.ThrowIfNull(requirements);

    ArgumentNullException.ThrowIfNull(authenticationSchemes);

    Requirements = new List<IAuthorizationRequirement>(requirements).AsReadOnly();
    AuthenticationSchemes = new List<string>(authenticationSchemes).AsReadOnly();
  }

  /// <summary>
  ///   Gets the collection of requirements that must be met for this policy.
  /// </summary>
  public IReadOnlyList<IAuthorizationRequirement> Requirements { get; }

  /// <summary>
  ///   Gets the authentication schemes the policy applies to.
  /// </summary>
  public IReadOnlyList<string> AuthenticationSchemes { get; }

  /// <summary>
  ///   Combines multiple policies into a single policy.
  /// </summary>
  public static AuthorizationPolicy Combine(params AuthorizationPolicy[] policies)
  {
    return Combine((IEnumerable<AuthorizationPolicy>)policies);
  }

  /// <summary>
  ///   Combines multiple policies into a single policy.
  /// </summary>
  public static AuthorizationPolicy Combine(IEnumerable<AuthorizationPolicy> policies)
  {
    ArgumentNullException.ThrowIfNull(policies);

    PolicyBuilder builder = new();
    foreach (AuthorizationPolicy policy in policies)
    {
      builder.Combine(policy);
    }

    return builder.Build();
  }
}
