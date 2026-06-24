namespace Everlong.Nester.Auth;

/// <summary>
///   Defines the core identity information required by the framework to build a ClaimsPrincipal.
/// </summary>
public interface IIdentityUser
{
  /// <summary>
  ///   Gets the unique identifier or username for the user.
  ///   Maps to ClaimTypes.NameIdentifier and "sub".
  /// </summary>
  string GetIdentity();

  /// <summary>
  ///   Gets the roles assigned to the user.
  ///   Maps to ClaimTypes.Role.
  /// </summary>
  IEnumerable<string> GetRoles();

  /// <summary>
  ///   Gets any additional claims for the user.
  /// </summary>
  IEnumerable<KeyValuePair<string, string>>? GetAdditionalClaims();
}
