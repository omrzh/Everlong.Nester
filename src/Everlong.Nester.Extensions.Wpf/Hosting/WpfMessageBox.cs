using System.Windows;

namespace Everlong.Nester.Hosting;

/// <summary>
///   The default WPF prompt surface — a native message box.
/// </summary>
public sealed class WpfMessageBox : IMessageBox
{
  /// <summary>Gets the shared instance — stateless.</summary>
  public static WpfMessageBox Default { get; } = new();

  /// <inheritdoc />
  public bool Confirm(string title, string message)
    => MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning)
       == MessageBoxResult.Yes;
}
