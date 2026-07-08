using Everlong.DI;
using Microsoft.Extensions.DependencyInjection;
using NesterApp;
using Xunit;

namespace Everlong.Nester.Tests.Hosting;

/// <summary>
///   The template's <see cref="ServiceBridgeExtensions.BridgeSingleton{T}" />:
///   bridging the OPTIONAL process-level container into a window's own
///   container — explicit cross-window sharing, fail fast on any missing
///   piece (never silent).
/// </summary>
public sealed partial class ServiceBridgeTests
{
  [Fact]
  public void BridgeSingleton_BridgesSharedInstanceFromProcessContainer()
  {
    var process = new ServiceCollection()
      .AddSingleton<MarkerService, MarkerService>()
      .BuildServiceProvider();

    var windowServices = new ServiceCollection();
    windowServices.BridgeSingleton<MarkerService>(process);

    // The window's resolution returns the SAME shared instance (registered
    // by reference — the process container owns its lifetime).
    var window = windowServices.BuildServiceProvider();
    Assert.Same(process.GetRequiredService<MarkerService>(), window.GetRequiredService<MarkerService>());
  }

  [Fact]
  public void BridgeSingleton_NoProcessContainer_FailsFast()
  {
    // Explicit null → falls back to AppLifetime.Current?.Services (null in
    // this harness) → fail fast, never a silent NRE.
    var windowServices = new ServiceCollection();
    var ex = Assert.Throws<InvalidOperationException>(() => windowServices.BridgeSingleton<MarkerService>(null));
    Assert.Contains("process-level container", ex.Message);
  }

  [Fact]
  public void BridgeSingleton_MissingServiceInProcessContainer_FailsFast()
  {
    var process = new ServiceCollection().BuildServiceProvider();   // empty process container

    var windowServices = new ServiceCollection();
    Assert.Throws<InvalidOperationException>(() => windowServices.BridgeSingleton<MarkerService>(process));
  }

  [Fact]
  public void BridgeSingleton_MemberInjectsInjectableInstance()
  {
    // A plain provider resolves an IInjectable instance WITHOUT member injection —
    // the bridge completes it, so window-side resolutions see a whole object.
    var process = new ServiceCollection()
      .AddSingleton<IMarkedDependency, MarkedDependency>()
      .AddSingleton<InjectableMarker>()
      .BuildServiceProvider();

    var windowServices = new ServiceCollection();
    windowServices.BridgeSingleton<InjectableMarker>(process);

    var marker = windowServices.BuildServiceProvider().GetRequiredService<InjectableMarker>();
    Assert.NotNull(marker.Dep);   // member-injected at bridge time (process-level container)
  }

  /// <summary>Any class works — the bridge is generic.</summary>
  private sealed class MarkerService;

  private interface IMarkedDependency;

  private sealed class MarkedDependency : IMarkedDependency;

  private sealed partial class InjectableMarker : IInjectable
  {
    [Inject] public partial IMarkedDependency Dep { get; }
  }
}
