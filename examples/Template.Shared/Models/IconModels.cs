namespace NesterApp.Models;

/// <summary>
///   An emoji used as an icon.  The glyph is carried as data so the view model
///   stays UI-free; each platform renders it with its own text stack
///   (<c>DataTemplate</c> in App.axaml / App.xaml).
/// </summary>
/// <param name="Glyph">The emoji character (or ZWJ sequence).</param>
public sealed record EmojiIcon(string Glyph);
