using Avalonia;
using Avalonia.Headless.XUnit;
using Everlong.DI;
using Everlong.Nester.Routing;
using Everlong.Nester.Shell;
using Everlong.Nester.Tests.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NesterApp;
using NesterApp.Pages.Landing;
using NesterApp.Pages.Profile;
using NesterApp.Pages.Shell;
using Xunit;

namespace Everlong.Nester.Tests.Shell;

/// <summary>
///   The one-shot surface contract: a derived router created with
///   <c>isEphemeral: true</c> accepts one route request and hands every later
///   one off — a navigable surface takes it, and stacked one-shot surfaces
///   never hand off to each other.
/// </summary>
[Collection("RealShell")]
public sealed class EphemeralRouterTests
{
  private static AvaloniaShell BuildShell()
  {
    Application.Current!.DataTemplates.Clear();
    Application.Current!.DataTemplates.Add(RealAppHarness.PageTemplate);

    return TestHost.CreateShell<MainViewModel>(s =>
    {
      TestAppServices.AddWindowAuth(s);
      s.AddServices(new AppServices());
    });
  }

  [AvaloniaFact]
  public async Task SecondRoute_LeavesTheOneShotSurface_AndLandsOnTheBase()
  {
    AvaloniaShell shell = BuildShell();
    shell.Start();
    await shell.Lifetime.Startup;

    IRouter ground = shell.Services!.GetRequiredService<IRouter>();
    IRouter overlay = ground.Derive(isEphemeral: true);

    await overlay.RouteAsync(new LandingLocator());
    await overlay.RouteAsync(new ProfileLocator());

    // The overlay kept its single entry; the hand-off landed on the ground.
    Assert.Equal(1, overlay.Stack.Count);
    Assert.IsType<ProfilePageModel>(ground.Stack.Location?.Instance);

    await shell.DisposeAsync();
  }

  [AvaloniaFact]
  public async Task StackedOneShotSurfaces_DoNotHandOffToEachOther()
  {
    AvaloniaShell shell = BuildShell();
    shell.Start();
    await shell.Lifetime.Startup;

    IRouter ground = shell.Services!.GetRequiredService<IRouter>();
    IRouter lower = ground.Derive(isEphemeral: true);
    IRouter upper = ground.Derive(isEphemeral: true);

    await lower.RouteAsync(new LandingLocator());
    await upper.RouteAsync(new LandingLocator());
    await upper.RouteAsync(new ProfileLocator());

    // Neither one-shot surface grew — the hand-off skipped both.
    Assert.Equal(1, lower.Stack.Count);
    Assert.Equal(1, upper.Stack.Count);
    Assert.IsType<ProfilePageModel>(ground.Stack.Location?.Instance);

    await shell.DisposeAsync();
  }

  [AvaloniaFact]
  public async Task A_NavigableDerivedSurface_TakesTheHandOff_BeforeTheBase()
  {
    AvaloniaShell shell = BuildShell();
    shell.Start();
    await shell.Lifetime.Startup;

    IRouter ground = shell.Services!.GetRequiredService<IRouter>();
    IRouter navigable = ground.Derive();
    await navigable.RouteAsync(new LandingLocator());

    IRouter overlay = ground.Derive(isEphemeral: true);
    await overlay.RouteAsync(new LandingLocator());
    await overlay.RouteAsync(new ProfileLocator());

    // The open navigable surface is above the ground and took the request.
    Assert.Equal(2, navigable.Stack.Count);
    Assert.IsType<ProfilePageModel>(navigable.Stack.Location?.Instance);
    Assert.IsType<LandingPageModel>(ground.Stack.Location?.Instance);

    await shell.DisposeAsync();
  }
}
