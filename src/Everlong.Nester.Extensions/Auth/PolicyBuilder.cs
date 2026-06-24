namespace Everlong.Nester.Auth;

/// <summary>
///   Builds <see cref="AuthorizationPolicy" /> instances.
/// </summary>
public class PolicyBuilder
{
  /// <summary>
  ///   Initializes a new instance of the <see cref="PolicyBuilder" /> class.
  /// </summary>
  public PolicyBuilder(params string[] authenticationSchemes)
  {
    foreach (string scheme in authenticationSchemes)
    {
      AuthenticationSchemes.Add(scheme);
    }
  }

  /// <summary>
  ///   Initializes a new instance of the <see cref="PolicyBuilder" /> class by copying another policy.
  /// </summary>
  public PolicyBuilder(AuthorizationPolicy policy)
  {
    Combine(policy);
  }

  /// <summary>
  ///   Gets the list of requirements.
  /// </summary>
  public IList<IAuthorizationRequirement> Requirements { get; } = new List<IAuthorizationRequirement>();

  /// <summary>
  ///   Gets the list of authentication schemes.
  /// </summary>
  public IList<string> AuthenticationSchemes { get; } = new List<string>();

  /// <summary>
  ///   Adds a requirement to the policy.
  /// </summary>
  public PolicyBuilder AddRequirements(params IAuthorizationRequirement[] requirements)
  {
    foreach (IAuthorizationRequirement req in requirements)
    {
      Requirements.Add(req);
    }

    return this;
  }

  /// <summary>
  ///   Adds a role requirement.
  /// </summary>
  public PolicyBuilder RequireRole(params string[] roles)
  {
    AddRequirements(new RolesAuthorizationRequirement(roles));
    return this;
  }

  /// <summary>
  ///   Adds a role requirement.
  /// </summary>
  public PolicyBuilder RequireRole(IEnumerable<string> roles)
  {
    AddRequirements(new RolesAuthorizationRequirement(roles));
    return this;
  }

  /// <summary>
  ///   Adds a requirement that the user must be authenticated.
  /// </summary>
  public PolicyBuilder RequireAuthenticatedUser()
  {
    AddRequirements(new DenyAnonymousAuthorizationRequirement());
    return this;
  }

  /// <summary>
  ///   Adds a claim requirement.
  /// </summary>
  public PolicyBuilder RequireClaim(string claimType, params string[] allowedValues)
  {
    AddRequirements(new ClaimsAuthorizationRequirement(claimType, allowedValues));
    return this;
  }

  /// <summary>
  ///   Adds a claim requirement.
  /// </summary>
  public PolicyBuilder RequireClaim(string claimType, IEnumerable<string> allowedValues)
  {
    AddRequirements(new ClaimsAuthorizationRequirement(claimType, allowedValues));
    return this;
  }

  /// <summary>
  ///   Adds an assertion requirement.
  /// </summary>
  public PolicyBuilder RequireAssertion(Func<AuthorizationHandlerContext, bool> handler)
  {
    AddRequirements(new AssertionRequirement(handler));
    return this;
  }

  /// <summary>
  ///   Combines another policy into the current builder.
  /// </summary>
  public PolicyBuilder Combine(AuthorizationPolicy policy)
  {
    ArgumentNullException.ThrowIfNull(policy);

    foreach (IAuthorizationRequirement req in policy.Requirements)
    {
      Requirements.Add(req);
    }

    foreach (string scheme in policy.AuthenticationSchemes)
    {
      if (!AuthenticationSchemes.Contains(scheme))
      {
        AuthenticationSchemes.Add(scheme);
      }
    }

    return this;
  }

  /// <summary>
  ///   Builds the policy.
  /// </summary>
  public AuthorizationPolicy Build()
  {
    return new AuthorizationPolicy(Requirements, AuthenticationSchemes);
  }
}
