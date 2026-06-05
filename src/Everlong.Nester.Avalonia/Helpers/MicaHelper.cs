using System.Runtime.InteropServices;

namespace Everlong.Nester.Helpers;

/// <summary>
///   The Windows backdrop helpers — the Mica system backdrop (Windows 11)
///   and the immersive dark-mode flag, applied to a window's native handle.
/// </summary>
/// <remarks>Both entry points are no-ops outside Windows or without a native handle.</remarks>
public static partial class MicaHelper
{
  private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
  private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

  /// <summary>Applies the Mica system backdrop to <paramref name="window" />.</summary>
  /// <param name="window">The window whose native handle receives the attribute.</param>
  /// <param name="useAlt"><see langword="true" /> uses the alternate backdrop type (4); otherwise Mica (2).</param>
  /// <returns><see langword="true" /> when the backdrop was applied; otherwise <see langword="false" />.</returns>
  public static bool TryApplyMica(PWindow window, bool useAlt = false)
  {
    if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
      return false;

    var hwnd = window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
    if (hwnd == IntPtr.Zero)
      return false;

    int backdropType = useAlt ? 4 : 2;
    var ret = DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, sizeof(int));
    return ret == 0;
  }

  /// <summary>Sets the immersive dark-mode flag on <paramref name="window" />.</summary>
  /// <param name="window">The window whose native handle receives the attribute.</param>
  /// <param name="isDark"><see langword="true" /> for dark mode; otherwise light.</param>
  public static void ApplyDarkMode(PWindow window, bool isDark)
  {
    if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
      return;

    var hwnd = window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
    if (hwnd == IntPtr.Zero)
      return;

    int value = isDark ? 1 : 0;
    DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
  }

  [LibraryImport("dwmapi.dll")]
  private static partial int DwmSetWindowAttribute(
    IntPtr hwnd,
    int dwAttribute,
    ref int pvAttribute,
    int cbAttribute);
}
