using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Everlong.Nester.Activation;

/// <summary>
///   Desktop activation agent for Windows: publishes the main window handle
///   in the handshake Ack and performs Win32 foreground activation.
/// </summary>
/// <remarks>
///   The Win32 foreground APIs require a window; activation is a no-op until
///   a host window handle is provided.
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed partial class WindowsActivationAgent : DesktopActivationAgent
{
  /// <summary>Creates the Windows activation agent (Win32 foreground handling).</summary>
  public WindowsActivationAgent(
    string? leaseName = null,
    bool enableNegotiation = false,
    TimeSpan? negotiateTimeout = null)
    : base(leaseName, enableNegotiation, negotiateTimeout)
  {
  }

  /// <inheritdoc />
  protected override void ActivateLeaderWindow(long mainWindowHandle)
  {
    if (mainWindowHandle == 0)
      return;

    var hwnd = (nint)mainWindowHandle;
    var currentThread = GetCurrentThreadId();
    var leaderThread = GetWindowThreadProcessId(hwnd, out _);
    bool attached = false;
    if (leaderThread != currentThread)
      attached = AttachThreadInput(currentThread, leaderThread, true);
    if (IsIconic(hwnd))
      ShowWindow(hwnd, SW_RESTORE);
    SetForegroundWindow(hwnd);
    BringWindowToTop(hwnd);
    if (attached)
      AttachThreadInput(currentThread, leaderThread, false);
  }

  /// <inheritdoc />
  protected override void ActivateFollowerWindow(int sourcePid)
    => BringProcessToForeground(sourcePid);

  private static void BringProcessToForeground(int pid)
  {
    try
    {
      var process = Process.GetProcessById(pid);
      process.WaitForInputIdle(2000);
      var hwnd = process.MainWindowHandle;
      if (hwnd == IntPtr.Zero)
        return;

      var currentThread = GetCurrentThreadId();
      var targetThread = GetWindowThreadProcessId(hwnd, out _);
      bool attached = false;
      if (currentThread != targetThread)
        attached = AttachThreadInput(currentThread, targetThread, true);
      if (IsIconic(hwnd))
        ShowWindow(hwnd, SW_RESTORE);
      SetForegroundWindow(hwnd);
      BringWindowToTop(hwnd);
      if (attached)
        AttachThreadInput(currentThread, targetThread, false);
    }
    catch { }
  }

  #region P/Invokes

  [LibraryImport("user32.dll")]
  [return: MarshalAs(UnmanagedType.Bool)]
  private static partial bool SetForegroundWindow(IntPtr hWnd);

  [LibraryImport("user32.dll")]
  [return: MarshalAs(UnmanagedType.Bool)]
  private static partial bool IsIconic(IntPtr hWnd);

  [LibraryImport("user32.dll")]
  [return: MarshalAs(UnmanagedType.Bool)]
  private static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);

  [LibraryImport("user32.dll")]
  [return: MarshalAs(UnmanagedType.Bool)]
  private static partial bool BringWindowToTop(IntPtr hWnd);

  [LibraryImport("user32.dll")]
  [return: MarshalAs(UnmanagedType.Bool)]
  private static partial bool AttachThreadInput(uint idAttach,
                                                uint idAttachTo,
                                                [MarshalAs(UnmanagedType.Bool)] bool fAttach);

  [LibraryImport("user32.dll")]
  private static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

  [LibraryImport("kernel32.dll")]
  private static partial uint GetCurrentThreadId();

  const int SW_RESTORE = 9;

  #endregion
}
