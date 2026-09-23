using Everlong.Nester.Messaging;

namespace Everlong.Nester.Hosting;

/// <summary>
///   The WPF session-ending entry point.
/// </summary>
public static class WpfSessionEndingExtensions
{
  /// <summary>
  ///   Runs the session-ending orchestration over <paramref name="hub" />.
  /// </summary>
  /// <param name="hub">The hub that broadcasts the request.</param>
  /// <param name="boxFactory">
  ///   Supplies the prompt surface; <see langword="null" /> uses the built-in
  ///   WPF message box.
  /// </param>
  /// <returns><see langword="true" /> when every guard was confirmed.</returns>
  public static bool RequestSessionEnding(this IMessageHub hub, Func<IMessageBox>? boxFactory)
    => SessionEndingArbitration.Run(hub, boxFactory?.Invoke() ?? WpfMessageBox.Default);
}
