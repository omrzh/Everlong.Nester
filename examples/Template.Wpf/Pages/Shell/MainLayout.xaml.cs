using System.Windows;
using System.Windows.Controls;
using Everlong.Nester.Chrome;
using Everlong.Nester.Presentation;
using Everlong.Nester.Shell;
using Everlong.Nester.Primitives;

namespace NesterApp.Pages.Shell;

[ViewFor<MainLayoutModel>]
public partial class MainLayout : UserControl, ILayoutControl, ISceneTransition
{
  private Window? _hostWindow;

  public MainLayout()
  {
    InitializeComponent();
    Loaded += OnLoaded;
    Unloaded += OnUnloaded;
  }

  public ILayoutBody GetLayoutBody() => Body;

  public Task AnimateEnterAsync(TransitionContext context, CancellationToken token)
    => this.PassThroughAsync(context, token);

  public Task AnimateExitAsync(TransitionContext context, CancellationToken token)
    => this.PassExitAsync(context, token);

  /// <summary>
  ///   Wires the caption buttons to the host window: the maximize button is
  ///   handed to the chrome helper (system snap-layout flyout on hover), the
  ///   glyph follows the window state.
  /// </summary>
  private void OnLoaded(object sender, RoutedEventArgs e)
  {
    _hostWindow = Window.GetWindow(this);
    if (_hostWindow is null)
      return;

    NesterWindowChrome.SetSnapLayoutButton(_hostWindow, MaximizeButton);
    _hostWindow.StateChanged += OnHostStateChanged;
    UpdateMaximizeIcon();
  }

  private void OnUnloaded(object sender, RoutedEventArgs e)
  {
    _hostWindow?.StateChanged -= OnHostStateChanged;
    _hostWindow = null;
  }

  private void OnHostStateChanged(object? sender, EventArgs e) => UpdateMaximizeIcon();

  private void UpdateMaximizeIcon()
    => MaximizeIcon.Text = _hostWindow?.WindowState == WindowState.Maximized ? "\uE923" : "\uE922";

  private void OnMinimizeClicked(object sender, RoutedEventArgs e)
    => this.PostIntent(new MutateShellStateIntent(HostState.Minimized));

  private void OnMaximizeClicked(object sender, RoutedEventArgs e)
    => this.PostIntent(new MutateShellStateIntent(
                         _hostWindow?.WindowState == WindowState.Maximized ? HostState.Normal : HostState.Maximized));

  private void OnCloseClicked(object sender, RoutedEventArgs e)
    => this.PostIntent(new TryCloseIntent());
}
