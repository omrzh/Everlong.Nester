namespace Everlong.Nester.Hosting;

/// <summary>
///   The process-wide Terminal.Gui handles the terminal platform needs at
///   runtime (the running <see cref="Terminal.Gui.App.IApplication" />) —
///   bound by the platform lifetime at init, read by the shells to stop
///   the app loop.
/// </summary>
internal static class TerminalRuntime
{
  /// <summary>The bound application instance, or <see langword="null" /> before init or after dispose.</summary>
  public static Terminal.Gui.App.IApplication? App { get; set; }
}
