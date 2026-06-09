namespace Everlong.Nester.Shell;

/// <summary>
///   The WPF clipboard contract — a synchronous text surface over
///   <c>System.Windows.Clipboard</c>.  Resolved through
///   <see cref="IShell.GetPlatformService{T}" />.
/// </summary>
public interface IClipboardService
{
  /// <summary>Gets the current clipboard text; <see langword="null" /> when empty.</summary>
  string? GetText();

  /// <summary>Sets the clipboard text.</summary>
  void SetText(string text);
}
