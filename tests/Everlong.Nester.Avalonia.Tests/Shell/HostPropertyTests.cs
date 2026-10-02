using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Everlong.Nester.Intent;
using Everlong.Nester.Presentation;
using Everlong.Nester.Primitives;
using Everlong.Nester.Shell;
using Everlong.Nester.Tests.Hosting;
using Xunit;
using PlatformControl = Avalonia.Controls.Control;

namespace Everlong.Nester.Tests.Shell;

/// <summary>
///   Verifies the host status snapshot: <see cref="HostProperty.Feed" /> maps
///   the host window's property changes into the snapshot — the window forwards
///   its own <c>OnPropertyChanged</c> payload (the framework never hooks
///   platform events).
/// </summary>
public class HostPropertyTests
{
  private sealed class TestDirector : IShellDirector
  {

    public ValueTask HandleAsync(IntentContext context, IntentDelegate next) => next(context);

    public void OnAssembled(IShell shell) { }
    public bool HandleError(Exception exception) => true;
  }

  private sealed class HostWindow : Window, IAvaloniaShellHost
  {
    private readonly ContentLayer _contentLayer = new();

    public void HostShell(IAvaloniaShell shell, PlatformControl stage) => _contentLayer.Content = stage;
  }

  private static AvaloniaPropertyChangedEventArgs Capture(Action<Window> mutate)
  {
    var window = new Window();
    AvaloniaPropertyChangedEventArgs? captured = null;
    window.PropertyChanged += (_, e) => captured = e;
    mutate(window);
    return captured!;
  }

  private static AvaloniaPropertyChangedEventArgs CaptureVisible(Action<Window> mutate)
  {
    var window = new Window();
    AvaloniaPropertyChangedEventArgs? captured = null;
    window.PropertyChanged += (_, e) =>
    {
      if (e.Property == Visual.IsVisibleProperty)
        captured = e;
    };
    mutate(window);
    return captured!;
  }

  [AvaloniaFact]
  public void Feed_UpdatesTitle()
  {
    var status = new HostProperty();
    status.Feed(Capture(w => w.Title = "Hello"));

    Assert.Equal("Hello", status.Title);
  }

  [AvaloniaFact]
  public void Feed_TracksTopmost()
  {
    var status = new HostProperty();
    status.Feed(Capture(w => w.Topmost = true));

    Assert.True(status.TopMost);
  }

  [AvaloniaFact]
  public void Feed_TracksShellStateAndLast()
  {
    var status = new HostProperty();
    status.Feed(Capture(w => w.WindowState = WindowState.Minimized));

    Assert.Equal(HostState.Minimized, status.HostState);
    Assert.Equal(HostState.Normal, status.LastHostState);
  }

  [AvaloniaFact]
  public void Feed_TracksVisibility()
  {
    var status = new HostProperty();
    status.Feed(CaptureVisible(w => w.Show()));

    Assert.True(status.IsVisible);
  }

  [AvaloniaFact]
  public void Prime_SeedsCurrentState()
  {
    var window = new Window { Title = "Seeded", Topmost = true };
    var status = new HostProperty();

    status.Prime(window);

    Assert.Equal("Seeded", status.Title);
    Assert.True(status.TopMost);
    Assert.False(status.IsVisible);
    Assert.Equal(HostState.Normal, status.HostState);
    Assert.Equal(HostState.Normal, status.LastHostState);
  }

  [AvaloniaFact]
  public void HostHandle_NoWindowCreated_IsZero()
  {
    var shell = TestHost.CreateShell<TestDirector>(rootView: new HostWindow());

    // Not shown yet — no native handle (derived on read).
    Assert.Equal(nint.Zero, shell.HostHandle);
  }
}
