using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Everlong.Nester.Shell;
using Everlong.Nester.Presentation;
using NesterApp.Dialogs;
using NesterApp.Properties;

namespace NesterApp.Pages.Shell;

[ViewFor<MainViewModel>]
public partial class MainWindow : Window, IAvaloniaShellHost
{
  public MainWindow()
  {
    InitializeComponent();
    ApplyWindowRect();
    // Opaque window: the leased backdrop layer owns every pixel — no Mica,
    // no layered surface, so subpixel text rendering stays available.
  }

  /// <inheritdoc />
  public void HostShell(IAvaloniaShell shell, Control stage)
  {
    Content = stage;
    Shell = shell;
  }

  private IAvaloniaShell? Shell { get; set; }

  protected override void OnOpened(EventArgs e)
  {
    base.OnOpened(e);
    AddHandler(KeyDownEvent, KeyDownToNavigationIntent, RoutingStrategies.Tunnel, true);
  }

  /// <summary>
  ///   Forwards the window's property changes into the shell's status
  ///   snapshot through the host channel — the shell maps them; the
  ///   window only leaves the channel open (the framework never hooks
  ///   platform events).
  /// </summary>
  protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
  {
    base.OnPropertyChanged(change);
    Shell?.FeedHostPropertyChanged(change);
  }

  private void ApplyWindowRect()
  {
    if (AppSettings.Default.WindowWidth == 0 || AppSettings.Default.WindowHeight == 0)
    {
      var snapshot = AppSettings.Default.Snapshot();
      // first launch, set to some reasonable default size
      snapshot.Shadow.WindowWidth = (int)Width;
      snapshot.Shadow.WindowHeight = (int)Height;
      snapshot.Shadow.WindowLeft = Position.X;
      snapshot.Shadow.WindowTop = Position.Y;
      snapshot.Store.Commit();
    }
    else
    {
      // restore saved window size and position
      Width = AppSettings.Default.WindowWidth;
      Height = AppSettings.Default.WindowHeight;
      Position = new PixelPoint(AppSettings.Default.WindowLeft, AppSettings.Default.WindowTop);
    }
  }

  private void KeyDownToNavigationIntent(object? sender, KeyEventArgs e)
  {
    // The shell declares its platform identity: a View-side type that knows
    // it lives on Avalonia calls platform members directly (IShell members
    // stay available — inherited).

    // Ctrl+Shift+P is the app's own gesture, so the window translates it — the
    // shell's shortcut table belongs to the framework and stays untouched.
    if (e.Key == Key.P
        && (e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Shift))
        == (KeyModifiers.Control | KeyModifiers.Shift))
    {
      e.Handled = true;
      _ = Shell?.DispatchIntent(sender, new ShowWorkspacePaletteIntent());
    }
    else if (e.Key == Key.Escape)
    {
      e.Handled = true;
      _ = Shell?.DispatchIntent(sender, new Everlong.Nester.Routing.BackIntent());
    }
    else
    {
      Shell?.KeyDownToRoutingIntent(sender, e);
    }
  }

  /// <summary>
  /// Translate window closing to TryClose intent for unified arbitration.
  /// Nester never hooks <c>Window.Closing</c>.
  /// (no hidden interception, no black magic).
  /// </summary>
  protected override void OnClosing(WindowClosingEventArgs e)
  {
    base.OnClosing(e);
    Shell?.WindowClosingToTryCloseIntent(e);
    var snapshot = AppSettings.Default.Snapshot();
    snapshot.Shadow.WindowWidth = (int)Width;
    snapshot.Shadow.WindowHeight = (int)Height;
    snapshot.Shadow.WindowLeft = Position.X;
    snapshot.Shadow.WindowTop = Position.Y;
    snapshot.Store.Commit();
  }
}
