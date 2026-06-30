using System.Diagnostics;
using Everlong.DI;
using Microsoft.Extensions.Logging;

namespace NesterApp.Services;

/// <summary>Hands a workspace file to the OS's default application.</summary>
public interface IWorkspaceFileOpener
{
  /// <summary>
  ///   Opens <paramref name="path" />.
  /// </summary>
  /// <returns>
  ///   <see langword="true" /> when the file was handed over; <see langword="false" />
  ///   when the platform has no recipe or the launch failed — the caller
  ///   reports it, this interface never throws.
  /// </returns>
  /// <remarks>
  ///   Asynchronous on purpose: the shell hand-off is slow (the OS resolves the
  ///   association and then negotiates with the application it starts), and the
  ///   caller owns a surface that must not wait for an editor to come up.
  /// </remarks>
  Task<bool> OpenAsync(string path);
}

/// <summary>
///   The Windows recipe: <c>UseShellExecute</c> hands the path to the shell, and
///   the file's own association decides the application.
/// </summary>
/// <remarks>
///   Anchored to Windows on purpose — the palette's job is to reach the editor,
///   not to re-implement a launcher per OS.  Porting is one implementation, not
///   a rewrite of the domain: macOS / Linux return <see langword="false" /> here,
///   and a platform copy (Avalonia's <c>ILauncher</c>, WPF's own) registers over
///   this one — the session only ever sees <see cref="IWorkspaceFileOpener" />.
/// </remarks>
[Singleton<IWorkspaceFileOpener>]
public sealed class WindowsWorkspaceFileOpener : IWorkspaceFileOpener
{
  private readonly ILogger<WindowsWorkspaceFileOpener> _logger;

  /// <summary>Creates the opener.</summary>
  public WindowsWorkspaceFileOpener(ILogger<WindowsWorkspaceFileOpener> logger) => _logger = logger;

  /// <inheritdoc />
  public async Task<bool> OpenAsync(string path)
  {
    if (!OperatingSystem.IsWindows())
    {
      _logger.LogWarning("Opening {Path} has no recipe on this platform.", path);
      return false;
    }

    try
    {
      // The hand-off blocks until the shell has committed the launch — off the
      // caller's thread, or a slow association would freeze the UI that asked.
      await Task.Run(() => Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }));
      return true;
    }
    catch (Exception e)
    {
      _logger.LogWarning(e, "Could not open {Path}.", path);
      return false;
    }
  }
}
