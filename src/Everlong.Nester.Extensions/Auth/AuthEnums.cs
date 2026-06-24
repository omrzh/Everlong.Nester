namespace Everlong.Nester.Auth;

/// <summary>
///   Defines UI authorization behavior modes.
/// </summary>
public enum AuthorizationType
{
  /// <summary>
  ///   Controls element visibility based on authorization result.
  /// </summary>
  Visible,
  /// <summary>
  ///   Controls element enabled state based on authorization result.
  /// </summary>
  Enable,
  /// <summary>
  ///   Uses an authorization view representation.
  /// </summary>
  AuthorizeView
}

/// <summary>
///   Represents the current authorization evaluation state.
/// </summary>
public enum AuthorizationState
{
  /// <summary>
  ///   Authorization has not completed yet.
  /// </summary>
  Pending,
  /// <summary>
  ///   Authorization check passed.
  /// </summary>
  Passed,
  /// <summary>
  ///   Authorization check failed.
  /// </summary>
  Failed,
  /// <summary>
  ///   Authorization check was skipped.
  /// </summary>
  Skipped,
  /// <summary>
  ///   Authorization has been checked.
  /// </summary>
  Checked
}
