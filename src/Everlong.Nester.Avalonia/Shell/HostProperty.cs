using Avalonia;
using Avalonia.Controls;
using Everlong.Nester.Primitives;

namespace Everlong.Nester.Shell;

/// <summary>
///   The Avalonia window-status snapshot — the host window maps its own
///   property changes into it.
/// </summary>
public sealed class HostProperty : HostPropertyBase
{
  /// <summary>Seeds this snapshot with the host window's current state.</summary>
  /// <param name="window">The window to read.</param>
  /// <remarks>
  ///   Called once at mount, before the window has raised anything.
  ///   Geometry is not seeded — a window has no bounds before layout; the
  ///   first change carries it.  The pre-change state stays
  ///   <see cref="HostState.Normal" />, the state a freshly mounted window
  ///   restores to.
  /// </remarks>
  public void Prime(PWindow window)
  {
    IsActive = window.IsActive;
    TopMost = window.Topmost;
    IsVisible = window.IsVisible;
    Title = window.Title ?? string.Empty;
    HostState = window.WindowState.AsShellState();
    // Bounds deliberately not seeded: no geometry before layout; the first
    // layout change feeds it.
  }

  /// <summary>Maps one host-window property change into this snapshot.</summary>
  /// <param name="e">The change payload.</param>
  public void Feed(AvaloniaPropertyChangedEventArgs e)
  {
    if (e.Property == WindowBase.IsActiveProperty)
      IsActive = e.NewValue is true;
    else if (e.Property == WindowBase.TopmostProperty)
      TopMost = e.NewValue is true;
    else if (e.Property == Window.TitleProperty)
      Title = e.NewValue as string ?? string.Empty;
    else if (e.Property == PVisual.BoundsProperty)
      Bounds = e.NewValue is PRect rect
                 ? new Bounds(rect.X, rect.Y, rect.Width, rect.Height)
                 : Bounds.Empty;
    else if (e.Property == PVisual.IsVisibleProperty)
      IsVisible = e.NewValue is true;
    else if (e.Property == Window.WindowStateProperty)
    {
      if (e.OldValue is PWindowState oldState)
        LastHostState = oldState.AsShellState();
      if (e.NewValue is PWindowState newState)
        HostState = newState.AsShellState();
    }
  }
}
