using System.Windows;
using Everlong.Nester.Primitives;

namespace Everlong.Nester.Shell;

/// <summary>
///   The WPF window-status snapshot — the host window maps its own
///   dependency-property changes into it.
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
    // Bounds deliberately not seeded: no geometry before layout (WPF Left/Top
    // are NaN until positioned); the first layout change feeds it.
  }

  /// <summary>Maps one host-window dependency-property change into this snapshot.</summary>
  /// <param name="window">The window the change came from — the source of the full geometry.</param>
  /// <param name="e">The change payload.</param>
  public void Feed(PWindow window, DependencyPropertyChangedEventArgs e)
  {
    if (e.Property == Window.WindowStateProperty)
    {
      if (e.OldValue is PWindowState oldState)
        LastHostState = oldState.AsShellState();
      if (e.NewValue is PWindowState newState)
        HostState = newState.AsShellState();
    }
    else if (e.Property == Window.TopmostProperty)
      TopMost = e.NewValue is true;
    else if (e.Property == Window.TitleProperty)
      Title = e.NewValue as string ?? string.Empty;
    else if (e.Property == Window.IsActiveProperty)
      IsActive = e.NewValue is true;
    else if (e.Property == UIElement.IsVisibleProperty)
      IsVisible = e.NewValue is true;
    else if (e.Property == Window.LeftProperty || e.Property == Window.TopProperty
                                               || e.Property == FrameworkElement.WidthProperty ||
                                               e.Property == FrameworkElement.HeightProperty)
      // A bounds component carries a single value — the window supplies the
      // full geometry.
      Bounds = new Bounds(window.Left, window.Top, window.Width, window.Height);
  }
}
