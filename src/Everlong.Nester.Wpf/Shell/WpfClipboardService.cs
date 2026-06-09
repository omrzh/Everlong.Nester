using System.Windows;

namespace Everlong.Nester.Shell;

/// <summary>Synchronous text clipboard over <c>System.Windows.Clipboard</c>.</summary>
internal sealed class WpfClipboardService : IClipboardService
{
  public string? GetText() => Clipboard.ContainsText() ? Clipboard.GetText() : null;

  public void SetText(string text) => Clipboard.SetText(text);
}
