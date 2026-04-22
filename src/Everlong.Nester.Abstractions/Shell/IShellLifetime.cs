using Everlong.Nester.Hosting;

namespace Everlong.Nester.Shell;

/// <summary>
///   The shell's lifecycle state and its startup / teardown signals.
/// </summary>
/// <remarks>
///   Live from the shell's construction; one shell-owned instance — never
///   construct it.
/// </remarks>
public interface IShellLifetime : IHostLifetime
{
  /// <summary>The shell's current lifecycle state.</summary>
  ShellLifecycle Lifecycle { get; }

  /// <summary>
  ///   The startup flow's completion signal — completes when the async
  ///   startup process settles.
  /// </summary>
  /// <remarks>
  ///   Always available; faulted when startup failed, or when the shell is
  ///   disposed before the startup flow settled.
  /// </remarks>
  Task Startup { get; }
}
