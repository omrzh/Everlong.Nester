using Everlong.Nester.Routing;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Everlong.Nester.Shell;

/// <summary>
///   The WPF platform face of a shell — the shell itself (an <see cref="IShell" />)
///   declares its WPF identity: View-side types (a window) that know they live
///   on WPF can test/cast their <see cref="IShell" /> reference to this
///   interface and call platform members directly — no extension indirection
///   needed.
/// </summary>
public interface IWpfShell : IShell
{
  /// <summary>
  ///   Translates a window-closing notification into a <see cref="TryCloseIntent" />
  ///   for unified arbitration through the intent chain.  The host window's
  ///   <c>OnClosing</c> override calls this.
  /// </summary>
  void ClosingToTryCloseIntent(CancelEventArgs e);

  /// <summary>Translates mouse side buttons (XButton1 / XButton2) into <see cref="BackIntent" /> / <see cref="ForwardIntent" />.</summary>
  void MouseSideButtonToRoutingIntent(object? sender, MouseButtonEventArgs e);

  /// <summary>Translates keyboard shortcuts (F5, Alt+Left/Right) into navigation intents.</summary>
  void KeyDownToRoutingIntent(object? sender, KeyEventArgs e);

  /// <summary>Gets the window's DPI scale on the X axis.</summary>
  double DpiX { get; }

  /// <summary>
  ///   Channel: forwards the host window's dependency-property change into
  ///   the shell's status snapshot.
  /// </summary>
  void FeedHostPropertyChanged(DependencyPropertyChangedEventArgs e);

  /// <summary>Gets the window's DPI scale on the Y axis.</summary>
  double DpiY { get; }

  /// <summary>Gets whether the window shows in the taskbar.</summary>
  bool ShowInTaskbar { get; }

  /// <summary>Gets the window's resize mode.</summary>
  ResizeMode ResizeMode { get; }

  /// <summary>Gets the window's style.</summary>
  WindowStyle WindowStyle { get; }

  /// <summary>Gets whether the window supports transparency.</summary>
  bool AllowsTransparency { get; }

  /// <summary>Gets the WPF dispatcher the window runs on.</summary>
  Dispatcher Dispatcher { get; }

  /// <summary>Flashes the taskbar button to attract attention.</summary>
  void FlashTaskbar();

  /// <summary>Gets the shell's window — never null (WPF has no single-view host).</summary>
  PWindow Window { get; }
}
