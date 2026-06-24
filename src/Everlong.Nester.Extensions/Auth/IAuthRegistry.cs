namespace Everlong.Nester.Auth;

/// <summary>
///   Provides the authorization requirement declared for a type.
/// </summary>
public interface IAuthRegistry
{
  /// <summary>
  ///   Gets the requirement declared for <paramref name="type" />, or
  ///   <see langword="null" /> when none is declared.
  /// </summary>
  AuthDescriptor? GetDescriptor(Type type);
}
