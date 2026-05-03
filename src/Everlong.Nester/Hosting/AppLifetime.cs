using System.Diagnostics;
using Everlong.Nester.Diagnostics;
using Everlong.Nester.Shell;
using Everlong.Nester.Threading;

namespace Everlong.Nester.Hosting;

/// <summary>
///   Static facade over the bound <see cref="IAppLifetime" /> instance.
/// </summary>
public static partial class AppLifetime
{
  /// <summary>
  ///   Gets the currently bound runtime lifetime instance.
  /// </summary>
  public static IAppLifetime? Current { get; private set; }

  /// <summary>
  ///   Gets the platform main-thread dispatcher.
  /// </summary>
  public static IMainDispatcher Dispatcher => RequireAdapter().Dispatcher;

  /// <summary>
  ///   Gets the host-level error handler, when configured.
  /// </summary>
  public static IErrorHandler? ErrorHandler => RequireAdapter().ErrorHandler;

  /// <summary>
  ///   Gets the process-level container (cross-window shared services) —
  ///   <see langword="null" /> when none was configured.
  /// </summary>
  public static IServiceProvider? Services => RequireAdapter().Services;

  /// <summary>
  ///   Gets a value indicating whether the app is running in a single-view model.
  /// </summary>
  /// <remarks>
  ///   <see langword="false" /> until a runtime lifetime is bound.
  /// </remarks>
  public static bool IsSingleView => Current?.IsSingleView == true;

  /// <summary>
  ///   Gets the startup arguments of the current process (excluding the executable path).
  /// </summary>
  public static IReadOnlyList<string> Args
  {
    get
    {
      string[] startupArgs = Environment.GetCommandLineArgs();
      return startupArgs.Length <= 1 ? [] : startupArgs[1..];
    }
  }

  /// <summary>
  ///   Restarts the current process with the specified startup arguments.
  /// </summary>
  public static void RestartApp(IReadOnlyList<string> startupArgs)
  {
    string? processPath = Environment.ProcessPath;
    if (string.IsNullOrWhiteSpace(processPath))
    {
      throw new InvalidOperationException("Current process path is unavailable.");
    }

    ProcessStartInfo startInfo = new(processPath)
    {
      UseShellExecute = false
    };

    foreach (string startupArg in startupArgs)
    {
      startInfo.ArgumentList.Add(startupArg);
    }

    Process.Start(startInfo);
  }

  /// <summary>
  ///   Gets a cancellation token that is triggered when the application begins
  ///   its teardown.
  /// </summary>
  /// <remarks>
  ///   Available once the platform lifetime is bound.
  /// </remarks>
  public static CancellationToken Stopping => RequireAdapter().Stopping;

  /// <summary>
  ///   Gets a cancellation token that is triggered when the application's
  ///   teardown cascade has completed.
  /// </summary>
  /// <remarks>
  ///   Available once the platform lifetime is bound.
  /// </remarks>
  public static CancellationToken Stopped => RequireAdapter().Stopped;

  /// <summary>
  ///   Gets the current main shell.
  /// </summary>
  public static IShell? MainShell => Current?.MainShell;

  /// <summary>
  ///   Promotes the shell as the application's main shell.
  /// </summary>
  public static void SetMainShell(IShell shell) => RequireAdapter().SetMainShell(shell);

  /// <summary>
  ///   Binds the platform adapter and root service provider to this static facade.
  ///   Called once per process from the platform-specific <c>BindLifetime</c> extension method.
  /// </summary>
  internal static void BindImpl(IAppLifetime impl)
  {
    ArgumentNullException.ThrowIfNull(impl);
    if (Current != null && !ReferenceEquals(Current, impl))
    {
      throw new InvalidOperationException("A different adapter is already bound to AppLifetime.");
    }

    if (ReferenceEquals(Current, impl))
    {
      return; // idempotent
    }

    Current = impl;
  }

  private static IAppLifetime RequireAdapter()
  {
    return Current ?? throw new InvalidOperationException(
             "AppLifetime is not initialized. Call BindLifetime before using AppLifetime.");
  }

#if DEBUG
  /// <summary>Resets the static adapter for test isolation. For use in tests only.</summary>
  internal static void ResetForTesting()
  {
    Current = null;
  }
#endif
}

/// <summary>
///   Public helper APIs over <see cref="IAppLifetime" /> runtime instances.
/// </summary>
public static class IAppLifetimeExtensions
{
  extension(IAppLifetime lifetime)
  {
    /// <summary>
    ///   Binds this runtime instance to the <see cref="AppLifetime" /> static facade.
    ///   Intended for platform bootstrap code rather than normal application logic.
    /// </summary>
    public void BindStaticFacades()
    {
      AppLifetime.BindImpl(lifetime);
    }
  }
}
