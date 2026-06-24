namespace Everlong.Nester.Auth;

/// <summary>
///   Specifies that the resource requires authorization.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class AuthorizeAttribute : Attribute
{
  /// <summary>
  ///   Gets or sets a comma-delimited list of roles that are allowed to access the resource.
  /// </summary>
  public string? Roles { get; set; }

  /// <summary>
  ///   Gets or sets the policy name that determines access to the resource.
  /// </summary>
  public string? Policy { get; set; }
}

/// <summary>
///   Marks a partial class as the assembly's authorization registry — the
///   generator emits its <see cref="IAuthRegistry" /> implementation into
///   the marked class.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class AuthRegistryAttribute : Attribute;

/// <summary>
///   Appends the registry implemented at <typeparamref name="TRegistry" />
///   to the registry generated at the marked class — consulted after the
///   generated registry's own declarations miss.  Repeatable — each
///   occurrence appends one registry.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ConcatAttribute<TRegistry> : Attribute where TRegistry : IAuthRegistry, new();
