namespace Everlong.Nester.Presentation;

/// <summary>
///   The layout-body host contract — a layout view's <c>Body</c> slot, the
///   container the routing stages fill with the inner chain.
/// </summary>
public interface ITerminalBodyHost
{
  /// <summary>The layout's inner-content container.</summary>
  TuiLayoutBody Body { get; }
}
