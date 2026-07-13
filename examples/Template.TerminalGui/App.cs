using Everlong.Nester.Auth;
using Everlong.Nester.Diagnostics;
using Everlong.Nester.Hosting;
using Everlong.Nester.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NesterApp;

/// <summary>
///   The Terminal.Gui app surface — the process-level container builder and
///   the host-level error handler.
/// </summary>
public sealed class App : IErrorHandler
{
  /// <summary>Builds the process-level container (cross-window shared services).</summary>
  public static IServiceProvider BuildAppServices()
  {
    var services = new ServiceCollection();

    // Cross-surface message hub — process-level (the shell bridges it into
    // its own container).
    services.AddSingleton<IMessageHub>(new MessageHub());

    services.AddNesterAuth(options =>
    {
      options.AddPolicy("Admin", p => p.RequireRole("Admin"));
    });

    return services.BuildServiceProvider(new ServiceProviderOptions
    {
      ValidateScopes = true,
      ValidateOnBuild = true
    });
  }

  public bool HandleError(Exception exception)
  {
    AppLifetime.MainShell?.Services.GetService<ILogger<App>>()
      ?.LogCritical(exception, "Unhandled host error");
    Console.Error.WriteLine(exception);
    return true;
  }
}
