using Everlong.Nester.Diagnostics;
using Everlong.Nester.Shell;
using Everlong.Nester.Threading;

namespace Everlong.Nester.Hosting;

/// <summary>
///   The host surface — main-shell tracking, the host components and the
///   process lifecycle.
/// </summary>
public interface IAppLifetime : IHostLifetime, IAsyncDisposable
{
  /// <summary>Gets the current main shell, if any.</summary>
  IShell? MainShell { get; }

  /// <summary>
  ///   Tracks a shell for the app-exit teardown cascade.
  /// </summary>
  /// <remarks>
  ///   Idempotent: tracking a shell that was already released is a no-op.
  ///   Throws when the lifetime is already shutting down.
  /// </remarks>
  void Track(IShell shell);

  /// <summary>
  ///   Removes a shell from the app-exit teardown cascade.
  /// </summary>
  /// <remarks>
  ///   Idempotent: untracking a shell that was never tracked is a no-op.
  ///   After completion, the cascade never disposes the shell again.
  /// </remarks>
  ValueTask UntrackAsync(IShell shell);

  /// <summary>
  ///   Promotes the shell as the application's main shell.
  /// </summary>
  /// <remarks>Declaration only — it does not present the shell.</remarks>
  /// <param name="shell">The shell to promote — an active shell.</param>
  void SetMainShell(IShell shell);

  /// <summary>
  ///   Whether the app runs in a single-view model.
  /// </summary>
  bool IsSingleView { get; }

  /// <summary>
  ///   Gets the platform main-thread dispatcher.
  /// </summary>
  IMainDispatcher Dispatcher { get; }

  /// <summary>
  ///   Gets the host-level error handler, when configured.
  /// </summary>
  IErrorHandler? ErrorHandler { get; }

  /// <summary>
  ///   The process-level container, when configured;
  ///   <see langword="null" /> by default.
  /// </summary>
  /// <remarks>Released after every shell during the teardown cascade.</remarks>
  IServiceProvider? Services { get; }
}
