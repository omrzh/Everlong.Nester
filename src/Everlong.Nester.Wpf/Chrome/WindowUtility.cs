namespace Everlong.Nester.Chrome;

/// <summary>
///   OS and Fluent-theme capability detection helpers for WPF window chrome.
/// </summary>
internal static class WindowUtility
{
  public static bool IsWindows11OrGreater()
  {
    var v = Environment.OSVersion.Version;
    return v.Major >= 10 && v.Build >= 22000;
  }

  /// <summary>Windows 11 22H2+ supports Mica/Acrylic backdrop.</summary>
  public static bool IsBackdropSupported()
  {
    var v = Environment.OSVersion.Version;
    return v.Major >= 10 && v.Build >= 22621;
  }

  public static bool IsBackdropDisabled()
  {
    var data = AppContext.GetData("Switch.System.Windows.Appearance.DisableFluentThemeWindowBackdrop");
    return data is not null && bool.Parse(Convert.ToString(data)!);
  }
}
