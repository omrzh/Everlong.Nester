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
/// <remarks>
///   Without a main window there is no surface to prompt on: a confirmation
///   reports <see langword="true" /> without asking, and an alert is dropped.
/// </remarks>
public sealed class AvaloniaModalPrompt : IModalPrompt
{
  /// <summary>Gets the shared instance — stateless.</summary>
  public static AvaloniaModalPrompt Default { get; } = new();

  /// <inheritdoc />
  public bool Confirm(string title, string message) => Show(title, message, alert: false);

  /// <inheritdoc />
  public void Alert(string title, string message) => _ = Show(title, message, alert: true);

  private static bool Show(string title, string message, bool alert)
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
    dialog.Content = BuildContent(dialog, message, alert);

    Task<bool> answer = dialog.ShowDialog<bool>(owner);

    // A nested dispatcher frame makes the modal answer synchronous — the shape
    // WPF's MessageBox.Show uses internally.
    var frame = new DispatcherFrame();
    answer.ContinueWith(_ => frame.Continue = false,
                        TaskScheduler.FromCurrentSynchronizationContext());
    Dispatcher.UIThread.PushFrame(frame);

    return answer.Result;
  }

  private static Control BuildContent(Window dialog, string message, bool alert)
  {
    var text = new TextBlock
    {
      Text = message,
      TextWrapping = TextWrapping.Wrap,
      Margin = new Thickness(16),
    };

    var buttons = new StackPanel
    {
      Orientation = Orientation.Horizontal,
      HorizontalAlignment = HorizontalAlignment.Right,
      Spacing = 8,
      Margin = new Thickness(16, 0, 16, 16),
    };

    if (alert)
    {
      var ok = new Button { Content = "OK", IsDefault = true, IsCancel = true, MinWidth = 80 };
      ok.Click += (_, _) => dialog.Close(true);
      buttons.Children.Add(ok);
    }
    else
    {
      var no = new Button { Content = "No", IsCancel = true, MinWidth = 80 };
      var yes = new Button { Content = "Yes", IsDefault = true, MinWidth = 80 };
      no.Click += (_, _) => dialog.Close(false);
      yes.Click += (_, _) => dialog.Close(true);
      buttons.Children.Add(no);
      buttons.Children.Add(yes);
    }

    return new StackPanel { Children = { text, buttons } };
  }
}
