namespace Everlong.Nester.Hosting;

static partial class AppLifetime
{
  /// <summary>Whether the process runs on Android.</summary>
  public static bool IsAndroid => OperatingSystem.IsAndroid();

  /// <summary>Whether the process runs in a browser (WASM).</summary>
  public static bool IsBrowser => OperatingSystem.IsBrowser();

  /// <summary>Whether the process runs on iOS.</summary>
  public static bool IsIOS => OperatingSystem.IsIOS();

  /// <summary>Whether the process runs on Windows.</summary>
  public static bool IsWindows => OperatingSystem.IsWindows();

  /// <summary>Whether the process runs on macOS.</summary>
  public static bool IsMacOS => OperatingSystem.IsMacOS();

  /// <summary>Whether the process runs on MacCatalyst.</summary>
  public static bool IsMacCatalyst => OperatingSystem.IsMacCatalyst();

  /// <summary>Whether the process runs on macOS or MacCatalyst.</summary>
  public static bool IsAnyMac => IsMacOS || IsMacCatalyst;

  /// <summary>Whether the process runs on Linux.</summary>
  public static bool IsLinux => OperatingSystem.IsLinux();

  /// <summary>
  ///  Gets a value indicating whether the app is running on a desktop platform.
  /// </summary>
  public static bool IsDesktop => IsWindows || IsLinux || IsAnyMac;
}
