using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Everlong.Nester.Helpers;

/// <summary>
/// Provides helper methods for interacting with native Win32 window management functions.
/// </summary>
[SupportedOSPlatform("windows")]
public static partial class Win32Helper
{
  /// <summary>Show-normal window state.</summary>
  private const int SW_SHOW = 5;

  /// <summary>Restore window state.</summary>
  private const int SW_RESTORE = 9;

  /// <summary>
  /// Attempts to bring the specified window to the foreground.
  /// </summary>
  /// <remarks>This method only performs the operation on Windows platforms. If called on a non-Windows operating system or if the handle is zero, the method returns false.</remarks>
  /// <param name="hWnd">A handle to the window to bring to the front. Must be a valid window handle on a Windows operating system.</param>
  /// <returns>true if the window was successfully brought to the front; otherwise, false.</returns>
  public static bool TryBringWindowToFront(IntPtr hWnd)
  {
    if (!OperatingSystem.IsWindows() || hWnd == IntPtr.Zero)
    {
      return false;
    }

    return TryBringToFrontWindowWindows(hWnd);
  }

  private static bool TryBringToFrontWindowWindows(IntPtr hWnd)
  {
    _ = ShowWindow(hWnd, SW_RESTORE);
    _ = ShowWindow(hWnd, SW_SHOW);
    _ = BringWindowToTop(hWnd);
    if (SetForegroundWindow(hWnd))
    {
      return true;
    }

    var foreground = GetForegroundWindow();
    if (foreground == IntPtr.Zero)
    {
      return false;
    }

    var foregroundThreadId = GetWindowThreadProcessId(foreground, out _);
    var currentThreadId = GetCurrentThreadId();
    if (foregroundThreadId == 0 || foregroundThreadId == currentThreadId)
    {
      return SetForegroundWindow(hWnd);
    }

    if (!AttachThreadInput(foregroundThreadId, currentThreadId, true))
    {
      return false;
    }

    try
    {
      _ = BringWindowToTop(hWnd);
      return SetForegroundWindow(hWnd);
    }
    finally
    {
      _ = AttachThreadInput(foregroundThreadId, currentThreadId, false);
    }
  }

  /// <summary>Shows or hides the window per the show-state command.</summary>
  [LibraryImport("user32.dll")]
  [return: MarshalAs(UnmanagedType.Bool)]
  public static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);

  /// <summary>Brings the window to the foreground.</summary>
  [LibraryImport("user32.dll")]
  [return: MarshalAs(UnmanagedType.Bool)]
  public static partial bool SetForegroundWindow(IntPtr hWnd);

  /// <summary>Brings the window to the top of the z-order.</summary>
  [LibraryImport("user32.dll")]
  [return: MarshalAs(UnmanagedType.Bool)]
  public static partial bool BringWindowToTop(IntPtr hWnd);

  /// <summary>Gets the foreground window handle.</summary>
  [LibraryImport("user32.dll")]
  public static partial IntPtr GetForegroundWindow();

  /// <summary>Gets the thread and process id owning the window.</summary>
  [LibraryImport("user32.dll")]
  public static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

  /// <summary>Gets the calling thread's id.</summary>
  [LibraryImport("user32.dll")]
  public static partial uint GetCurrentThreadId();

  /// <summary>Attaches or detaches the input queues of two threads.</summary>
  [LibraryImport("user32.dll")]
  [return: MarshalAs(UnmanagedType.Bool)]
  public static partial bool AttachThreadInput(uint idAttach,
                                               uint idAttachTo,
                                               [MarshalAs(UnmanagedType.Bool)] bool fAttach);

  /// <summary>Plays a system sound.</summary>
  [LibraryImport("user32.dll", EntryPoint = "MessageBeep")]
  [return: MarshalAs(UnmanagedType.Bool)]
  public static partial bool MessageBeep(uint uType);



  /// <summary> Flash the taskbar button to attract attention.</summary>
  public static bool FlashWindow(IntPtr hWnd)
  {
    var flashInfo = new FLASHWINFO
    {
      cbSize = (uint)Marshal.SizeOf<FLASHWINFO>(),
      hwnd = hWnd,
      dwFlags = FLASHW_TRAY | FLASHW_TIMERNOFG,
      uCount = uint.MaxValue,
      dwTimeout = 0
    };
    return FlashWindowEx(ref flashInfo);
  }

  [StructLayout(LayoutKind.Sequential)]
  private struct FLASHWINFO
  {
    public uint cbSize;
    public IntPtr hwnd;
    public uint dwFlags;
    public uint uCount;
    public uint dwTimeout;
  }

  private const uint FLASHW_TRAY = 0x00000002;
  private const uint FLASHW_TIMERNOFG = 0x0000000C;

  [LibraryImport("user32.dll")]
  [return: MarshalAs(UnmanagedType.Bool)]
  private static partial bool FlashWindowEx(ref FLASHWINFO pwfi);
}
