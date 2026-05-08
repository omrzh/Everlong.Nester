namespace Everlong.Nester.Activation;

/// <summary>
///   The empty activation intent — dispatched when the startup input
///   decided nothing; handlers interpret it as the default target.
/// </summary>
public sealed record ShellActivationIntent : IActivationIntent;

/// <summary>
///   A second process requests this (leader) instance to handle its activation.
/// </summary>
/// <param name="Args">The follower's raw command-line arguments.</param>
/// <param name="SourceProcessId">The follower's process id.</param>
/// <param name="Converted">The follower's args after this agent's conversion
///   (<see cref="IActivationAgent.Convert" />) — what accepting the
///   negotiation commits this instance to handle.</param>
public sealed record NegotiateActivationIntent(
  IReadOnlyList<string> Args,
  int SourceProcessId,
  IReadOnlyList<IActivationIntent> Converted) : IActivationIntent;

/// <summary>
///   Represents an activation intent triggered by a URI (deep link).
/// </summary>
public sealed record UriActivationIntent(Uri Uri) : IActivationIntent;

/// <summary>
///   One or more files to open, given as command-line paths.
/// </summary>
public sealed record FileActivationIntent(IReadOnlyList<string> Files) : IActivationIntent;

