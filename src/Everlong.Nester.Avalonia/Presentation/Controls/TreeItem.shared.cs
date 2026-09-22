// NOTE: Single-source file — the WPF project compiles this exact file via
// <Compile Include> in Everlong.Nester.Wpf.csproj.  Edit it here only;
// never create a WPF-side copy (the two builds would drift).
namespace Everlong.Nester.Presentation;

public partial class TreeItem
{
  /// <summary>
  ///   Called when the header button is clicked. Toggles <see cref="IsExpanded" /> when items are present.
  /// </summary>
  protected virtual void OnHeaderClicked()
  {
    if (HasItems)
      IsExpanded = !IsExpanded;
    // No items (leaf): no-op — subclasses handle their own leaf behavior.
  }

  /// <summary>
  ///   Called when the expand button is clicked. Toggles <see cref="IsExpanded" /> when items are present.
  /// </summary>
  protected virtual void OnExpandClicked()
  {
    if (HasItems)
      IsExpanded = !IsExpanded;
  }

  /// <summary>
  ///   Called after <see cref="OnHeaderClicked" /> and <see cref="ItemClickedEvent" /> have fired.
  ///   Executes <see cref="Command" /> with <see cref="CommandParameter" /> when the command can execute.
  /// </summary>
  protected virtual void OnItemClicked()
  {
    if (Command is { } cmd)
    {
      object? param = CommandParameter;
      if (cmd.CanExecute(param))
        cmd.Execute(param);
    }
  }
}

