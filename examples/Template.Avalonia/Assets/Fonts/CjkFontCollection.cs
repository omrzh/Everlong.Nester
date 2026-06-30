using Avalonia.Media.Fonts;

namespace NesterApp;

/// <summary>
///   The embedded CJK font collection — Noto Sans SC ships in the shared
///   App project (<c>Assets/Fonts</c>) and covers Chinese where the default
///   chain (Inter) does not.  Embedded, not system fonts: WASM has no system
///   font access, and the Android system font names vary by device/image.
///   FontManager falls back to every registered collection per codepoint
///   (<c>TryMatchCharacter</c> walks all collections), so registering this
///   collection is enough — no FontFallbacks configuration needed.
/// </summary>
public sealed class CjkFontCollection : EmbeddedFontCollection
{
  public CjkFontCollection() : base(
    new Uri("fonts:NesterCjk", UriKind.Absolute),
    new Uri("avares://Template.Avalonia/Assets/Fonts", UriKind.Absolute))
  {
  }
}
