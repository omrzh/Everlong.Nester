using Everlong.Nester.Primitives;
using Everlong.Nester.Routing;

namespace Everlong.Nester.Dialog;

/// <summary>
///   Platform-specific dialog extensions on <see cref="IRouter" />.
/// </summary>
public static class PlatformDialogExtensions
{
  /// <summary>
  ///   Shows a color picker dialog and returns the selected WPF <see cref="PColor" />.
  /// </summary>
  /// <param name="router">The router that shows the color picker.</param>
  /// <param name="title">The dialog title.</param>
  /// <param name="message">The dialog message.</param>
  /// <param name="sessionOptions">Optional session-level overrides for initial color and button texts.</param>
  /// <returns>The selected WPF color, or <see langword="null" /> if the dialog was canceled.</returns>
  public static async Task<PColor?> PickPlatformColorAsync(this IRouter router,
                                                          string? title = null,
                                                          string? message = null,
                                                          ColorPickerOptions? sessionOptions = null)
  {
    var result = await router.PickColorAsync(title, message, sessionOptions);
    return result?.ToColor();
  }
}
