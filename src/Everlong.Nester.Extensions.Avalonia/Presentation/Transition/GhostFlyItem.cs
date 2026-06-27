using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;

namespace Everlong.Nester.Presentation;

/// <summary>
///   A flying ghost rectangle — animates from <paramref name="SourceRect" />
///   to <paramref name="DestRect" /> and is removed when the flight ends.
/// </summary>
/// <param name="SourceRect">The rectangle the ghost starts at, in the flying canvas' coordinates.</param>
/// <param name="DestRect">The rectangle the ghost ends at, in the flying canvas' coordinates.</param>
/// <param name="Duration">The flight duration; <see langword="null" /> uses 250 ms.</param>
public record GhostFlyItem(PRect SourceRect, PRect DestRect, TimeSpan? Duration = null)
{
  private static readonly TimeSpan DefaultDuration = TimeSpan.FromMilliseconds(250);
  private static readonly Easing DefaultEase = new CubicEaseInOut();

  /// <summary>Gets the fill colour of the ghost.</summary>
  public PColor Fill { get; init; } = PColor.FromArgb(0x40, 0x80, 0x88, 0x90);

  /// <summary>Gets the border colour of the ghost.</summary>
  public PColor Stroke { get; init; } = PColor.FromArgb(0x1A, 0xFF, 0xFF, 0xFF);

  /// <summary>Gets the border thickness of the ghost.</summary>
  public double StrokeThickness { get; init; } = 1;

  /// <summary>Gets the corner radius of the ghost.</summary>
  public PCornerRadius CornerRadius { get; init; } = new(6);

  /// <summary>Gets the opacity at the start of the flight.</summary>
  public double StartOpacity { get; init; } = 0.55;

  /// <summary>Gets the opacity at the end of the flight.</summary>
  public double EndOpacity { get; init; } = 0.05;

  /// <summary>Gets the easing of the flight.</summary>
  public Easing Ease { get; init; } = DefaultEase;

  /// <summary>Gets the shadow cast by the ghost.</summary>
  public BoxShadows Shadow { get; init; } = new BoxShadows(new BoxShadow
  {
    OffsetX = 0,
    OffsetY = 4,
    Blur = 12,
    Color = PColor.FromArgb(0x4D, 0x00, 0x00, 0x00)
  });

  /// <summary>Adds the ghost to <paramref name="canvas" />, flies it to <see cref="DestRect" /> and removes it — also when the flight is cancelled.</summary>
  public async Task FlyAsync(PCanvas canvas, CancellationToken token)
  {
    TimeSpan dur = Duration ?? DefaultDuration;

    double srcW = Math.Max(1d, SourceRect.Width);
    double srcH = Math.Max(1d, SourceRect.Height);
    double destW = Math.Max(1d, DestRect.Width);
    double destH = Math.Max(1d, DestRect.Height);

    Border ghost = new()
    {
      Width = srcW,
      Height = srcH,
      CornerRadius = CornerRadius,
      Background = new SolidColorBrush(Fill),
      BorderBrush = new SolidColorBrush(Stroke),
      BorderThickness = new PThickness(StrokeThickness),
      IsHitTestVisible = false,
      ZIndex = 1000,
      BoxShadow = Shadow
    };

    PCanvas.SetLeft(ghost, SourceRect.X);
    PCanvas.SetTop(ghost, SourceRect.Y);

    canvas.Children.Add(ghost);

    try
    {
      KeyFrame fromKey = new() { Cue = new Cue(0d) };
      fromKey.Setters.Add(new Setter { Property = Canvas.LeftProperty, Value = SourceRect.X });
      fromKey.Setters.Add(new Setter { Property = Canvas.TopProperty, Value = SourceRect.Y });
      fromKey.Setters.Add(new Setter { Property = Layoutable.WidthProperty, Value = srcW });
      fromKey.Setters.Add(new Setter { Property = Layoutable.HeightProperty, Value = srcH });
      fromKey.Setters.Add(new Setter { Property = PVisual.OpacityProperty, Value = StartOpacity });

      KeyFrame toKey = new() { Cue = new Cue(1d) };
      toKey.Setters.Add(new Setter { Property = Canvas.LeftProperty, Value = DestRect.X });
      toKey.Setters.Add(new Setter { Property = Canvas.TopProperty, Value = DestRect.Y });
      toKey.Setters.Add(new Setter { Property = Layoutable.WidthProperty, Value = destW });
      toKey.Setters.Add(new Setter { Property = Layoutable.HeightProperty, Value = destH });
      toKey.Setters.Add(new Setter { Property = PVisual.OpacityProperty, Value = EndOpacity });

      Animation anim = new()
      {
        Duration = dur,
        Easing = Ease,
        FillMode = FillMode.Forward,
        Children = { fromKey, toKey }
      };

      await anim.RunAsync(ghost, token);
    }
    finally
    {
      canvas.Children.Remove(ghost);
    }
  }
}
