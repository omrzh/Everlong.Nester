using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NesterApp.Backdrop;

/// <summary>
///   The WPF backdrop surface: one element draws the base wash and the
///   veils; motes are drawn by the same element (vector path) or hosted as
///   child visuals (visual path).
/// </summary>
public sealed class AuroraBackdrop : Canvas
{
  private const int AlphaLevels = 12;

  private readonly BackdropLab _lab;
  private readonly AuroraField _field = new();
  private readonly SolidColorBrush[] _moteBrushes = new SolidColorBrush[AlphaLevels];
  private readonly List<Ellipse> _moteVisuals = [];

  private LinearGradientBrush _baseBrush = null!;
  private TimeSpan _lastRenderingTime;
  private bool _running;
  private bool _themeDirty = true;
  private bool _dark = true;
  private int _themeCheck;

  /// <summary>Creates the surface over the given lab.</summary>
  public AuroraBackdrop(BackdropLab lab)
  {
    _lab = lab;
    IsHitTestVisible = false;
    ClipToBounds = true;
    Loaded += (_, _) => Start();
    Unloaded += (_, _) => Stop();
  }

  /// <inheritdoc />
  protected override void OnRender(DrawingContext dc)
  {
    EnsureTheme();

    var bounds = new Rect(RenderSize);
    if (bounds.Width < 1 || bounds.Height < 1)
      return;

    dc.DrawRectangle(_baseBrush, null, bounds);

    foreach (var veil in _field.Veils)
    {
      var brush = new RadialGradientBrush
      {
        Center = new Point(0, 0),
        GradientOrigin = new Point(0, 0),
        RadiusX = 1,
        RadiusY = 1,
        GradientStops =
        {
          new GradientStop(ToColor(veil.Color, veil.Alpha), 0),
          new GradientStop(ToColor(veil.Color, veil.Alpha * 0.42), 0.5),
          new GradientStop(ToColor(veil.Color, 0), 1),
        },
      };

      dc.PushTransform(new MatrixTransform(veil.RadiusX, 0, 0, veil.RadiusY, veil.X, veil.Y));
      dc.DrawEllipse(brush, null, new Point(0, 0), 1, 1);
      dc.Pop();
    }

    if (_lab.Mode == BackdropMode.Vector)
    {
      foreach (var mote in _field.Motes)
      {
        dc.DrawEllipse(_moteBrushes[AlphaIndex(mote.Alpha)], null, new Point(mote.X, mote.Y), mote.Radius,
                       mote.Radius);
      }
    }
  }

  private void Start()
  {
    if (_running)
      return;

    _running = true;
    _lastRenderingTime = TimeSpan.MinValue;
    CompositionTarget.Rendering += OnRendering;
  }

  private void Stop()
  {
    if (!_running)
      return;

    _running = false;
    CompositionTarget.Rendering -= OnRendering;
  }

  private void OnRendering(object? sender, EventArgs e)
  {
    if (!_running || e is not RenderingEventArgs args || args.RenderingTime == _lastRenderingTime)
      return;

    double dt = _lastRenderingTime == TimeSpan.MinValue
      ? 0
      : Math.Clamp((args.RenderingTime - _lastRenderingTime).TotalSeconds, 0, 0.1);
    _lastRenderingTime = args.RenderingTime;

    Tick(dt);
  }

  private void Tick(double dt)
  {
    _field.Resize(ActualWidth, ActualHeight);
    _field.MoteCount = _lab.MoteCount;
    _field.VeilCount = _lab.VeilCount;

    if (_lab.Parallax)
    {
      Point position = Mouse.GetPosition(this);
      double width = Math.Max(1, ActualWidth);
      double height = Math.Max(1, ActualHeight);
      _field.SetPointer(position.X / width * 2 - 1, position.Y / height * 2 - 1);
    }
    else
    {
      _field.ClearPointer();
    }

    double scaled = _lab.Paused ? 0 : dt * Math.Clamp(_lab.Speed, 0.05, 4);
    _field.Advance(scaled);
    _lab.Stats.Report(dt * 1000);

    SyncMoteVisuals();
    if (_lab.Mode == BackdropMode.Visual)
      UpdateMoteVisuals();

    InvalidateVisual();
  }

  private void SyncMoteVisuals()
  {
    int wanted = _lab.Mode == BackdropMode.Visual ? _field.MoteCount : 0;

    while (_moteVisuals.Count < wanted)
    {
      var sample = _field.Motes[_moteVisuals.Count];
      var mote = new Ellipse
      {
        IsHitTestVisible = false,
        Width = sample.Radius * 2,
        Height = sample.Radius * 2,
        Fill = new SolidColorBrush(ToColor(_field.MoteColor, 1)),
        RenderTransform = new TranslateTransform(),
      };
      _moteVisuals.Add(mote);
      Children.Add(mote);
    }

    while (_moteVisuals.Count > wanted)
    {
      Ellipse mote = _moteVisuals[^1];
      _moteVisuals.RemoveAt(_moteVisuals.Count - 1);
      Children.Remove(mote);
    }
  }

  private void UpdateMoteVisuals()
  {
    ReadOnlySpan<MoteSample> motes = _field.Motes;
    for (int i = 0; i < _moteVisuals.Count && i < motes.Length; i++)
    {
      MoteSample sample = motes[i];
      Ellipse mote = _moteVisuals[i];
      var transform = (TranslateTransform)mote.RenderTransform;
      transform.X = sample.X - sample.Radius;
      transform.Y = sample.Y - sample.Radius;
      mote.Opacity = sample.Alpha;
    }
  }

  private void EnsureTheme()
  {
    if (!_themeDirty && _themeCheck++ % 60 != 0)
      return;

    _themeCheck = 1;
    bool dark = DetectDarkTheme();
    if (!_themeDirty && dark == _dark)
      return;

    _dark = dark;
    _themeDirty = false;
    _field.DarkTheme = dark;

    _baseBrush = new LinearGradientBrush
    {
      StartPoint = new Point(0, 0),
      EndPoint = new Point(0.28, 1),
      GradientStops =
      {
        new GradientStop(ToColor(_field.BaseTop, 1), 0),
        new GradientStop(ToColor(_field.BaseBottom, 1), 1),
      },
    };

    Rgba moteColor = _field.MoteColor;
    for (int i = 0; i < AlphaLevels; i++)
    {
      double alpha = (i + 1) / (double)AlphaLevels;
      _moteBrushes[i] = new SolidColorBrush(ToColor(moteColor, alpha));
    }

    var fill = new SolidColorBrush(ToColor(moteColor, 1));
    foreach (Ellipse mote in _moteVisuals)
      mote.Fill = fill;
  }

  /// <summary>Reads the theme's primary text brush luminance; light text means the dark theme.</summary>
  private static bool DetectDarkTheme()
  {
    if (Application.Current?.TryFindResource("TextFillColorPrimaryBrush") is SolidColorBrush brush)
    {
      Color color = brush.Color;
      return (color.R + color.G + color.B) / 3 > 160;
    }

    return true;
  }

  private static int AlphaIndex(double alpha)
    => Math.Clamp((int)(alpha * AlphaLevels), 0, AlphaLevels - 1);

  private static Color ToColor(Rgba color, double alpha)
    => Color.FromArgb((byte)Math.Clamp(alpha * 255, 0, 255), color.R, color.G, color.B);
}
