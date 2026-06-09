using Everlong.Nester.Diagnostics;
namespace Everlong.Nester.Hosting;

/// <summary>
///   The host's lifetime options.
/// </summary>
public sealed class AppLifetimeOptions
{
  /// <summary>
  ///   The host-level error handler, hosted as a managed service on <see cref="IAppLifetime" />
  ///   (retrievable via <see cref="AppLifetime.ErrorHandler" />). When non-<see langword="null" />,
  ///   the framework also binds global error hooks (AppDomain, TaskScheduler, dispatcher).
  /// </summary>
  public IErrorHandler? ErrorHandler { get; init; }

  /// <summary>
  ///   The user-built process-level container — <see langword="null" /> = no
  ///   process container (default).  The user owns the build (cross-window
  ///   shared services, the activation broker); the host receives it and
  ///   releases it in the teardown cascade, after every shell.  Bridging
  ///   process-level services into each window's container is the user
  ///   shell's job (<c>InitializeServices</c>).
  /// </summary>
  public IServiceProvider? Services { get; init; }

  /// <summary>Gets whether the host is disposed when the application exits.</summary>
  public bool DisposeOnExit { get; init; }

  /// <summary>Gets whether the application shuts down only on an explicit request.</summary>
  public bool UseExplicitShutdown { get; init; }

  /// <summary>Gets the host-level error handling options.</summary>
  public ErrorHandlingOptions ErrorHandling { get; init; } = new();
}
