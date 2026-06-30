namespace NesterApp;

/// <summary>Window-chrome geometry shared by every head.</summary>
public static class AppChrome
{
  /// <summary>
  ///   Height of the title-bar row, in device-independent units.  Matches the
  ///   Windows 11 caption-button band, so the system-drawn buttons line up
  ///   with the row; the WPF <c>WindowChrome</c> caption height uses it too.
  /// </summary>
  public const double TitleBarHeight = 32;
}
