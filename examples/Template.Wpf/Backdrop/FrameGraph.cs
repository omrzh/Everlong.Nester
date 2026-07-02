using System.Windows;
using System.Windows.Media;

namespace NesterApp.Backdrop;

/// <summary>Draws a rolling frame-time sparkline from a <see cref="FrameStats" /> window.</summary>
public sealed class FrameGraph : FrameworkElement
{
  /// <summary>The frame-time scale top, in milliseconds (≈30 fps).</summary>
  private const double ScaleMs = 33.4;

  /// <summary>The 60 fps guide position.</summary>
  private const double GuideMs = 16.7;

  /// <summary>The surface colour.</summary>
  public static readonly DependencyProperty SurfaceBrushProperty =
    DependencyProperty.Register(nameof(SurfaceBrush), typeof(Brush), typeof(FrameGraph),
                                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

  /// <summary>The sparkline colour.</summary>
  public static readonly DependencyProperty LineBrushProperty =
    DependencyProperty.Register(nameof(LineBrush), typeof(Brush), typeof(FrameGraph),
                                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

  /// <summary>The guide colour.</summary>
  public static readonly DependencyProperty GuideBrushProperty =
    DependencyProperty.Register(nameof(GuideBrush), typeof(Brush), typeof(FrameGraph),
                                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

  private FrameStats? _stats;

  /// <summary>The surface colour.</summary>
  public Brush? SurfaceBrush
  {
    get => (Brush?)GetValue(SurfaceBrushProperty);
    set => SetValue(SurfaceBrushProperty, value);
  }

  /// <summary>The sparkline colour.</summary>
  public Brush? LineBrush
  {
    get => (Brush?)GetValue(LineBrushProperty);
    set => SetValue(LineBrushProperty, value);
  }

  /// <summary>The guide colour.</summary>
  public Brush? GuideBrush
  {
    get => (Brush?)GetValue(GuideBrushProperty);
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
  protected override void OnRender(DrawingContext dc)
  {
    var bounds = new Rect(RenderSize);
    if (bounds.Width < 4 || bounds.Height < 4)
      return;

    dc.DrawRoundedRectangle(SurfaceBrush, null, bounds, 4, 4);

    double guideY = bounds.Height * (1 - GuideMs / ScaleMs);
    if (GuideBrush is { } guide)
    {
      // Not frozen on purpose: the brush is a shared theme resource that
      // already has an inheritance context once it is in the live tree, and
      // freezing a Freezable that holds it throws (Freezable.FreezeCore
      // cannot freeze the child brush).
      var pen = new Pen(guide, 1) { DashStyle = new DashStyle([3, 3], 0) };
      dc.DrawLine(pen, new Point(0, guideY), new Point(bounds.Width, guideY));
    }

    if (_stats is not { Count: > 1 } stats || LineBrush is not { } line)
      return;

    int count = stats.Count;
    double step = bounds.Width / (count - 1);
    var geometry = new StreamGeometry();
    using (StreamGeometryContext ctx = geometry.Open())
    {
      ctx.BeginFigure(ToPoint(stats, 0, bounds, step), false, false);
      for (int i = 1; i < count; i++)
        ctx.LineTo(ToPoint(stats, i, bounds, step), true, false);
    }

    geometry.Freeze();
    var strokePen = new Pen(line, 1.4) { LineJoin = PenLineJoin.Round };
    dc.DrawGeometry(null, strokePen, geometry);
  }

  private static Point ToPoint(FrameStats stats, int index, Rect bounds, double step)
  {
    double normalized = Math.Clamp(stats[stats.Count - 1 - index] / ScaleMs, 0, 1);
    return new Point(index * step, bounds.Height * (1 - normalized));
  }

  private void OnSampled(object? sender, EventArgs e) => InvalidateVisual();
}
