using System.Windows;

namespace Everlong.Nester.Hosting;

/// <summary>
///   The default WPF prompt surface — a native message box.
/// </summary>
public sealed class WpfModalPrompt : IModalPrompt
{
  /// <summary>Gets the shared instance — stateless.</summary>
  public static WpfModalPrompt Default { get; } = new();

  /// <inheritdoc />
  public bool Confirm(string title, string message)
    => MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning)
       == MessageBoxResult.Yes;

  /// <inheritdoc />
  public void Alert(string title, string message)
    => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
}
