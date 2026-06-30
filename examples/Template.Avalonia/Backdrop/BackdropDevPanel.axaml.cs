using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;

namespace NesterApp.Backdrop;

/// <summary>
///   The backdrop control panel: it drives <see cref="BackdropLab" /> and
///   applies the glass alpha to the shell's translucent surface brushes.
/// </summary>
public partial class BackdropDevPanel : UserControl
{
  private BackdropLab? _lab;
  private DispatcherTimer? _timer;

  /// <summary>Creates the panel.</summary>
  public BackdropDevPanel()
  {
    InitializeComponent();
    ActualThemeVariantChanged += (_, _) => ApplyGlass();
  }

  /// <inheritdoc />
  protected override void OnDataContextChanged(EventArgs e)
  {
    base.OnDataContextChanged(e);

    Detach();
    _lab = DataContext as BackdropLab;
    if (_lab is null)
      return;

    Graph.Stats = _lab.Stats;
    _lab.PropertyChanged += OnLabChanged;
    ApplyGlass();

    _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
    _timer.Tick += OnTimerTick;
    _timer.Start();
    UpdateReadout();
  }

  /// <inheritdoc />
  protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
  {
    Detach();
    base.OnDetachedFromVisualTree(e);
  }

  private void Detach()
  {
    _timer?.Stop();
    _timer = null;
    _lab?.PropertyChanged -= OnLabChanged;
    _lab = null;
    Graph.Stats = null;
  }

  private void OnLabChanged(object? sender, PropertyChangedEventArgs e)
  {
    if (e.PropertyName == nameof(BackdropLab.PanelOpacity))
      ApplyGlass();
  }

  private void OnTimerTick(object? sender, EventArgs e) => UpdateReadout();

  private void UpdateReadout()
  {
    if (!IsEffectivelyVisible)
      return; // the lease hid the layer — skip the readout

    FpsText.Text = _lab is null ? "—" : $"{_lab.Stats.Fps:0} fps · {_lab.Stats.FrameMs:0.0} ms";
  }

  private void OnResetClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => _lab?.Reset();

  /// <summary>
  ///   Applies the glass alpha to the shell's surface brushes: a white glass
  ///   in the light theme, a near-black glass in the dark theme.
  /// </summary>
  private void ApplyGlass()
  {
    if (_lab is null)
      return;

    double alpha = Math.Clamp(_lab.PanelOpacity, 0, 1);
    bool dark = ActualThemeVariant == ThemeVariant.Dark;

    SetBrushColor("App.Sidebar.Background", dark
      ? Color.FromArgb((byte)(alpha * 255), 12, 16, 26)
      : Color.FromArgb((byte)(alpha * 255), 255, 255, 255));
    SetBrushColor("App.Body.Background", dark
      ? Color.FromArgb((byte)(alpha * 235), 8, 11, 19)
      : Color.FromArgb((byte)(alpha * 235), 255, 255, 255));
  }

  private void SetBrushColor(string key, Color color)
  {
    if (this.TryFindResource(key, out object? value) && value is SolidColorBrush brush)
      brush.Color = color;
  }
}
