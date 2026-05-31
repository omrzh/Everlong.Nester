using Avalonia;
using Everlong.Nester.Intent;
using Everlong.Nester.Primitives;

namespace Everlong.Nester.Shell;

/// <summary>
///   Shell-window-tree conveniences: state conversion, the stage-crawled
///   shell resolution and control-level intent dispatch.  Platform-face
///   members (window/input translation, capabilities) are NOT duplicated
///   here — a View-side type casts its <see cref="IShell" /> reference to
///   <see cref="IAvaloniaShell" /> and calls them directly.
/// </summary>
public static class ShellOperatorExtensions
{
  /// <summary>Converts a shell state to the Avalonia window state.</summary>
  public static WindowState AsWindowState(this HostState hostState)
  {
    return hostState switch
    {
      HostState.Normal => WindowState.Normal,
      HostState.Minimized => WindowState.Minimized,
      HostState.Maximized => WindowState.Maximized,
      HostState.FullScreen => WindowState.FullScreen,
      _ => throw new ArgumentOutOfRangeException(nameof(hostState), hostState, null)
    };
  }

  /// <summary>Converts an Avalonia window state to the shell state.</summary>
  public static HostState AsShellState(this WindowState windowState)
  {
    return windowState switch
    {
      WindowState.Normal => HostState.Normal,
      WindowState.Minimized => HostState.Minimized,
      WindowState.Maximized => HostState.Maximized,
      WindowState.FullScreen => HostState.FullScreen,
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
  public static IShell? GetShell(this PlatformControl control)
  {
    StyledElement? node = control;
    while (node is not null)
    {
      if (node is IShellStage stage)
      {
        return stage.Shell;
      }

      node = node.Parent;
    }

    return null;
  }

  /// <summary>
  ///   Dispatches an intent from any control under a shell's stage — to the
  ///   shell resolved by <see cref="GetShell"/>; passes through when no
  ///   shell is reachable.
  /// </summary>
  public static ValueTask<IntentResult> DispatchIntent(this PlatformControl control, IIntent intent)
    => control.GetShell()?.DispatchIntent(control, intent)
       ?? new ValueTask<IntentResult>(IntentResult.Pass);

  /// <summary>
  ///   Fire-and-forget variant of <see cref="DispatchIntent"/>.
  /// </summary>
  public static bool PostIntent(this PlatformControl control, IIntent intent)
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
