using Everlong.Nester.Diagnostics;

namespace Everlong.Nester.Hosting;

/// <summary>
///   Lifetime options for a Terminal.Gui process — the host components the
///   terminal lifetime binds (error handler, optional process container).
/// </summary>
public sealed class AppLifetimeOptions
{
  /// <summary>
  ///   The host-level error handler — retrievable via
  ///   <see cref="AppLifetime.ErrorHandler" />.  When non-<see langword="null" />,
  ///   the framework also binds the global error hooks.
  /// </summary>
  public IErrorHandler? ErrorHandler { get; init; }

  /// <summary>
  ///   The user-built process-level container — <see langword="null" /> = no
  ///   process container.  Released in the teardown cascade after every shell.
  /// </summary>
  public IServiceProvider? Services { get; init; }

  /// <summary>Gets the host-level error handling options.</summary>
  public ErrorHandlingOptions ErrorHandling { get; init; } = new();
}
