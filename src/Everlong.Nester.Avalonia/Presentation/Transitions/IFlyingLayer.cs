// NOTE: Single-source file — the WPF project compiles this exact file via
// <Compile Include> in Everlong.Nester.Wpf.csproj.  Edit it here only;
// never create a WPF-side copy (the two builds would drift).

namespace Everlong.Nester.Presentation;

/// <summary>
///   The window's flying layer — the topmost band's plane figure.
/// </summary>
public interface IFlyingLayer
{
  /// <summary>The flying layer's plane figure.</summary>
  FlyingCanvas Canvas { get; }
}
