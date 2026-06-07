namespace Everlong.Nester.Chrome;

/// <summary>
///   Configuration options for <see cref="NesterWindowChrome"/>.
/// </summary>
public record NesterWindowChromeOptions
{
  /// <summary>
  ///   Height of the caption / drag area, in device-independent units.
  ///   Default: 32 — the height of the Windows 11 caption buttons.
  /// </summary>
  public double CaptionHeight { get; init; } = 32;

  /// <summary>Corner radius of the window frame. Default: 12 on all corners.</summary>
  public PCornerRadius CornerRadius { get; init; } = new(12);

  /// <summary>
  ///   Thickness of the glass (DWM) frame.  <c>new(-1)</c> extends the frame
  ///   into the client area, which is where the system draws its caption
  ///   buttons; <c>new(0)</c> leaves a plain client area.
  ///   Default: <c>new(-1)</c>.
  /// </summary>
  public PThickness GlassFrameThickness { get; init; } = new(-1);

  /// <summary>
  ///   Whether the system draws its own caption buttons.  The system draws
  ///   them into the extended frame, which app content covers; an app that
  ///   paints its own title bar sets this to <c>false</c>.
  ///   Default: <c>false</c>.
  /// </summary>
  public bool UseAeroCaptionButtons { get; init; }

  /// <summary>Resize border thickness used when the window is resizable. Default: 4px on all sides.</summary>
  public PThickness ResizeBorderThickness { get; init; } = new(4);

  /// <summary>Shared default instance with Fluent-style settings.</summary>
  public static NesterWindowChromeOptions Default { get; } = new();
}
