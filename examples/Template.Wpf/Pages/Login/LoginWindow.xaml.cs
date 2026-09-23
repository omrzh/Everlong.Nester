using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Everlong.Nester.Presentation;
using Everlong.Nester.Shell;

namespace NesterApp.Pages.Login;

/// <summary>
///   The login window — also the custom startup-animation demo surface.
///   This window is chrome-less (WindowStyle=None + AllowsTransparency=True):
///   DWM does not play its native open animation, so the ENTIRE entrance
///   animation is the shell's (the shell's OnStarted →
///   PlayEntranceAnimationAsync).
/// </summary>
[ViewFor<LoginWindowModel>]
public partial class LoginWindow : Window, IWpfShellHost
{
  /// <summary>The window's logical content slot — the shell's stage hosts
  /// here; the window wires this slot into the visual tree when it loads.
  /// Independent of the XAML layout: hosting never waits for the window's
  /// visual readiness.</summary>
  private readonly ContentLayer _contentLayer = new();

  /// <inheritdoc />
  public void HostShell(IWpfShell shell, System.Windows.FrameworkElement stage) => _contentLayer.Content = stage;

  internal IWpfShell? Shell { get; init; }

  public LoginWindow()
  {
    InitializeComponent();
    RootSlot.Content = _contentLayer;   // wire the logical slot into the visual tree (the stage mounts into it at shell start)
    // WPF's Border does not clip its children to CornerRadius: the leased
    // aurora field fills the window rect and would paint square corners.
    RootBorder.SizeChanged += OnRootBorderSizeChanged;
  }

  /// <summary>Clips the window face to its rounded corners.</summary>
  private void OnRootBorderSizeChanged(object sender, SizeChangedEventArgs e)
  {
    double radius = RootBorder.CornerRadius.TopLeft;
    RootBorder.Clip = new RectangleGeometry(new Rect(new Size(e.NewSize.Width, e.NewSize.Height)), radius, radius);
  }

  /// <summary>
  ///   Translate window closing to TryCloseIntent for unified arbitration.
  ///   Nester never hooks <c>Window.Closing</c>.
  /// </summary>
  protected override void OnClosing(CancelEventArgs e)
  {
    base.OnClosing(e);
    Shell?.ClosingToTryCloseIntent(e);
  }

  /// <summary>Drags the chrome-less window — the rounded Border is the drag zone (the close button is not).</summary>
  private void OnDragMove(object sender, MouseButtonEventArgs e)
  {
    if (e.OriginalSource is DependencyObject source && IsInsideButton(source))
      return;
    if (e.ButtonState == MouseButtonState.Pressed)
      DragMove();
  }

  private static bool IsInsideButton(DependencyObject source)
  {
    for (DependencyObject? d = source; d is not null; d = VisualTreeHelper.GetParent(d))
      if (d is Button)
        return true;
    return false;
  }

}
