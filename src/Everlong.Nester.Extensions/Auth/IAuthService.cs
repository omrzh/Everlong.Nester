using System.Security.Claims;

namespace Everlong.Nester.Auth;

/// <summary>
///   A service for authentication state and authorization checks.
/// </summary>
public interface IAuthService
{
  /// <summary>
  ///   Gets the current user's ClaimsPrincipal.
  /// </summary>
  ClaimsPrincipal? CurrentPrincipal { get; }

  /// <summary>
  ///   Gets the current authenticated user object.
  /// </summary>
  IIdentityUser? CurrentUser { get; }

  /// <summary>
  ///   Raised when the authentication state changes (login, logout, or
  ///   account switching).
  /// </summary>
  event EventHandler<AuthenticationStateChangedEventArgs>? AuthenticationStateChanged;

  /// <summary>
  ///   Raised when the authorization state changes.
  /// </summary>
  event EventHandler? AuthorizationChanged;

  /// <summary>
  ///   Logs in the user.
  /// </summary>
  /// <param name="user">The user object.</param>
  void Login(IIdentityUser user);

  /// <summary>
  ///   Logs out the user.
  /// </summary>
  void Logout();

  /// <summary>
  ///   Checks if a user meets a specific set of requirements for the specified resource.
  /// </summary>
  AuthorizationResult Authorize(ClaimsPrincipal user,
                                object? resource,
                                IEnumerable<IAuthorizationRequirement> requirements);

  /// <summary>
  ///   Checks if a user meets a specific policy for the specified resource.
  /// </summary>
  AuthorizationResult Authorize(ClaimsPrincipal user, object? resource, string policyName);

  /// <summary>
  ///   Checks if the current user meets the specified roles and policy.
  /// </summary>
  AuthorizationResult Authorize(string? roles, string? policyName);

  /// <summary>
  ///   Checks whether the current user is authenticated.
  /// </summary>
  AuthorizationResult Authenticate();
}

/// <summary>
///   Provides data for the AuthenticationStateChanged event.
/// </summary>
public class AuthenticationStateChangedEventArgs(ClaimsPrincipal currentPrincipal, ClaimsPrincipal? oldPrincipal)
  : EventArgs
{
  /// <summary>
  ///   Gets the current user.
  /// </summary>
  public ClaimsPrincipal CurrentPrincipal { get; } = currentPrincipal;

  /// <summary>
  ///   Gets the previous user.
  /// </summary>
  public ClaimsPrincipal? OldPrincipal { get; } = oldPrincipal;
}
