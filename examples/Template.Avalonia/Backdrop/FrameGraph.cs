using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace NesterApp.Backdrop;

/// <summary>Draws a rolling frame-time sparkline from a <see cref="FrameStats" /> window.</summary>
public sealed class FrameGraph : Control
{
  /// <summary>The frame-time scale top, in milliseconds (≈30 fps).</summary>
  private const double ScaleMs = 33.4;

  /// <summary>The 60 fps guide position.</summary>
  private const double GuideMs = 16.7;

  /// <summary>The surface colour.</summary>
  public static readonly StyledProperty<IBrush?> SurfaceBrushProperty =
    AvaloniaProperty.Register<FrameGraph, IBrush?>(nameof(SurfaceBrush));

  /// <summary>The sparkline colour.</summary>
  public static readonly StyledProperty<IBrush?> LineBrushProperty =
    AvaloniaProperty.Register<FrameGraph, IBrush?>(nameof(LineBrush));

  /// <summary>The guide colour.</summary>
  public static readonly StyledProperty<IBrush?> GuideBrushProperty =
    AvaloniaProperty.Register<FrameGraph, IBrush?>(nameof(GuideBrush));

  private FrameStats? _stats;

  static FrameGraph()
  {
    AffectsRender<FrameGraph>(SurfaceBrushProperty, LineBrushProperty, GuideBrushProperty);
  }

  /// <summary>The surface colour.</summary>
  public IBrush? SurfaceBrush
  {
    get => GetValue(SurfaceBrushProperty);
    set => SetValue(SurfaceBrushProperty, value);
  }

  /// <summary>The sparkline colour.</summary>
  public IBrush? LineBrush
  {
    get => GetValue(LineBrushProperty);
    set => SetValue(LineBrushProperty, value);
  }

  /// <summary>The guide colour.</summary>
  public IBrush? GuideBrush
  {
    get => GetValue(GuideBrushProperty);
    set => SetValue(GuideBrushProperty, value);
  }

  /// <summary>The sampled window; the graph invalidates itself on every recorded frame.</summary>
  public FrameStats? Stats
  {
    get => _stats;
    set
    {
      if (ReferenceEquals(_stats, value))
        return;

      _stats?.Sampled -= OnSampled;

      _stats = value;

      _stats?.Sampled += OnSampled;

      InvalidateVisual();
    }
  }

  /// <inheritdoc />
  public override void Render(DrawingContext context)
  {
    var bounds = new Rect(Bounds.Size);
    if (bounds.Width < 4 || bounds.Height < 4)
      return;

    if (SurfaceBrush is { } surface)
      context.FillRectangle(surface, bounds, 4);

    double guideY = bounds.Height * (1 - GuideMs / ScaleMs);
    if (GuideBrush is { } guide)
      context.DrawLine(new Pen(guide, 1) { DashStyle = new DashStyle([3, 3], 0) },
                       new Point(0, guideY), new Point(bounds.Width, guideY));

    if (_stats is not { Count: > 1 } stats || LineBrush is not { } line)
      return;

    int count = stats.Count;
    double step = bounds.Width / (count - 1);
    var geometry = new StreamGeometry();
    using (StreamGeometryContext ctx = geometry.Open())
    {
      ctx.BeginFigure(ToPoint(stats, 0, bounds, step), false);
      for (int i = 1; i < count; i++)
        ctx.LineTo(ToPoint(stats, i, bounds, step));
      ctx.EndFigure(false);
    }

    context.DrawGeometry(null, new Pen(line, 1.4, lineJoin: PenLineJoin.Round), geometry);
  }

  private static Point ToPoint(FrameStats stats, int index, Rect bounds, double step)
  {
    double normalized = Math.Clamp(stats[stats.Count - 1 - index] / ScaleMs, 0, 1);
    return new Point(index * step, bounds.Height * (1 - normalized));
  }

  private void OnSampled(object? sender, EventArgs e) => InvalidateVisual();
}
