using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using Everlong.Nester.Controls;
using Everlong.Nester.Shell;
using Everlong.Nester.Presentation;

namespace NesterApp.Pages.Login;

/// <summary>
///   The login window — also the custom startup-animation demo surface.
///   This window is chrome-less (SystemDecorations=None): the platform does
///   not play its own open animation, so the ENTIRE entrance animation is
///   the shell's (the shell's OnStarted → PlayEntranceAnimationAsync).
/// </summary>
[ViewFor<LoginWindowModel>]
public partial class LoginWindow : Window, IAvaloniaShellHost
{
  /// <summary>The window's logical content slot — the shell's stage hosts
  /// here; the window wires this slot into the visual tree when it loads.
  /// Independent of the XAML layout: hosting never waits for the window's
  /// visual readiness.</summary>
  private readonly ContentLayer _contentLayer = new();

  public LoginWindow()
  {
    InitializeComponent();
    RootSlot.Content =
      _contentLayer; // wire the logical slot into the visual tree (the stage mounts into it at shell start)
    if (OperatingSystem.IsWindows())
    {
      // ExtendClientAreaToDecorationsHint = true;
      // ExtendClientAreaTitleBarHeightHint = -1;
      this.WindowDecorations = WindowDecorations.None;
      // A layered surface keeps the rounded card; no Mica material — the
      // card's own background is the whole face.
      TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
      Background = Brushes.Transparent;
    }
    else
    {
      this.WindowDecorations = WindowDecorations.None;
      Background = Brushes.Transparent;
      TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
    }
  }

  /// <inheritdoc />
  public void HostShell(IAvaloniaShell shell, Avalonia.Controls.Control stage)
  {
    _contentLayer.Content = stage;
    _shell ??= shell;
  }

  /// <summary>The owning shell — wired by the bootstrap (init) or at HostShell; the window sits above the stage, so it never crawls for it.</summary>
  internal IAvaloniaShell Shell
  {
    get => _shell!;
    init => _shell = value;
  }

  private IAvaloniaShell? _shell;

  private void OnCloseClicked(object? sender, RoutedEventArgs e) => Close();

  /// <summary>
  ///   Translate window closing to TryCloseIntent for unified arbitration.
  ///   Nester never hooks <c>Window.Closing</c>.
  /// </summary>
  protected override void OnClosing(WindowClosingEventArgs e)
  {
    base.OnClosing(e);
    Shell.WindowClosingToTryCloseIntent(e);
  }

  /// <summary>Drags the chrome-less window — the rounded Border is the drag zone (the close button is not).</summary>
  private void OnDragMove(object? sender, PointerPressedEventArgs e)
  {
    if (e.Source is Control source && source.FindAncestorOfType<Button>() is not null)
      return;
    BeginMoveDrag(e);
  }

  protected override void OnOpened(EventArgs e)
  {
    base.OnOpened(e);
    if (OperatingSystem.IsWindows())
    {
      Background = Brushes.Transparent;
      TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
    }
    else if (OperatingSystem.IsLinux())
    {
      // Do something else
    }
  }
}
