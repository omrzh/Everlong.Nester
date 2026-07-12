using Everlong.DI;
using Everlong.Nester.Diagnostics;
using Everlong.Nester.Hosting;
using Everlong.Nester.Intent;
using Everlong.Nester.Layer;
using Everlong.Nester.Notice;
using Everlong.Nester.Routing;
using Everlong.Nester.Shell;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Everlong.Nester.DI;

/// <summary>
///   DI registration for the shell, its core infrastructure and the notice
///   domain.
/// </summary>
public static partial class ServiceCollectionExtensions
{
  /// <param name="services">The service collection.</param>
  extension(IServiceCollection services)
  {
    /// <summary>
    ///   Registers the shell's identity in its own window container:
    ///   <see cref="IShell" />, <see cref="ILayerBroker" />, the concrete
    ///   shell type, the shell's <see cref="IShellLifetime" /> and the shell
    ///   as the container's <see cref="IHostLifetime" />.
    /// </summary>
    /// <param name="shell">The user shell — the window's concrete <see cref="ShellBase" />.</param>
    public IServiceCollection AddNesterShell(ShellBase shell)
    {
      services.AddSingleton<IShell>(shell);
      services.AddSingleton<ILayerBroker>(shell);
      services.AddSingleton<IErrorReporter>(shell);
      services.AddSingleton<IIntentDispatcher>(shell);
      services.AddSingleton(shell.GetType(), shell);
      services.AddSingleton<IShellLifetime>(shell.Lifetime);
      services.AddSingleton<IHostLifetime>(shell.Lifetime);
      return services;
    }

    /// <summary>
    ///   Registers the shell's core infrastructure — logging (Null
    ///   fallbacks), the Everlong.DI injector (Scoped) and the routing
    ///   machinery (seed + scoped router).
    /// </summary>
    public IServiceCollection AddNesterCore()
    {
      services.TryAddSingleton<ILoggerFactory, NullLoggerFactory>();
      services.TryAddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
      services.AddInjector(ServiceLifetime.Scoped);

      // Routing domain — the router machinery: MS DI scopes carry no
      // tree semantics, so the scope's router is seeded before it resolves.
      services.TryAddScoped<IRouterSeed, RouterSeed>();
      services.TryAddScoped<IRouter, TerminalRouter>();
      return services;
    }

    /// <summary>Registers the notice surface — the terminal notice tenant (one per window).</summary>
    public IServiceCollection AddNesterNotice()
    {
      services.TryAddSingleton(new NoticeServiceOptions());
      services.AddSingleton<TerminalNoticeService>();
      services.AddSingleton<IToastService>(sp => sp.GetRequiredService<TerminalNoticeService>());
      services.AddSingleton<ISnackbarService>(sp => sp.GetRequiredService<TerminalNoticeService>());
      services.AddSingleton<INotificationService>(sp => sp.GetRequiredService<TerminalNoticeService>());
      services.AddSingleton<INoticeService>(sp => sp.GetRequiredService<TerminalNoticeService>());
      return services;
    }
  }
}
