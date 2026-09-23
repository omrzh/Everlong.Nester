using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace Everlong.Nester.Hosting;

/// <summary>
///   The default Avalonia prompt surface — a minimal modal window.
/// </summary>
public sealed class AvaloniaMessageBox : IMessageBox
{
  /// <summary>Gets the shared instance — stateless.</summary>
  public static AvaloniaMessageBox Default { get; } = new();

  /// <inheritdoc />
  public bool Confirm(string title, string message)
  {
    Window? owner = (Application.Current?.ApplicationLifetime
                     as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
    if (owner is null)
      return true;   // no window to prompt on — nothing to ask

    var dialog = new Window
    {
      Title = title,
      Width = 380,
      SizeToContent = SizeToContent.Height,
      WindowStartupLocation = WindowStartupLocation.CenterOwner,
      CanResize = false,
      ShowInTaskbar = false,
    };
    dialog.Content = BuildContent(dialog, message);

    Task<bool> answer = dialog.ShowDialog<bool>(owner);

    // A nested dispatcher frame makes the modal answer synchronous — the shape
    // WPF's MessageBox.Show uses internally.
    var frame = new DispatcherFrame();
    answer.ContinueWith(_ => frame.Continue = false,
                        TaskScheduler.FromCurrentSynchronizationContext());
    Dispatcher.UIThread.PushFrame(frame);

    return answer.Result;
  }

  private static Control BuildContent(Window dialog, string message)
  {
    var text = new TextBlock
    {
      Text = message,
      TextWrapping = TextWrapping.Wrap,
      Margin = new Thickness(16),
    };

    var no = new Button { Content = "No", IsCancel = true, MinWidth = 80 };
    var yes = new Button { Content = "Yes", IsDefault = true, MinWidth = 80 };
    no.Click += (_, _) => dialog.Close(false);
    yes.Click += (_, _) => dialog.Close(true);

    var buttons = new StackPanel
    {
      Orientation = Orientation.Horizontal,
      HorizontalAlignment = HorizontalAlignment.Right,
      Spacing = 8,
      Margin = new Thickness(16, 0, 16, 16),
      Children = { no, yes },
    };

    return new StackPanel { Children = { text, buttons } };
  }
}
