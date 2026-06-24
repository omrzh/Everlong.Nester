namespace Everlong.Nester.Auth;

/// <summary>
///   Represents an authorization requirement that can handle itself.
/// </summary>
internal interface ISelfHandlingAuthorizationRequirement : IAuthorizationRequirement
{
  /// <summary>
  ///   Handles the requirement.
  /// </summary>
  /// <param name="context">The authorization context.</param>
  void Handle(AuthorizationHandlerContext context);
}

/// <summary>
///   Checks if the user has specific roles.
/// </summary>
internal class RolesAuthorizationRequirement : ISelfHandlingAuthorizationRequirement
{
  /// <summary>
  ///   Initializes a new instance of the <see cref="RolesAuthorizationRequirement" /> class.
  /// </summary>
  /// <param name="allowedRoles">The allowed roles.</param>
  public RolesAuthorizationRequirement(IEnumerable<string> allowedRoles)
  {
    ArgumentNullException.ThrowIfNull(allowedRoles);

    if (!allowedRoles.Any())
    {
      throw new ArgumentException("At least one role must be specified.", nameof(allowedRoles));
    }

    AllowedRoles = allowedRoles;
  }

  /// <summary>
  ///   Gets the collection of allowed roles.
  /// </summary>
  public IEnumerable<string> AllowedRoles { get; }

  /// <summary>
  ///   Handles the requirement.
  /// </summary>
  public void Handle(AuthorizationHandlerContext context)
  {
    if (context.User.Identity?.IsAuthenticated == true)
    {
      bool found = false;
      foreach (string role in AllowedRoles)
      {
        if (context.User.IsInRole(role))
        {
          found = true;
          break;
        }
      }

      if (found)
      {
        context.Succeed(this);
      }
    }
  }
}

/// <summary>
///   Checks if the user is authenticated.
/// </summary>
internal class DenyAnonymousAuthorizationRequirement : ISelfHandlingAuthorizationRequirement
{
  /// <summary>
  ///   Handles the requirement.
  /// </summary>
  public void Handle(AuthorizationHandlerContext context)
  {
    if (context.User.Identity?.IsAuthenticated == true)
    {
      context.Succeed(this);
    }
  }
}

/// <summary>
///   Checks if the user has a specific claim.
/// </summary>
internal class ClaimsAuthorizationRequirement : ISelfHandlingAuthorizationRequirement
{
  /// <summary>
  ///   Initializes a new instance of the <see cref="ClaimsAuthorizationRequirement" /> class.
  /// </summary>
  internal ClaimsAuthorizationRequirement(string claimType, IEnumerable<string>? allowedValues)
  {
    ClaimType = claimType ?? throw new ArgumentNullException(nameof(claimType));
    AllowedValues = allowedValues;
  }

  /// <summary>
  ///   Gets the claim type.
  /// </summary>
  public string ClaimType { get; }

  /// <summary>
  ///   Gets the allowed values.
  /// </summary>
  public IEnumerable<string>? AllowedValues { get; }

  /// <summary>
  ///   Handles the requirement.
  /// </summary>
  public void Handle(AuthorizationHandlerContext context)
  {
    if (context.User.HasClaim(c =>
                                string.Equals(c.Type, ClaimType, StringComparison.OrdinalIgnoreCase) &&
                                (AllowedValues == null || !AllowedValues.Any() ||
                                 AllowedValues.Contains(c.Value, StringComparer.Ordinal))))
    {
      context.Succeed(this);
    }
  }
}

/// <summary>
///   Checks a custom assertion.
/// </summary>
/// <remarks>
///   Initializes a new instance of the <see cref="AssertionRequirement" /> class.
/// </remarks>
internal class AssertionRequirement(Func<AuthorizationHandlerContext, bool> handler)
  : ISelfHandlingAuthorizationRequirement
{
  /// <summary>
  ///   Handles the requirement.
  /// </summary>
  public void Handle(AuthorizationHandlerContext context)
  {
    if (handler(context))
    {
      context.Succeed(this);
    }
  }
}
