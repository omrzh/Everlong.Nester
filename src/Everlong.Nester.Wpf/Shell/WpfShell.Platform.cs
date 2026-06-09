using Everlong.Nester.Helpers;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace Everlong.Nester.Shell;

partial class WpfShell : IWpfShell
{
  #region IWpfShell

  /// <inheritdoc />
  public double DpiX
  {
    get
    {
      var source = PresentationSource.FromVisual(Window);
      var matrix = source?.CompositionTarget?.TransformToDevice;
      return matrix?.M11 * 96d ?? 96d;
    }
  }

  /// <inheritdoc />
  public double DpiY
  {
    get
    {
      var source = PresentationSource.FromVisual(Window);
      var matrix = source?.CompositionTarget?.TransformToDevice;
      return matrix?.M22 * 96d ?? 96d;
    }
  }

  /// <inheritdoc />
  public bool ShowInTaskbar
  {
    get
    {
      return Window.ShowInTaskbar;
    }
  }

  /// <inheritdoc />
  public ResizeMode ResizeMode
  {
    get
    {
      return Window.ResizeMode;
    }
  }

  /// <inheritdoc />
  public WindowStyle WindowStyle
  {
    get
    {
      return Window.WindowStyle;
    }
  }

  /// <inheritdoc />
  public bool AllowsTransparency
  {
    get
    {
      return Window.AllowsTransparency;
    }
  }

  /// <inheritdoc />
  public Dispatcher Dispatcher
  {
    get
    {
      return Window.Dispatcher;
    }
  }

  /// <inheritdoc />
  public PWindow Window
  {
    get
    {
      return HostWindow
             ?? throw new InvalidOperationException("The shell window is not available yet.");
    }
  }

  /// <inheritdoc />
  public void FlashTaskbar()
  {
    var handle = new WindowInteropHelper(Window).Handle;
    if (handle == IntPtr.Zero)
    {
      return;
    }

    Win32Helper.FlashWindow(handle);

  }

  #endregion
}
