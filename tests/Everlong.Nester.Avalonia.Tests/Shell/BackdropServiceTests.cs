using Avalonia.Headless.XUnit;
using Everlong.DI;
using Everlong.Nester.Intent;
using Everlong.Nester.Messaging;
using Everlong.Nester.Shell;
using Everlong.Nester.Tests.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NesterApp;
using NesterApp.Backdrop;
using NesterApp.Pages.Shell;
using NesterApp.Services;
using Xunit;

namespace Everlong.Nester.Tests.Shell;

/// <summary>
///   The backdrop domain's tenant contract: the shell rents nothing itself —
///   the service owns the leases, answers its own intent and broadcasts the
///   result.  The surfaces are fake (the test project compiles Template.Shared
///   only), the band accounting is real.
/// </summary>
[Collection("RealShell")]
public sealed class BackdropServiceTests
{
  /// <summary>Stands in for the platform surfaces — the broker mounts any object.</summary>
  private sealed class FakeSurfaces : IBackdropSurfaceFactory
  {
    public object CreateField(BackdropLab lab) => new();

    public object CreatePanel(BackdropLab lab) => new();
  }

  private sealed class Recorder : IMessageRecipient
  {
    public List<bool> Received { get; } = [];

    public bool CanReceive(IMessage message) => message is BackdropPanelVisibilityChangedMessage;

    public void Receive(IMessage message)
      => Received.Add(((BackdropPanelVisibilityChangedMessage)message).IsVisible);
  }

  private static AvaloniaShell BuildShell()
    => TestHost.CreateShell<MainViewModel>(s =>
    {
      TestAppServices.AddWindowAuth(s);
      s.AddSingleton<IBackdropSurfaceFactory>(new FakeSurfaces());
      s.AddServices(new AppServices());
    });

  [AvaloniaFact]
  public async Task ToggleIntent_FlipsThePanelLease_AndBroadcasts()
  {
    var recorder = new Recorder();
    AvaloniaShell shell = BuildShell();
    shell.Start();
    await shell.Lifetime.Startup;

    var backdrop = shell.Services!.GetRequiredService<BackdropService>();
    backdrop.Mount(BackdropProfile.Main);
    Assert.True(backdrop.IsPanelVisible);

    using IDisposable subscription = shell.Services!.GetRequiredService<IMessageHub>().Register(recorder);

    IntentResult result = await shell.DispatchIntent(null, new ToggleBackdropPanelIntent());

    Assert.Equal(IntentResult.Handled, result);
    Assert.False(backdrop.IsPanelVisible);
    Assert.Contains(false, recorder.Received);

    await shell.DisposeAsync();
  }

  [AvaloniaFact]
  public async Task ToggleIntent_PassesThrough_WhenTheProfileHasNoPanel()
  {
    var shell = BuildShell();
    shell.Start();
    await shell.Lifetime.Startup;

    var backdrop = shell.Services!.GetRequiredService<BackdropService>();
    backdrop.Mount(BackdropProfile.Login);

    Assert.False(backdrop.IsPanelVisible);
    Assert.Equal(IntentResult.Pass, await shell.DispatchIntent(null, new ToggleBackdropPanelIntent()));

    await shell.DisposeAsync();
  }
}
