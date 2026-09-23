using Everlong.Nester.Messaging;

namespace Everlong.Nester.Hosting;

/// <summary>
///   The Avalonia session-ending entry point.
/// </summary>
public static class AvaloniaSessionEndingExtensions
{
  /// <summary>
  ///   Runs the session-ending orchestration over <paramref name="hub" />.
  /// </summary>
  /// <param name="hub">The hub that broadcasts the request.</param>
  /// <param name="boxFactory">
  ///   Supplies the prompt surface; <see langword="null" /> uses the built-in
  ///   Avalonia message box.
  /// </param>
  /// <returns><see langword="true" /> when every guard was confirmed.</returns>
  public static bool RequestSessionEnding(this IMessageHub hub, Func<IMessageBox>? boxFactory)
    => SessionEndingArbitration.Run(hub, boxFactory?.Invoke() ?? AvaloniaMessageBox.Default);
}
