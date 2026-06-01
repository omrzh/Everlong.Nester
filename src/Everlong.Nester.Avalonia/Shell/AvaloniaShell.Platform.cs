using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Everlong.Nester.Shell;

partial class AvaloniaShell : IAvaloniaShell
{
  #region IAvaloniaShell

  /// <inheritdoc />
  public IStorageProvider? StorageProvider => TopLevel?.StorageProvider;

  /// <inheritdoc />
  public IClipboard? Clipboard => TopLevel?.Clipboard;

  /// <inheritdoc />
  public IPlatformSettings? PlatformSettings => TopLevel?.GetPlatformSettings();

  /// <inheritdoc />
  public Screens? Screens => (TopLevel as PlatformWindow)?.Screens;

  /// <inheritdoc />
  public IReadOnlyList<WindowTransparencyLevel> TransparencyLevelHint
  {
    get => Window?.TransparencyLevelHint ?? [];
  }

  /// <inheritdoc />
  public bool ExtendClientAreaToDecorationsHint
  {
    get => Window?.ExtendClientAreaToDecorationsHint ?? false;
  }

  /// <inheritdoc />
  public ThemeVariant? RequestedThemeVariant => TopLevel?.RequestedThemeVariant;

  /// <inheritdoc />
  public WindowDecorations WindowDecorations
    => Window?.WindowDecorations ?? WindowDecorations.None;

  /// <inheritdoc />
  public PlatformWindow? Window => TopLevel as PlatformWindow;

  /// <inheritdoc />
  public IActivatableLifetime? ActivatableLifetime =>
    PlatformApp.Current?.TryGetFeature(typeof(IActivatableLifetime)) as IActivatableLifetime;

  /// <inheritdoc />
  public TopLevel? GetTopLevel() => TopLevel;

  #endregion
}
