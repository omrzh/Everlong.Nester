using Everlong.Nester.Intent;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Everlong.Nester.Routing;

// Core interfaces of the routing domain — the router, its stack, and the route and location surfaces.


/// <summary>The marker for routing-domain intents.</summary>
public interface IRouteIntent : IIntent;


/// <summary>The navigation entry point of a surface.</summary>
public interface IRouter
{
  /// <summary>The router's role — <see cref="RouterRole.Base"/> for the shell's main router, <see cref="RouterRole.Derived"/> for result-bearing routers.</summary>
  RouterRole Role { get; }

  /// <summary>Routes to a new location according to the locator.</summary>
  /// <returns>A task that finishes when the location is reached.</returns>
  Task RouteAsync(ILocator location);

  /// <summary>Records a fresh visit to the live entry that presents the given site.</summary>
  /// <returns><see langword="true"/> when the visit landed or the site is already current; <see langword="false"/> when no live entry presents the site.</returns>
  /// <remarks>A visit that faults before it lands faults the returned task; <see langword="false"/> means only that no live entry presents the site.</remarks>
  Task<bool> JumpAsync(ILocation site);

  /// <summary> Creates a derived router. </summary>
  /// <param name="isEphemeral">
  ///   <see langword="true" /> for a one-shot surface: it accepts one route
  ///   request and hands every later one off for a navigable surface to take,
  ///   instead of stacking it.
  /// </param>
  IRouter Derive(bool isEphemeral = false);

  /// <summary>The router's result channel — <see langword="null"/> when <see cref="Role"/> is <see cref="RouterRole.Base"/>, always present on a derived router.</summary>
  IRouterCompletion? Completion { get; }

  /// <summary>The router's navigation-state surface.</summary>
  IRouterStack Stack { get; }
}

/// <summary>The role of a router — the base router of a shell, or a derived router created for a result-bearing presentation.</summary>
public enum RouterRole
{
  /// <summary>The shell's main router.</summary>
  Base = 0,

  /// <summary>A result-bearing router created by <see cref="IRouter.Derive" />.</summary>
  Derived = 1
}

/// <summary>
///   The router's navigation-state surface — the current location, the
///   stack's traversal reach and entries.
/// </summary>
/// <remarks>
///   The property members raise
///   <see cref="System.ComponentModel.INotifyPropertyChanged.PropertyChanged" />
///   on every change (a landing or a trim); the query members compute on
///   demand and carry no notification promise.
/// </remarks>
public interface IRouterStack : INotifyPropertyChanged
{
  /// <summary>The router's current site — the presented chain's content target, <see langword="null" /> when nothing is presented.</summary>
  ILocation? Location { get; }

  /// <summary>Whether the router can traverse backward.</summary>
  bool CanGoBack { get; }

  /// <summary>Whether the router can traverse forward.</summary>
  bool CanGoForward { get; }

  /// <summary>
  ///   The entry ceiling, or <see langword="null" /> for no ceiling.
  /// </summary>
  /// <remarks>
  ///   A push beyond the ceiling evicts the oldest entries — the count
  ///   stays within it after every push; traversal never evicts.
  ///   Tightening an existing stack takes effect on the next push.
  /// </remarks>
  int? MaxDepth { get; set; }

  /// <summary>The number of entries.</summary>
  int Count { get; }

  /// <summary>
  ///   Removes the entry immediately behind the current position.
  /// </summary>
  /// <remarks>Requires a bound main-thread dispatcher; without one the trim runs on the caller's thread.</remarks>
  /// <returns><see langword="true" /> when an entry was removed; <see langword="false" /> when none exists behind the current position or the stack is mid-transaction.</returns>
  bool TrimBackward();

  /// <summary>
  ///   Removes the entry immediately ahead of the current position.
  /// </summary>
  /// <remarks>Requires a bound main-thread dispatcher; without one the trim runs on the caller's thread.</remarks>
  /// <returns><see langword="true" /> when an entry was removed; <see langword="false" /> when none exists ahead of the current position or the stack is mid-transaction.</returns>
  bool TrimForward();

  /// <summary>The entry immediately behind the current position's site, or <see langword="null" /> when none — a pure read.</summary>
  ILocation? PeekPrevious();

  /// <summary>The entry immediately ahead of the current position's site, or <see langword="null" /> when none — a pure read.</summary>
  ILocation? PeekNext();

  /// <summary>The entries behind the current one's sites, nearest first.</summary>
  IReadOnlyList<ILocation> BackStack();

  /// <summary>The entries ahead of the current one's sites, nearest first.</summary>
  IReadOnlyList<ILocation> ForwardStack();

  /// <summary>All entries' sites, oldest first.</summary>
  IReadOnlyList<ILocation> Snapshot();
}

/// <summary>A result-bearing router's completion channel.</summary>
public interface IRouterCompletion
{
  /// <summary>The router's lifecycle task — settles when the router yields a result.</summary>
  Task<object?> Result { get; }

  /// <summary>Completes the router with the given result.</summary>
  void Complete(object? result);
}

/// <summary>The arguments a route target consumes.</summary>
/// <remarks>Any value carries arguments by implementing this marker.</remarks>
public interface IArgs;

/// <summary>
///   A route target — the participant type, the arguments it consumes, and
///   the participant instance when pre-constructed.  A
///   <see langword="null" /> instance is resolved when the route is
///   interpreted.
/// </summary>
public interface ITarget
{
  /// <summary>The participant type.</summary>
  Type Type { get; }

  /// <summary>The requested arguments, or <see langword="null" /> when the target carries none.</summary>
  IArgs? Args { get; }

  /// <summary>A nullable pre-constructed participant instance.</summary>
  object? Instance { get; }
}

/// <summary>
///   A location descriptor — an instance that describes a destination
///   (its target chain) without navigating it.
/// </summary>
public interface ILocator
{
  /// <summary>The described target chain, outermost first — the content target last.</summary>
  /// <remarks>The value is fixed for the descriptor's lifetime.</remarks>
  IReadOnlyList<ITarget> Path { get; }
}

/// <summary>
///   A realized node of the presented chain — every resolved participant,
///   outermost or content target alike.  A location knows its place: the
///   ordered path to it and its immediate ancestor.
/// </summary>
public interface ILocation
{
  /// <summary>The participant type.</summary>
  Type Type { get; }

  /// <summary>The arguments the node serves — a parameterized node's identity, read back from <see cref="IParameterized.EngagedArgs" /> after delivery; <see langword="null" /> when the node serves none.</summary>
  IArgs? Args { get; }

  /// <summary>The resolved participant instance.</summary>
  object Instance { get; }

  /// <summary>The immediate ancestor site, or <see langword="null" /> at the outermost.</summary>
  ILocation? Parent { get; }

  /// <summary>The ordered path to this site, outermost first, this site last.</summary>
  IReadOnlyList<ILocation> Trail { get; }
}

/// <summary>Extension methods for types in Routing domain.</summary>
public static class RoutingExtensions
{
  extension(IRouterCompletion completion)
  {
    /// <summary>Gets the awaiter that completes when the channel's result settles.</summary>
    public TaskAwaiter<object?> GetAwaiter() => completion.Result.GetAwaiter();
  }
}
