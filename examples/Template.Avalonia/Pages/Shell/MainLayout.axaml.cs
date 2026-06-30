using Avalonia;
using Avalonia.Controls;
using Everlong.Nester.Presentation;
using Everlong.Nester.Primitives;
using Everlong.Nester.Shell;

namespace NesterApp.Pages.Shell;

[ViewFor<MainLayoutModel>]
public partial class MainLayout : UserControl, ISceneTransition
{
  private TopLevel? _topLevel;

  public MainLayout()
  {
    InitializeComponent();
  }

  public Task AnimateEnterAsync(TransitionContext context, CancellationToken token)
    => this.PassThroughAsync(context, token);

  public Task AnimateExitAsync(TransitionContext context, CancellationToken token)
    => this.PassExitAsync(context, token);

  /// <summary>Tracks the host window so the maximize glyph follows its state.</summary>
  protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
  {
    base.OnAttachedToVisualTree(e);
    _topLevel = TopLevel.GetTopLevel(this);
    _topLevel?.PropertyChanged += OnTopLevelPropertyChanged;
    UpdateMaximizeIcon();
  }

  /// <inheritdoc />
  protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
  {
    _topLevel?.PropertyChanged -= OnTopLevelPropertyChanged;
    _topLevel = null;
    base.OnDetachedFromVisualTree(e);
  }

  private void OnTopLevelPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
  {
    if (e.Property == Window.WindowStateProperty)
      UpdateMaximizeIcon();
  }

  private void UpdateMaximizeIcon()
    => MaximizeIcon.Text = _topLevel is Window { WindowState: WindowState.Maximized } ? "\uE923" : "\uE922";

  private void OnMinimizeClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    => this.PostIntent(new MutateShellStateIntent(HostState.Minimized));

  private void OnMaximizeClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    => this.PostIntent(new MutateShellStateIntent(
                         _topLevel is Window { WindowState: WindowState.Maximized }
                           ? HostState.Normal
                           : HostState.Maximized));

  private void OnCloseClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    => this.PostIntent(new TryCloseIntent());
}
