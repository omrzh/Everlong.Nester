namespace Everlong.Nester.Primitives;

/// <summary>
///   Window state for the shell.
/// </summary>
public enum HostState
{
  /// <summary>
  ///   The window is in its normal (restored) state.
  /// </summary>
  Normal = 0,

  /// <summary>The window is minimized to the taskbar.</summary>
  Minimized = 1,

  /// <summary>The window is maximized to fill the screen.</summary>
  Maximized = 2,

  /// <summary>The shell occupies the full screen without window decorations.</summary>
  FullScreen = 3
}
