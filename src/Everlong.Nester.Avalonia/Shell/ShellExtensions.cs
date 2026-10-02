// NOTE: Single-source file — the Wpf project compiles this exact file via
// <Compile Include> in Everlong.Nester.Wpf.csproj.  Edit it here only; never
// create a WPF-side copy (the two builds would drift).
//
// The `#if AVALONIA` branches are the two platform facts: the FullScreen member
// a window-state enum may lack, and the loaded signal.  The receiver is spelled
// `PElement` — a Control here, a DependencyObject on WPF, where the walk must
// also serve content elements.

#if AVALONIA
using Avalonia.VisualTree;
#endif
using Everlong.Nester.Helpers;
using Everlong.Nester.Presentation;
using Everlong.Nester.Intent;
using Everlong.Nester.Primitives;

namespace Everlong.Nester.Shell;

/// <summary>
///   Shell-window-tree conveniences: state conversion, the stage-crawled
///   shell resolution and control-level intent dispatch.  Platform-face
///   members (window/input translation, capabilities) are NOT duplicated
///   here — a View-side type casts its <see cref="IShell" /> reference to
///   the platform shell interface (<c>IAvaloniaShell</c> /
///   <c>IWpfShell</c>) and calls them directly.
/// </summary>
public static class ShellOperatorExtensions
{
  /// <summary>Converts a shell state to the platform window state.</summary>
  public static PWindowState AsWindowState(this HostState hostState)
  {
    return hostState switch
    {
      HostState.Normal => PWindowState.Normal,
      HostState.Minimized => PWindowState.Minimized,
      HostState.Maximized => PWindowState.Maximized,
#if AVALONIA
      HostState.FullScreen => PWindowState.FullScreen,
#else
      // WPF has no native fullscreen; the maximized window is the closest state.
      HostState.FullScreen => PWindowState.Maximized,
#endif
      _ => throw new ArgumentOutOfRangeException(nameof(hostState), hostState, null)
    };
  }

  /// <summary>Converts a platform window state to the shell state.</summary>
  public static HostState AsShellState(this PWindowState windowState)
  {
    return windowState switch
    {
      PWindowState.Normal => HostState.Normal,
      PWindowState.Minimized => HostState.Minimized,
      PWindowState.Maximized => HostState.Maximized,
#if AVALONIA
      PWindowState.FullScreen => HostState.FullScreen,
#endif
      _ => throw new ArgumentOutOfRangeException(nameof(windowState), windowState, null)
    };
  }

  /// <summary>
  ///   Resolves the owning <see cref="IShell"/> from any element under a
  ///   shell's stage — the nearest <see cref="IShellStage"/> ancestor's
  ///   owner.  <see langword="null"/> outside a stage subtree (host code
  ///   above the stage holds its shell from the host contract).
  /// </summary>
  public static IShell? GetShell(this PElement control)
    => control.FindAncestor<IShellStage>()?.Shell;

  /// <summary>
  ///   Resolves the stage owning any element below it — the nearest
  ///   <see cref="StagePanel" /> ancestor.  <see langword="null"/> outside
  ///   a stage subtree.
  /// </summary>
  internal static StagePanel? GetStage(this PElement control)
    => control.FindAncestor<StagePanel>();

  /// <summary>Whether <paramref name="control" /> has joined the visual tree.</summary>
  internal static bool IsInVisualTree(this PControl control)
  {
#if AVALONIA
    return control.IsAttachedToVisualTree();
#else
    return control.IsLoaded;
#endif
  }

  /// <summary>
  ///   Resolves the flying layer's plane figure from any element under a
  ///   shell's stage.  <see langword="null"/> outside a stage subtree, and
  ///   while the stage's shell provides no flying layer.
  /// </summary>
  public static FlyingCanvas? GetFlyingCanvas(this PElement control)
    => control.GetStage()?.FlyingCanvas;

  /// <summary>
  ///   Dispatches an intent from any element under a shell's stage — to the
  ///   shell resolved by <see cref="GetShell"/>; passes through when no
  ///   shell is reachable.
  /// </summary>
  public static ValueTask<IntentResult> DispatchIntent(this PElement control, IIntent intent)
    => control.GetShell()?.DispatchIntent(control, intent)
       ?? new ValueTask<IntentResult>(IntentResult.Pass);

  /// <summary>
  ///   Fire-and-forget variant of <see cref="DispatchIntent"/>.
  /// </summary>
  public static bool PostIntent(this PElement control, IIntent intent)
  {
    IShell? ctx = control.GetShell();
    if (ctx is null)
    {
      return false;
    }

    _ = ctx.DispatchIntent(control, intent);
    return true;
  }
}
