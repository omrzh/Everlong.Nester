using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Everlong.Nester.Chrome;
using Everlong.Nester.Routing;
using Everlong.Nester.Shell;
using Everlong.Nester.Presentation;
using NesterApp.Dialogs;

namespace NesterApp.Pages.Shell;

[ViewFor<MainViewModel>]
public partial class MainWindow : Window, IWpfShellHost
{
  internal IWpfShell? Shell { get; init; }

  public MainWindow()
  {
    InitializeComponent();
    NesterWindowChrome.Install(
      this,
      new NesterWindowChromeOptions { CaptionHeight = AppChrome.TitleBarHeight },
      mainGrid: MainGrid,
      highContrastBorder: HighContrastBorder);
  }

  /// <summary>
  ///   Mounts the shell's stage — the window holds nothing else; the title bar
  ///   and the system caption buttons sit above the stage's own layout.
  /// </summary>
  public void HostShell(IWpfShell shell, System.Windows.FrameworkElement stage)
  {
    var ctrl = stage as Control ?? new ContentControl { Content = stage };
    MainGrid.Children.Add(ctrl);
  }

  /// <summary>
  ///   The window owns its close — the framework doesn't hook <c>Window.Closing</c>.
  ///   Every close path (X button, Alt+F4, <c>Close()</c>) funnels into this override: the close is translated
  ///   into a <c>TryCloseIntent</c> and arbitrated through the intent chain,
  ///   so the shell can shut down gracefully — or the window stays open when
  ///   a guard vetoes.
  /// </summary>
  /// <remarks>
  ///   Chain of effects:
  ///   <list type="number">
  ///     <item><c>base.OnClosing(e)</c> raises the <c>Closing</c> event for any remaining subscribers.</item>
  ///     <item><c>ClosingToTryCloseIntent</c> holds the close (<c>e.Cancel = true</c>) and dispatches <c>TryCloseIntent</c>.</item>
  ///     <item>The intent walks the chain — layers → Director → the shell fallback.  A link that answers keeps the window open (veto, e.g. an unsaved-changes guard).</item>
  ///     <item>Nobody answers → the shell fallback disposes the shell (<c>DisposeAsync</c>: stop token, layer leases, window container), then calls <c>Close()</c>.</item>
  ///     <item>The re-entrant <c>OnClosing</c> falls through — the shell is already disposed, so no arbitration runs — and the window closes.</item>
  ///   </list>
  ///   Disposal always completes before the window's visual death: no
  ///   <c>Closed</c> hook and no backup teardown are needed.
  /// </remarks>
  protected override void OnClosing(CancelEventArgs e)
  {
    base.OnClosing(e);
    Shell?.ClosingToTryCloseIntent(e);
  }

  /// <summary>
  ///   Forwards the window's property changes into the shell's status
  ///   snapshot through the host channel — the shell maps them; the
  ///   window only leaves the channel open (the framework never hooks
  ///   window events).
  /// </summary>
  protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
  {
    base.OnPropertyChanged(e);
    Shell?.FeedHostPropertyChanged(e);
  }

  protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
  {
    base.OnPreviewMouseUp(e);
    Shell?.MouseSideButtonToRoutingIntent(this, e);
  }

  protected override void OnPreviewKeyDown(KeyEventArgs e)
  {
    base.OnPreviewKeyDown(e);
    TranslateKeyDownToIntent(this, e);
  }

  /// <summary>
  ///   Extend Key.Escape to BackIntent
  /// </summary>
  private void TranslateKeyDownToIntent(object sender, KeyEventArgs e)
  {
    // The shell declares its platform identity: a View-side type that knows
    // it lives on WPF casts the IShell reference to IWpfShell and calls
    // platform members directly (IShell members stay available — inherited).

    // Ctrl+Shift+P is the app's own gesture, so the window translates it — the
    // shell's shortcut table belongs to the framework and stays untouched.
    if (e.Key == Key.P
        && (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift))
        == (ModifierKeys.Control | ModifierKeys.Shift))
    {
      e.Handled = true;
      _ = Shell?.DispatchIntent(sender, new ShowWorkspacePaletteIntent());
    }
    else if (e.Key == Key.Escape)
    {
      e.Handled = true;
      Shell?.DispatchIntent(sender, new BackIntent(RetValue: null));
    }
    else
    {
      Shell?.KeyDownToRoutingIntent(this, e);
    }
  }
}
