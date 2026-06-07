using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using Microsoft.Win32;

namespace Everlong.Nester.Chrome;

/// <summary>
///   Attaches a <see cref="WindowChrome"/> surface to a <see cref="Window"/>:
///   the caption area becomes client-drawn while the system keeps providing
///   the Windows 11 snap-layout flyout for a marked maximize button.
/// </summary>
/// <remarks>
///   Call <see cref="Install"/> from the window constructor after
///   <c>InitializeComponent()</c>.  Buttons placed in the caption area need
///   <see cref="WindowChrome.IsHitTestVisibleInChromeProperty"/> to receive
///   input; <see cref="SetSnapLayoutButton"/> marks the maximize button the
///   system reports as <c>HTMAXBUTTON</c> while the pointer hovers it, which
///   opens the snap flyout and leaves the click to the button.
/// </remarks>
public static class NesterWindowChrome
{
  /// <summary>
  ///   Set on the snap-layout button while the system reports the pointer
  ///   over it; the button is in the non-client area then, so a style trigger
  ///   supplies the hover visual.
  /// </summary>
  public static readonly DependencyProperty IsChromeHotProperty =
    DependencyProperty.RegisterAttached("IsChromeHot", typeof(bool), typeof(NesterWindowChrome),
                                        new PropertyMetadata(false));

  /// <summary>
  ///   Set on the snap-layout button while its non-client press is active;
  ///   a style trigger supplies the pressed visual.
  /// </summary>
  public static readonly DependencyProperty IsChromePressedProperty =
    DependencyProperty.RegisterAttached("IsChromePressed", typeof(bool), typeof(NesterWindowChrome),
                                        new PropertyMetadata(false));

  private static readonly DependencyProperty SnapLayoutStateProperty =
    DependencyProperty.RegisterAttached("SnapLayoutState", typeof(SnapLayoutState), typeof(NesterWindowChrome));

  /// <summary>Reads <see cref="IsChromeHotProperty"/>.</summary>
  public static bool GetIsChromeHot(DependencyObject element) => (bool)element.GetValue(IsChromeHotProperty);

  /// <summary>Writes <see cref="IsChromeHotProperty"/>.</summary>
  public static void SetIsChromeHot(DependencyObject element, bool value)
    => element.SetValue(IsChromeHotProperty, value);

  /// <summary>Reads <see cref="IsChromePressedProperty"/>.</summary>
  public static bool GetIsChromePressed(DependencyObject element)
    => (bool)element.GetValue(IsChromePressedProperty);

  /// <summary>Writes <see cref="IsChromePressedProperty"/>.</summary>
  public static void SetIsChromePressed(DependencyObject element, bool value)
    => element.SetValue(IsChromePressedProperty, value);

  /// <summary>Attaches the chrome surface to <paramref name="window" />.</summary>
  /// <param name="window">The window to decorate.</param>
  /// <param name="options">Chrome geometry; <see cref="NesterWindowChromeOptions.Default" /> when <c>null</c>.</param>
  /// <param name="mainGrid">Root grid whose margin compensates the maximized overshoot.</param>
  /// <param name="highContrastBorder">Border that receives the high-contrast caption frame.</param>
  public static void Install(Window window,
                             NesterWindowChromeOptions? options = null,
                             Grid? mainGrid = null,
                             Border? highContrastBorder = null)
  {
    options ??= NesterWindowChromeOptions.Default;

    ApplyWindowChrome(window, options);
    ApplyWindowBackground(window);

    var ctx = new ChromeContext(window, mainGrid, highContrastBorder);

    window.StateChanged += ctx.OnStateOrActivationChanged;
    window.Activated += ctx.OnStateOrActivationChanged;
    window.Deactivated += ctx.OnStateOrActivationChanged;
    SystemEvents.UserPreferenceChanged += ctx.OnUserPreferenceChanged;
    window.Closed += ctx.OnClosed;

    ctx.UpdateVisuals();
  }

  /// <summary>
  ///   Marks the maximize/restore button whose hover opens the system
  ///   snap-layout flyout.  May be called before or after the window is shown.
  /// </summary>
  /// <param name="window">The decorated window.</param>
  /// <param name="button">The maximize/restore button; <c>null</c> detaches the behaviour.</param>
  public static void SetSnapLayoutButton(Window window, Button? button)
  {
    var state = (SnapLayoutState?)window.GetValue(SnapLayoutStateProperty);
    if (state is null)
    {
      state = new SnapLayoutState(window);
      window.SetValue(SnapLayoutStateProperty, state);
    }

    state.SetButton(button);
  }

  private static void ApplyWindowChrome(Window window, NesterWindowChromeOptions options)
  {
    WindowChrome.SetWindowChrome(
      window,
      new WindowChrome
      {
        CaptionHeight = options.CaptionHeight,
        CornerRadius = options.CornerRadius,
        GlassFrameThickness = options.GlassFrameThickness,
        ResizeBorderThickness = window.ResizeMode == ResizeMode.NoResize
          ? default
          : options.ResizeBorderThickness,
        UseAeroCaptionButtons = options.UseAeroCaptionButtons,
        NonClientFrameEdges = GetPreferredNonClientFrameEdges()
      });
  }

  private static void ApplyWindowBackground(Window window)
  {
    if (!WindowUtility.IsBackdropDisabled() && !WindowUtility.IsBackdropSupported())
      window.SetResourceReference(Control.BackgroundProperty, "WindowBackground");
  }

  internal static NonClientFrameEdges GetPreferredNonClientFrameEdges() =>
    SystemParameters.HighContrast || !WindowUtility.IsWindows11OrGreater()
      ? NonClientFrameEdges.None
      : NonClientFrameEdges.Right | NonClientFrameEdges.Bottom | NonClientFrameEdges.Left;

