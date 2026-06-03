// NOTE: Single-source file — the WPF project compiles this exact file via
// <Compile Include> in Everlong.Nester.Wpf.csproj.  Edit it here only;
// never create a WPF-side copy (the two builds would drift).
using Everlong.Nester.Hosting;
using Everlong.Nester.Intent;
using Everlong.Nester.Layer;
using Everlong.Nester.Routing;
using Everlong.Nester.Shell;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Everlong.Nester.DI;

/// <summary>
///   DI registration for the shell and its core infrastructure.
/// </summary>
public static partial class ServiceCollectionExtensions
{
  /// <param name="services">The service collection.</param>
  extension(IServiceCollection services)
  {
    /// <summary>
    ///   Registers the shell's identity in its own window container:
    ///   <see cref="IShell" />, <see cref="ILayerBroker" />, the
    ///   concrete shell type, the shell's <see cref="IShellLifetime" /> and
    ///   the shell as the container's <see cref="IHostLifetime" />.
    ///   Call first in <c>InitializeServices</c>, before the user's
    ///   registrations.
    ///   <para>
    ///     The platform face (<c>IAvaloniaShell</c> / <c>IWpfShell</c>) is
    ///     NOT registered here — it derives from <see cref="IShell" />, so
    ///     the concrete shell instance is resolvable as either contract
    ///     from the same registration.
    ///   </para>
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
      if (shell.ActivationAgent is { } agent)
      {
        // The declared agent — resolvable from this shell's container (the
        // shell keeps ownership; a container registration is a lookup).
        services.AddSingleton(agent);
      }

      return services;
    }

    /// <summary>
    ///   Registers the shell's core infrastructure — the window-level
    ///   services every shell needs: logging (Null fallbacks), the
    ///   Everlong.DI injector (Scoped — member injection resolves from the
    ///   scope the instance came from), the flying layer (the shared
    ///   transition canvas) and the exit-visual service.
    ///   Overriding is done by registering your own service before this call
    ///   (TryAdd semantics).
    /// </summary>
    public IServiceCollection AddNesterCore()
    {
      services.TryAddSingleton<ILoggerFactory, NullLoggerFactory>();

      // open generic is potential AOT threat
      services.TryAddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

      // InjectorServiceProvider from Everlong.DI — wraps IServiceProvider + auto-calls IInjectable.Inject.
      // Scoped on purpose: every scope (the window scope, user session
      // scopes) gets a wrapper over ITSELF — member injection must resolve
      // from the scope the instance came from.
      services.AddInjector(ServiceLifetime.Scoped);

      // The shared transition canvas — the navigation and dialog
      // domains inject it.  Exit visuals for the tenant-return path
      // (DismissAsync) — replaceable in tests to control animation timing.
      // Stateless: safe as an application-level singleton.
      services.AddSingleton<ShellFlyingLayer>();

      // Routing domain — the router machinery: MS DI scopes carry no
      // tree semantics, so the scope's router is seeded before it resolves
      // (Derive initializes the child scope's seed, then
      // resolves its router).
      services.TryAddScoped<IRouterSeed, RouterSeed>();
      services.TryAddScoped<IRouter, Router>();
      return services;
    }

  }
}
