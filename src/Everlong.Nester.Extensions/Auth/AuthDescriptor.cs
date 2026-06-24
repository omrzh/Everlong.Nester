namespace Everlong.Nester.Auth;

/// <summary>
///   Describes the authorization requirements of a resource — a null
///   <see cref="Roles" /> and <see cref="Policy" /> pair declares that
///   authentication alone is required.
/// </summary>
/// <param name="Roles">A comma-delimited list of roles that are allowed to access the resource.</param>
/// <param name="Policy">The policy name that determines access to the resource.</param>
public record AuthDescriptor(string? Roles = null, string? Policy = null);
