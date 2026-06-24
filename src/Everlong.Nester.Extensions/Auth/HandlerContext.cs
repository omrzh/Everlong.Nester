using System.Security.Claims;

namespace Everlong.Nester.Auth;

/// <summary>
///   Represents the context for an authorization request.
/// </summary>
/// <remarks>
///   Initializes a new instance of the <see cref="AuthorizationHandlerContext" /> class.
/// </remarks>
/// <param name="requirements">The requirements to evaluate.</param>
/// <param name="user">The user to evaluate the requirements against.</param>
/// <param name="resource">The resource to evaluate the requirements against.</param>
public class AuthorizationHandlerContext(
  IEnumerable<IAuthorizationRequirement> requirements,
  ClaimsPrincipal user,
  object? resource)
{
  private readonly HashSet<IAuthorizationRequirement> _pendingRequirements = [.. requirements];
  private bool _succeedCalled;

  /// <summary>
  ///   The requirements that have not yet been satisfied.
  /// </summary>
  public IEnumerable<IAuthorizationRequirement> PendingRequirements => _pendingRequirements;

  /// <summary>
  ///   The collection of all requirements for the current authorization action.
  /// </summary>
  public IEnumerable<IAuthorizationRequirement> Requirements { get; } = requirements;

  /// <summary>
  ///   The user to evaluate the requirements against.
  /// </summary>
  public ClaimsPrincipal User { get; } = user;

  /// <summary>
  ///   The resource to evaluate the requirements against.
  /// </summary>
  public object? Resource { get; } = resource;

  /// <summary>
  ///   Flag indicating whether the authorization check has failed.
  /// </summary>
  public bool HasFailed { get; private set; }

  /// <summary>
  ///   Flag indicating whether the authorization check has succeeded.
  /// </summary>
  public bool HasSucceeded => !HasFailed && _succeedCalled && _pendingRequirements.Count == 0;

  /// <summary>
  ///   Called to mark the specified requirement as being successfully evaluated.
  /// </summary>
  /// <param name="requirement">The requirement whose evaluation has succeeded.</param>
  public void Succeed(IAuthorizationRequirement requirement)
  {
    _succeedCalled = true;
    _pendingRequirements.Remove(requirement);
  }

  /// <summary>
  ///   Called to mark the authorization process as failed.
  /// </summary>
  public void Fail()
  {
    HasFailed = true;
  }
}
