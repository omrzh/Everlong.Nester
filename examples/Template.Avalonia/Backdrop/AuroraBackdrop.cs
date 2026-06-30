using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;

namespace NesterApp.Backdrop;

/// <summary>
///   The Avalonia backdrop surface: one element draws the base wash and the
///   veils; motes are drawn by the same element (vector path) or hosted as
///   child visuals (visual path).
/// </summary>
public sealed class AuroraBackdrop : Panel
{
  private const int AlphaLevels = 12;

  private readonly BackdropLab _lab;
  private readonly AuroraField _field = new();
  private readonly SolidColorBrush[] _moteBrushes = new SolidColorBrush[AlphaLevels];
  private readonly List<Ellipse> _moteVisuals = [];
  private readonly Stopwatch _clock = new();
  private readonly Painter _painter;
  private readonly Canvas _moteLayer = new() { IsHitTestVisible = false };

  private LinearGradientBrush _baseBrush = null!;
  private TopLevel? _topLevel;
  private double _lastSeconds;
  private bool _running;
  private bool _themeDirty = true;
  private bool _dark = true;

  /// <summary>Creates the surface over the given lab.</summary>
  public AuroraBackdrop(BackdropLab lab)
  {
    _lab = lab;
    IsHitTestVisible = false;
    ClipToBounds = true;
    _painter = new Painter(this);
    Children.Add(_painter);
    Children.Add(_moteLayer);

    ActualThemeVariantChanged += (_, _) =>
    {
      _themeDirty = true;
      _painter.InvalidateVisual();
    };
  }

  /// <inheritdoc />
  protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
  {
    base.OnAttachedToVisualTree(e);

    _topLevel = TopLevel.GetTopLevel(this);
    _topLevel?.AddHandler(PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel, handledEventsToo: true);

    _clock.Restart();
    _lastSeconds = 0;
    _running = true;
    _topLevel?.RequestAnimationFrame(OnFrame);
  }

  /// <inheritdoc />
  protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
  {
    _running = false;
    _topLevel?.RemoveHandler(PointerMovedEvent, OnPointerMoved);
    _topLevel = null;

    base.OnDetachedFromVisualTree(e);
  }

  private void Draw(DrawingContext context)
  {
    EnsureTheme();

    var bounds = new Rect(Bounds.Size);
    if (bounds.Width < 1 || bounds.Height < 1)
      return;

    context.FillRectangle(_baseBrush, bounds);

    foreach (var veil in _field.Veils)
    {
      var brush = new RadialGradientBrush
      {
        Center = new RelativePoint(0, 0, RelativeUnit.Absolute),
        GradientOrigin = new RelativePoint(0, 0, RelativeUnit.Absolute),
        RadiusX = new RelativeScalar(1, RelativeUnit.Absolute),
        RadiusY = new RelativeScalar(1, RelativeUnit.Absolute),
        GradientStops =
        {
          new GradientStop(ToColor(veil.Color, veil.Alpha), 0),
          new GradientStop(ToColor(veil.Color, veil.Alpha * 0.42), 0.5),
          new GradientStop(ToColor(veil.Color, 0), 1),
        },
      };

      using (context.PushTransform(
               Matrix.CreateScale(veil.RadiusX, veil.RadiusY) * Matrix.CreateTranslation(veil.X, veil.Y)))
      {
        context.DrawEllipse(brush, null, default, 1, 1);
      }
    }

    if (_lab.Mode == BackdropMode.Vector)
    {
      foreach (var mote in _field.Motes)
      {
        context.DrawEllipse(_moteBrushes[AlphaIndex(mote.Alpha)], null, new Point(mote.X, mote.Y), mote.Radius,
                            mote.Radius);
      }
    }
  }

  private void OnFrame(TimeSpan timestamp)
  {
    if (!_running)
      return;

    double now = _clock.Elapsed.TotalSeconds;
    double dt = Math.Clamp(now - _lastSeconds, 0, 0.1);
    _lastSeconds = now;

    Tick(dt);
    _topLevel?.RequestAnimationFrame(OnFrame);
  }

  private void Tick(double dt)
  {
    _field.Resize(Bounds.Width, Bounds.Height);
    _field.MoteCount = _lab.MoteCount;
    _field.VeilCount = _lab.VeilCount;
    if (!_lab.Parallax)
      _field.ClearPointer();

    double scaled = _lab.Paused ? 0 : dt * Math.Clamp(_lab.Speed, 0.05, 4);
    _field.Advance(scaled);
    _lab.Stats.Report(dt * 1000);

    SyncMoteVisuals();
    if (_lab.Mode == BackdropMode.Visual)
      UpdateMoteVisuals();

    _painter.InvalidateVisual();
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
      _moteLayer.Children.Add(mote);
    }

    while (_moteVisuals.Count > wanted)
    {
      Ellipse mote = _moteVisuals[^1];
      _moteVisuals.RemoveAt(_moteVisuals.Count - 1);
      _moteLayer.Children.Remove(mote);
    }
  }

  private void UpdateMoteVisuals()
  {
    ReadOnlySpan<MoteSample> motes = _field.Motes;
    for (int i = 0; i < _moteVisuals.Count && i < motes.Length; i++)
    {
      MoteSample sample = motes[i];
      Ellipse mote = _moteVisuals[i];
      var transform = (TranslateTransform)mote.RenderTransform!;
      transform.X = sample.X - sample.Radius;
      transform.Y = sample.Y - sample.Radius;
      mote.Opacity = sample.Alpha;
    }
  }

  private void OnPointerMoved(object? sender, PointerEventArgs e)
  {
    if (!_lab.Parallax)
    {
      _field.ClearPointer();
      return;
    }

    Point position = e.GetPosition(this);
    double width = Math.Max(1, Bounds.Width);
    double height = Math.Max(1, Bounds.Height);
    _field.SetPointer(position.X / width * 2 - 1, position.Y / height * 2 - 1);
  }

  private void EnsureTheme()
  {
    bool dark = ActualThemeVariant == ThemeVariant.Dark;
    if (!_themeDirty && dark == _dark)
      return;

    _dark = dark;
    _themeDirty = false;
    _field.DarkTheme = dark;

    _baseBrush = new LinearGradientBrush
    {
      StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
      EndPoint = new RelativePoint(0.28, 1, RelativeUnit.Relative),
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

  private static int AlphaIndex(double alpha)
    => Math.Clamp((int)(alpha * AlphaLevels), 0, AlphaLevels - 1);

  private static Color ToColor(Rgba color, double alpha)
    => Color.FromArgb((byte)Math.Clamp(alpha * 255, 0, 255), color.R, color.G, color.B);

  /// <summary>The drawing leaf — <see cref="Panel" /> seals <c>Render</c>, so the field is painted by a child element.</summary>
  private sealed class Painter(AuroraBackdrop owner) : Control
  {
    /// <inheritdoc />
    public override void Render(DrawingContext context) => owner.Draw(context);
  }
}
