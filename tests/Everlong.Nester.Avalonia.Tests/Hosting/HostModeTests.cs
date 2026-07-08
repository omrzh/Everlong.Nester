using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Everlong.Nester.Presentation;
using Everlong.Nester.Hosting;
using Everlong.Nester.Routing;
using Everlong.Nester.Layer;
using Everlong.Nester.Shell;
using Everlong.Nester.Threading;
using Everlong.Nester.Tests.Shell;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Everlong.Nester.Tests.Hosting;

/// <summary>
///   Host-mode (application container) smoke tests: one application root,
///   every window a scope of it.  Uses an INDEPENDENT AppLifetimeImpl
///   instance — no process-global state, so these tests race nothing.  The
///   DataTemplates setup is process-wide, so they stay in the RealShell
///   collection (template ordering).
/// </summary>
[Collection("RealShell")]
public sealed class HostModeTests
{
  [AvaloniaFact]
  public void HostMode_EachShellOwnsItsContainer()
  {
    // An independent app lifetime (never bound to the static facade): every
    // shell builds and owns its own window container — the host holds no
    // container, the Director assembles its shell's container.
    var impl = new AppLifetimeImpl(new AppLifetimeOptions(), isSingleView: false);
    try
    {
      // Fake visual stack (same as RealShell): every view model maps to a tagged ContentControl.
      Application.Current!.DataTemplates.Clear();
      Application.Current!.DataTemplates.Add(new NesterExtendedViewLocator());
      Application.Current!.DataTemplates.Add(new FakeTemplate());

      AvaloniaShell shell = new TestShell<RealShell.RealTestContext>();

      IServiceProvider window = ((IShell)shell).Services!;

      // Window-level identity: the shell self-registers into its own container.
      Assert.Same(shell, window.GetRequiredService<IShell>());
      Assert.Same(shell, window.GetRequiredService<TestShell<RealShell.RealTestContext>>());
      Assert.Same(shell, window.GetRequiredService<ILayerBroker>());
      Assert.NotNull(window.GetRequiredService<IRouter>());

      // The window container is the shell's own — releasing the shell
      // disposes it (a resolution afterwards fails fast).
      shell.DisposeAsync().GetAwaiter().GetResult();
      Assert.Throws<ObjectDisposedException>(() => window.GetRequiredService<IRouter>());

      // The app lifetime owns the host components only: releasing it is
      // safe even when the shell was never promoted (no SetMainShell).
      impl.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
    finally
    {
      impl.DisposeAsync().AsTask().GetAwaiter().GetResult();
      MainDispatcher.ResetForTesting();
    }
  }

  [AvaloniaFact]
  public void HostMode_OptionalProcessContainer_InvokedAndReleasedAfterShells()
  {
    // Default (no builder): no process container.
    var plain = new AppLifetimeImpl(new AppLifetimeOptions(), isSingleView: false);
    try
    {
      Assert.Null(plain.Services);
    }
    finally
    {
      plain.DisposeAsync().AsTask().GetAwaiter().GetResult();
      MainDispatcher.ResetForTesting();
    }

    // Services = the user-built process-level container: the host receives
    // it, owns the result, and releases it in the teardown cascade (the
    // windows' bridged references die first).
    var disposed = false;
    var sc = new ServiceCollection();
    sc.AddSingleton(sp => new MarkerService(() => disposed = true));
    var process = sc.BuildServiceProvider();
    var impl = new AppLifetimeImpl(new AppLifetimeOptions
    {
      Services = process
    }, isSingleView: false);
    try
    {
      Assert.NotNull(impl.Services);
      Assert.NotNull(impl.Services!.GetRequiredService<MarkerService>());

      impl.DisposeAsync().AsTask().GetAwaiter().GetResult();
      Assert.True(disposed);   // the host released the process container
    }
    finally
    {
      impl.DisposeAsync().AsTask().GetAwaiter().GetResult();
      MainDispatcher.ResetForTesting();
    }
  }


  /// <summary>Dispose-recording process-level marker.</summary>
  private sealed class MarkerService : IDisposable
  {
    private readonly Action _onDispose;

    public MarkerService(Action onDispose) => _onDispose = onDispose;

    public void Dispose() => _onDispose();
  }

  /// <summary>Fake visual stack: every view model maps to a tagged ContentControl.</summary>
  private sealed class FakeTemplate : IDataTemplate
  {
    public Control? Build(object? param)
      => param is null ? null : new TestHostView { Tag = param.GetType() };

    public bool Match(object? data) => true;
  }
}
