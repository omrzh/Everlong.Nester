namespace Everlong.Nester.Routing;

/// <summary>
///   The operation a router is scheduling — the cause axis of a change.
/// </summary>
public enum RoutingDirection
{
  /// <summary>A route request — ground navigation or a derived presentation.</summary>
  Route = 0,

  /// <summary>A back traversal of the stack.</summary>
  Back = 1,

  /// <summary>A forward traversal of the stack.</summary>
  Forward = 2,

  /// <summary>An in-place replay of the current chain.</summary>
  Refresh = 3,

  /// <summary>A derived router's close.</summary>
  Close = 4,

  /// <summary>A fresh visit to an existing engagement — the live entry that presents it lands again.</summary>
  Jump = 5
}
