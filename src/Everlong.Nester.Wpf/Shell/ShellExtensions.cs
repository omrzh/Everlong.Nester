using System.Windows;
using Everlong.Nester.Intent;
using Everlong.Nester.Primitives;

namespace Everlong.Nester.Shell;

/// <summary>
///   Shell-window-tree conveniences: state conversion, the stage-crawled
///   shell resolution and control-level intent dispatch.  Platform-face
///   members (window/input translation, capabilities) are NOT duplicated
///   here — a View-side type casts its <see cref="IShell" /> reference to
///   <see cref="IWpfShell" /> and calls them directly.
/// </summary>
public static class OperatorExtensions
{
  /// <summary>Converts a shell state to the WPF window state (FullScreen maps to Maximized — WPF has no native fullscreen).</summary>
  public static PWindowState AsWindowState(this HostState hostState)
  {
    return hostState switch
    {
      HostState.Normal => PWindowState.Normal,
      HostState.Minimized => PWindowState.Minimized,
      HostState.Maximized or HostState.FullScreen => PWindowState.Maximized,
      _ => throw new ArgumentOutOfRangeException(nameof(hostState), hostState, null)
    };
  }

  /// <summary>Converts a WPF window state to the shell state.</summary>
  public static HostState AsShellState(this PWindowState windowState)
  {
    return windowState switch
    {
      PWindowState.Normal => HostState.Normal,
      PWindowState.Minimized => HostState.Minimized,
      PWindowState.Maximized => HostState.Maximized,
      _ => throw new ArgumentOutOfRangeException(nameof(windowState), windowState, null)
    };
  }

  /// <summary>
  ///   Resolves the owning <see cref="IShell"/> from any control under a
  ///   shell's stage — the nearest <see cref="IShellStage"/> ancestor's
  ///   owner, by a logical-tree walk.  <see langword="null"/> outside a
  ///   stage subtree (host code above the stage holds its shell from the
  ///   host contract).
  /// </summary>
  public static IShell? GetShell(this DependencyObject control)
  {
    DependencyObject? node = control;
    while (node is not null)
    {
      if (node is IShellStage stage)
      {
        return stage.Shell;
      }

      node = node switch
      {
        FrameworkElement fe => fe.Parent,
        FrameworkContentElement fce => fce.Parent,
        _ => null,
      };
    }

    return null;
  }

  /// <summary>
  ///   Dispatches an intent from any control under a shell's stage — to the
  ///   shell resolved by <see cref="GetShell"/>; passes through when no
  ///   shell is reachable.
  /// </summary>
  public static ValueTask<IntentResult> DispatchIntent(this DependencyObject control, IIntent intent)
    => control.GetShell()?.DispatchIntent(control, intent)
       ?? new ValueTask<IntentResult>(IntentResult.Pass);

  /// <summary>
  ///   Fire-and-forget variant of <see cref="DispatchIntent"/>.
  /// </summary>
  public static bool PostIntent(this DependencyObject control, IIntent intent)
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
