// NOTE: Single-source file — the WPF project compiles this exact file via
// <Compile Include> in Everlong.Nester.Wpf.csproj.  Edit it here only;
// never create a WPF-side copy (the two builds would drift).

namespace Everlong.Nester.Presentation;

/// <summary>
///   The flying layer's plane figure — the canvas filling the window's
///   topmost slot.
/// </summary>
public sealed class FlyingCanvas : PCanvas
{
  /// <summary>
  ///   The anchor a transition was declared from — an arbitrary value the
  ///   plane never reads or validates.
  /// </summary>
  /// <remarks>
  ///   One value at a time — assigning replaces the previous one, and
  ///   reading does not take it.  Cleared with the surface: by
  ///   <see cref="Clear" />, and when the plane's lease is reclaimed.
  /// </remarks>
  public object? Anchor { get; set; }

  /// <summary>Clears the plane's children and its <see cref="Anchor" />.</summary>
  public void Clear()
  {
    Children.Clear();
    Anchor = null;
  }
}
