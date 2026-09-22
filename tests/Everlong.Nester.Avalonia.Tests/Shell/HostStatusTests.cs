using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Everlong.Nester.Presentation;
using Everlong.Nester.Intent;
using Everlong.Nester.Primitives;
using Everlong.Nester.Shell;
using Everlong.Nester.Tests.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using PlatformControl = Avalonia.Controls.Control;

namespace Everlong.Nester.Tests.Shell;

/// <summary>
///   Verifies the host status channel: the shell maps the host window's
///   property changes into the <see cref="HostProperty" /> snapshot — the
///   window forwards its own <c>OnPropertyChanged</c> payload through
///   <see cref="IAvaloniaShell.FeedHostPropertyChanged" /> (the framework
///   never hooks platform events).
/// </summary>
public class HostStatusTests
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

  private static (AvaloniaShell Shell, HostProperty Status) CreateShell()
  {
    var shell = TestHost.CreateShell<TestDirector>(
      s => s.AddScoped<HostProperty>(),
      rootView: new HostWindow());
    return (shell, ((IShell)shell).Services!.GetRequiredService<HostProperty>());
  }

  [AvaloniaFact]
  public void FeedHostPropertyChanged_UpdatesTitle()
  {
    var (shell, status) = CreateShell();
    shell.FeedHostPropertyChanged(Capture(w => w.Title = "Hello"));

    Assert.Equal("Hello", status.Title);
  }

  [AvaloniaFact]
  public void FeedHostPropertyChanged_TracksTopmost()
  {
    var (shell, status) = CreateShell();
    shell.FeedHostPropertyChanged(Capture(w => w.Topmost = true));

    Assert.True(status.TopMost);
  }

  [AvaloniaFact]
  public void FeedHostPropertyChanged_TracksShellStateAndLast()
  {
    var (shell, status) = CreateShell();
    shell.FeedHostPropertyChanged(Capture(w => w.WindowState = WindowState.Minimized));

    Assert.Equal(HostState.Minimized, status.HostState);
    Assert.Equal(HostState.Normal, status.LastHostState);
  }

  [AvaloniaFact]
  public void FeedHostPropertyChanged_TracksVisibility()
  {
    var (shell, status) = CreateShell();
    shell.FeedHostPropertyChanged(CaptureVisible(w => w.Show()));

    Assert.True(status.IsVisible);
  }

  [AvaloniaFact]
  public void HostHandle_NoWindowCreated_IsZero()
  {
    var (shell, _) = CreateShell();

    // Not shown yet — no native handle (derived on read).
    Assert.Equal(nint.Zero, shell.HostHandle);
  }
}
