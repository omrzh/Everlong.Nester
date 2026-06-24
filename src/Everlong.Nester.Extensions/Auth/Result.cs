namespace Everlong.Nester.Auth;

/// <summary>
///   Represents the result of an authorization check.
/// </summary>
public class AuthorizationResult
{
  private AuthorizationResult(bool succeeded, AuthorizationFailure? failure)
  {
    Succeeded = succeeded;
    Failure = failure;
  }

  /// <summary>
  ///   Gets a value indicating whether the authorization succeeded.
  /// </summary>
  public bool Succeeded { get; }

  /// <summary>
  ///   Gets the failure information if authorization failed.
  /// </summary>
  public AuthorizationFailure? Failure { get; }

  /// <summary>
  ///   Creates a successful authorization result.
  /// </summary>
  public static AuthorizationResult Success()
  {
    return new AuthorizationResult(true, null);
  }

  /// <summary>
  ///   Creates a failed authorization result.
  /// </summary>
  public static AuthorizationResult Failed(AuthorizationFailure failure)
  {
    return new AuthorizationResult(false, failure);
  }

  /// <summary>
  ///   Creates a failed authorization result.
  /// </summary>
  public static AuthorizationResult Failed()
  {
    return new AuthorizationResult(false, AuthorizationFailure.ExplicitFail());
  }
}

/// <summary>
///   Represents the details of an authorization failure.
/// </summary>
public class AuthorizationFailure
{
  private AuthorizationFailure(IEnumerable<IAuthorizationRequirement> failedRequirements, bool failCalled)
  {
    FailedRequirements = failedRequirements;
    FailCalled = failCalled;
  }

  /// <summary>
  ///   Gets the requirements that failed.
  /// </summary>
  public IEnumerable<IAuthorizationRequirement> FailedRequirements { get; }

  /// <summary>
  ///   Gets a value indicating whether the failure was explicit (Fail() was called).
  /// </summary>
  public bool FailCalled { get; }

  /// <summary>
  ///   Creates a failure due to failed requirements.
  /// </summary>
  public static AuthorizationFailure Failed(IEnumerable<IAuthorizationRequirement> failedRequirements)
  {
    return new AuthorizationFailure(failedRequirements, false);
  }

  /// <summary>
  ///   Creates a failure due to an explicit Fail() call.
  /// </summary>
  public static AuthorizationFailure ExplicitFail()
  {
    return new AuthorizationFailure([], true);
  }
}
