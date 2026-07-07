using Everlong.Nester.Auth;
using Everlong.Nester.Messaging;
using Microsoft.Extensions.DependencyInjection;
using NesterApp.Backdrop;
using NesterApp.Services;

namespace Everlong.Nester.Tests.Hosting;

/// <summary>
///   The harness's window-level auth/http registrations — the test shells
///   have no process container (no <c>AppLifetimeOptions.Services</c> in tests), so the
///   window container supplies its own <c>IAuthService</c> + http client
///   per window — the harness does not exercise the template's
///   process-level Auth + bridging.
/// </summary>
internal static class TestAppServices
{
  internal static void AddWindowAuth(IServiceCollection services)
  {
    services.AddNesterAuth(options =>
    {
      options.AddPolicy("Admin", p => p.RequireRole("Admin"));
    });
    services.AddHttpClient<JsonPlaceholderService>();
    // The template bridges the hub from its process container; the harness has
    // no process container, so the window supplies its own.
    services.AddSingleton<IMessageHub>(new MessageHub());
    // The backdrop domain needs platform surfaces; the harness supplies inert
    // stand-ins (the suites that mount the domain register their own).
    services.AddSingleton<IBackdropSurfaceFactory>(new TestBackdropSurfaces());
  }

  /// <summary>Inert backdrop surfaces — the broker mounts any object, no rendering happens in tests.</summary>
  private sealed class TestBackdropSurfaces : IBackdropSurfaceFactory
  {
    public object CreateField(BackdropLab lab) => new();

    public object CreatePanel(BackdropLab lab) => new();
  }
}
