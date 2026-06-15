// NOTE: Single-source file — the WPF project compiles this exact file via
// <Compile Include> in Everlong.Nester.Wpf.csproj.  Edit it here only;
// never create a WPF-side copy (the two builds would drift).
using System.Windows.Input;

namespace Everlong.Nester.Controls;

/// <summary>
///   A general-purpose hierarchical tree container without selection semantics.
/// </summary>
/// <remarks>
///   <para>
///     Use <see cref="TreeItem" /> as the item container, or bind hierarchical data
///     via a platform-specific hierarchical data template.
///   </para>
///   <para>
///     <see cref="ItemCommand" /> provides a centralized way to respond to item header clicks without
///     requiring event-handler code-behind. It is executed with the clicked item's <c>DataContext</c>
///     as the command parameter:
///     <code>
///       &lt;v:TreeControl ItemCommand="{Binding SelectNodeCommand}" /&gt;
///     </code>
///   </para>
///   <para>
///     For per-item commands (inside a <c>DataTemplate</c>), bind <see cref="TreeItem.Command" />
///     and <see cref="TreeItem.CommandParameter" /> directly on the <see cref="TreeItem" />.
///   </para>
///   <para>
///     Subscribe to the bubbled <see cref="TreeItem.ItemClickedEvent" /> at any ancestor level
///     for code-behind handlers.
///   </para>
/// </remarks>
public partial class TreeControl
{
  /// <summary>
  ///   Gets or sets the command to execute when any child <see cref="TreeItem" /> header is clicked.
  ///   The command receives the clicked item's <c>DataContext</c> as its parameter.
  /// </summary>
  public ICommand? ItemCommand
  {
    // The platform halves declare this property differently: Avalonia's
    // GetValue<T> is generic and already returns ICommand?, WPF's takes a
    // DependencyProperty and returns object.  The cast is therefore required
    // in one compilation and redundant in the other, so it stays.
#pragma warning disable IDE0004
    get => (ICommand?)GetValue(ItemCommandProperty);
#pragma warning restore IDE0004
    set => SetValue(ItemCommandProperty, value);
  }
}

