using Everlong.Nester.Intent;

namespace Everlong.Nester.Routing;

/// <summary>Represents an intent to go back.</summary>
public record BackIntent(object? RetValue = null) : ITraversalIntent;

/// <summary>Represents an intent to go forward.</summary>
public sealed record ForwardIntent : ITraversalIntent;

/// <summary>Represents an intent to refresh the current view.</summary>
public sealed record RefreshIntent : ITraversalIntent;

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
    public bool IsRouting => intent is ITraversalIntent;
  }
}
