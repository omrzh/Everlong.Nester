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

  /// <summary>Gets the host-level error handling options.</summary>
  public ErrorHandlingOptions ErrorHandling { get; init; } = new();

  /// <summary>Gets the desktop lifetime options.</summary>
  public DesktopOptions? Desktop { get; init; }

  /// <summary>Gets the browser lifetime options.</summary>
  public BrowserOptions? Browser { get; init; }

  /// <summary>Gets the Android lifetime options.</summary>
  public AndroidOptions? Android { get; init; }

  /// <summary>Gets the iOS lifetime options.</summary>
  public IOSOptions? IOS { get; init; }
}

/// <summary>The desktop lifetime options.</summary>
public class DesktopOptions
{
  /// <summary>Gets whether the host is disposed when the desktop lifetime exits.</summary>
  public bool DisposeOnExit { get; init; }

  /// <summary>Gets whether the desktop lifetime shuts down only on an explicit request.</summary>
  public bool UseExplicitShutdown { get; init; }
}

/// <summary>The browser lifetime options.</summary>
public class BrowserOptions
{
}

/// <summary>The Android lifetime options.</summary>
public class AndroidOptions
{
}

/// <summary>The iOS lifetime options.</summary>
public class IOSOptions
{
}
