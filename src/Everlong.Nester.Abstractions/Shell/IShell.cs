using Everlong.Nester.Diagnostics;
using Everlong.Nester.Hosting;
using Everlong.Nester.Intent;

namespace Everlong.Nester.Shell;

/// <summary>
///   Single entry point for shell operations.
/// </summary>
public interface IShell : IIntentDispatcher, IErrorReporter
{
  /// <summary>
  ///   The shell's lifetime — the current <see cref="ShellLifecycle" />
  ///   state and the teardown signals
  ///   (<see cref="IHostLifetime.Stopping" /> / <see cref="IHostLifetime.Stopped" />).
  /// </summary>
  /// <remarks>Live from construction; stable for this shell's lifetime.</remarks>
  IShellLifetime Lifetime { get; }

  /// <summary>
  ///   Gets the <see cref="IServiceProvider" /> scoped to this shell.
  /// </summary>
  /// <remarks>Live once the container is assembled.</remarks>
  /// <exception cref="InvalidOperationException">Read before assembly completed or after disposal.</exception>
  IServiceProvider Services { get; }

  /// <summary>
  ///   The native handle of the shell's host window, or <c>0</c> when none
  ///   is created.
  /// </summary>
  /// <remarks>Derived on read — a recreated window yields the fresh handle.</remarks>
  nint HostHandle { get; }

  /// <summary>
  ///   Gets an optional platform service for this shell; <see langword="null" />
  ///   when the platform does not provide it.
  /// </summary>
  T? GetPlatformService<T>() where T : class;

  /// <summary>
  ///   Starts the shell synchronously up to the presentation anchor — the
  ///   shell is assembled, mounted and presented.
  /// </summary>
  /// <remarks>
  ///   The asynchronous presentation chain then runs fire-and-forget; its
  ///   completion is observable via <see cref="IShellLifetime.Startup" />.
  /// </remarks>
  void Start();
}