  // ── Snap-layout plumbing ────────────────────────────────────────────────

  private sealed class SnapLayoutState
  {
    private const int WmNcHitTest = 0x0084;
    private const int WmNcLButtonDown = 0x00A1;
    private const int WmNcLButtonUp = 0x00A2;
    private const int HtMaxButton = 9;

    private readonly Window _window;
    private Button? _button;
    private bool _hooked;

    public SnapLayoutState(Window window)
    {
      _window = window;
      window.SourceInitialized += OnSourceInitialized;
      window.Closed += OnClosed;
    }

    public void SetButton(Button? button)
    {
      if (ReferenceEquals(_button, button))
        return;

      if (_button is not null)
      {
        SetIsChromeHot(_button, false);
        SetIsChromePressed(_button, false);
      }

      _button = button;
      EnsureHook();
    }

    private void OnSourceInitialized(object? sender, EventArgs e) => EnsureHook();

    private void OnClosed(object? sender, EventArgs e)
    {
      _window.SourceInitialized -= OnSourceInitialized;
      _window.Closed -= OnClosed;
    }

    /// <summary>
    ///   Adds the hook after the chrome worker's own hook: HwndSource invokes
    ///   hooks newest-first, and the worker answers WM_NCHITTEST as well.
    /// </summary>
    private void EnsureHook()
    {
      if (_hooked || _button is null)
        return;
      if (PresentationSource.FromVisual(_window) is not HwndSource source)
        return;

      source.AddHook(WndProc);
      _hooked = true;
    }

    /// <summary>
    ///   Reports the button as <c>HTMAXBUTTON</c> while it is hovered, which
    ///   is what opens the system snap-layout flyout.  Because the button is
    ///   in the non-client area then, its press is handled here: the click is
    ///   raised on release and the visual states are mirrored onto the
    ///   attached properties.
    /// </summary>
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
      if (_button is null || !_button.IsVisible || PresentationSource.FromVisual(_button) is null)
        return IntPtr.Zero;

      switch (msg)
      {
        case WmNcHitTest:
          {
            bool hot = GetScreenBounds(_button).Contains(GetScreenPoint(lParam));
            SetIsChromeHot(_button, hot);
            if (!hot)
            {
              SetIsChromePressed(_button, false);
              return IntPtr.Zero;
            }

            handled = true;
            return HtMaxButton;
          }

        case WmNcLButtonDown:
          {
            if (!GetScreenBounds(_button).Contains(GetScreenPoint(lParam)))
              return IntPtr.Zero;

            SetIsChromePressed(_button, true);
            handled = true;
            return IntPtr.Zero;
          }

        case WmNcLButtonUp:
          {
            bool pressed = GetIsChromePressed(_button);
            SetIsChromePressed(_button, false);
            if (!pressed)
              return IntPtr.Zero;

            if (GetScreenBounds(_button).Contains(GetScreenPoint(lParam)))
              _button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, _button));

            handled = true;
            return IntPtr.Zero;
          }

        default:
          return IntPtr.Zero;
      }
    }

    private static Point GetScreenPoint(IntPtr lParam)
    {
      long packed = lParam.ToInt64();
      return new Point((short)(packed & 0xFFFF), (short)((packed >> 16) & 0xFFFF));
    }

    private static PRect GetScreenBounds(Button button)
    {
      DpiScale dpi = VisualTreeHelper.GetDpi(button);
      Point topLeft = button.PointToScreen(default);
      return new PRect(topLeft, new Size(button.ActualWidth * dpi.DpiScaleX,
                                        button.ActualHeight * dpi.DpiScaleY));
    }
  }

  // ── Inner context ────────────────────────────────────────────────────────

  private sealed class ChromeContext(Window window, Grid? mainGrid, Border? highContrastBorder)
  {
    public void OnStateOrActivationChanged(object? sender, EventArgs e) => UpdateVisuals();

    public void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
      => window.Dispatcher.Invoke(UpdateVisuals);

    public void OnClosed(object? sender, EventArgs e)
    {
      window.StateChanged -= OnStateOrActivationChanged;
      window.Activated -= OnStateOrActivationChanged;
      window.Deactivated -= OnStateOrActivationChanged;
      SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
      window.Closed -= OnClosed;
    }

    public void UpdateVisuals()
    {
      UpdateMainGridMargin();
      UpdateHighContrastBorder();
      UpdateNonClientFrameEdges();
    }

    private void UpdateMainGridMargin()
    {
      mainGrid?.Margin = window.WindowState == PWindowState.Maximized
                           ? (SystemParameters.HighContrast ? new PThickness(0, 8, 0, 0) : new PThickness(8))
                           : default;
    }

    private void UpdateHighContrastBorder()
    {
      if (highContrastBorder is null)
        return;
      if (SystemParameters.HighContrast)
      {
        highContrastBorder.SetResourceReference(
          Border.BorderBrushProperty,
          window.IsActive ? SystemColors.ActiveCaptionBrushKey : SystemColors.InactiveCaptionBrushKey);
        highContrastBorder.BorderThickness = new PThickness(8, 1, 8, 8);
      }
      else
      {
        highContrastBorder.BorderBrush = Brushes.Transparent;
        highContrastBorder.BorderThickness = new PThickness(0);
      }
    }

    private void UpdateNonClientFrameEdges()
    {
      if (!WindowUtility.IsWindows11OrGreater())
        return;
      var wc = WindowChrome.GetWindowChrome(window);
      wc?.NonClientFrameEdges = GetPreferredNonClientFrameEdges();
    }
  }
}
