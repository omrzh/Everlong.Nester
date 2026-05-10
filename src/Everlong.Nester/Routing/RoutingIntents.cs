using Everlong.Nester.Intent;

namespace Everlong.Nester.Routing;

/// <summary>
///   The routing domain's route command — carries the location to route;
///   a dispatched command consults the chain (pages may veto).
/// </summary>
public sealed record RouteIntent(ILocator Locator) : IRouteIntent;

/// <summary>Represents an intent to go back.</summary>
public record BackIntent(object? RetValue = null) : IRouteIntent;

/// <summary>Represents an intent to go forward.</summary>
public sealed record ForwardIntent : IRouteIntent;

/// <summary>Represents an intent to refresh the current view.</summary>
public sealed record RefreshIntent : IRouteIntent;

/// <summary>
///   Extension methods for classifying and inspecting shell intents.
/// </summary>
public static class IIntentExtensions
{
  extension(IIntent intent)
  {
    /// <summary>
    /// Indicates whether the intent is about routing.
    /// </summary>
    public bool IsRouting => intent is IRouteIntent;
  }
}
