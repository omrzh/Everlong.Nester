using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Styling;

namespace Everlong.Nester.Shell;

/// <summary>
///   The Avalonia platform face of a shell — the shell itself (an
///   <see cref="IShell" />) declares its Avalonia identity: View-side types
///   (a window) that know they live on Avalonia can test/cast their
///   <see cref="IShell" /> reference to this interface and call platform
///   members directly — no extension indirection needed.
/// </summary>
public interface IAvaloniaShell : IShell
{
  /// <summary>
  ///   Translates a window-closing notification into a <see cref="TryCloseIntent" />
  ///   for unified arbitration through the intent chain.  The host window's
  ///   <c>OnClosing</c> override calls this.
  /// </summary>
  void WindowClosingToTryCloseIntent(WindowClosingEventArgs e);

  /// <summary>Translates mouse side buttons (XButton1 / XButton2) into <c>BackIntent</c> / <c>ForwardIntent</c>.</summary>
  void MouseSideButtonToRoutingIntent(object? sender, PointerReleasedEventArgs e);

  /// <summary>Translates keyboard shortcuts (F5, Alt+Left/Right) into navigation intents.</summary>
  void KeyDownToRoutingIntent(object? sender, KeyEventArgs e);

  /// <summary>Gets the top-level storage provider (file pickers) — null when no top-level is attached.</summary>
  IStorageProvider? StorageProvider { get; }

  /// <summary>
  ///   Channel: forwards the host window's property change into the
  ///   shell's status snapshot.
  /// </summary>
  void FeedHostPropertyChanged(AvaloniaPropertyChangedEventArgs e);

  /// <summary>Gets the top-level clipboard — null when no top-level is attached.</summary>
  IClipboard? Clipboard { get; }

  /// <summary>Gets the platform settings (scaling, animations) — null when no top-level is attached.</summary>
  IPlatformSettings? PlatformSettings { get; }

  /// <summary>Gets the requested theme variant (light/dark) — null when not configured.</summary>
  ThemeVariant? RequestedThemeVariant { get; }

  /// <summary>Gets the shell's top-level visual root (the host window / MainView).</summary>
  TopLevel? GetTopLevel();

  /// <summary>Gets the window's screens — null when the host is not a window.</summary>
  Screens? Screens { get; }

  /// <summary>Gets the window's transparency level hint (single-view: empty list).</summary>
  IReadOnlyList<WindowTransparencyLevel> TransparencyLevelHint { get; }

  /// <summary>Gets whether the window extends client area to decorations (single-view: false).</summary>
  bool ExtendClientAreaToDecorationsHint { get; }

  /// <summary>Gets the window's system decorations mode (single-view: default).</summary>
  WindowDecorations WindowDecorations { get; }

  /// <summary>Gets the shell's window — null for single-view (no window).</summary>
  PWindow? Window { get; }

  /// <summary>Gets the OS activation lifetime (single-view platforms) — null on classic desktop.</summary>
  IActivatableLifetime? ActivatableLifetime { get; }
}
