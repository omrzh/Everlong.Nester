using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

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
  private static readonly IEasingFunction DefaultEase = new CubicEase { EasingMode = EasingMode.EaseInOut };

  /// <summary>Gets the fill colour of the ghost.</summary>
  public PColor Fill { get; init; } = PColor.FromArgb(0x40, 0x80, 0x88, 0x90);

  /// <summary>Gets the border colour of the ghost.</summary>
  public PColor Stroke { get; init; } = PColor.FromArgb(0x1A, 0xFF, 0xFF, 0xFF);

  /// <summary>Gets the border thickness of the ghost.</summary>
  public double StrokeThickness { get; init; } = 1;

  /// <summary>Gets the corner radius of the ghost.</summary>
  public CornerRadius CornerRadius { get; init; } = new(6);

  /// <summary>Gets the opacity at the start of the flight.</summary>
  public double StartOpacity { get; init; } = 0.55;

  /// <summary>Gets the opacity at the end of the flight.</summary>
  public double EndOpacity { get; init; } = 0.05;

  /// <summary>Gets the easing of the flight.</summary>
  public IEasingFunction Ease { get; init; } = DefaultEase;

  /// <summary>Gets the shadow cast by the ghost.</summary>
  public DropShadowEffect? Shadow { get; init; } = new()
  {
    Color = Colors.Black,
    BlurRadius = 12,
    ShadowDepth = 4,
    Direction = 270,
    Opacity = 0.3
  };

  /// <summary>Adds the ghost to <paramref name="ctx" />'s flying canvas, flies it to <see cref="DestRect" /> and removes it — also when the flight is cancelled.</summary>
  public async Task FlyAsync(TransitionContext ctx, CancellationToken token)
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
      Effect = Shadow
    };

    Canvas.SetLeft(ghost, SourceRect.X);
    Canvas.SetTop(ghost, SourceRect.Y);
    Panel.SetZIndex(ghost, 1000);

    ctx.FlyingCanvas.Children.Add(ghost);

    try
    {
      Storyboard sb = new();

      AddAnimation(sb, ghost, "(Canvas.Left)", SourceRect.X, DestRect.X);
      AddAnimation(sb, ghost, "(Canvas.Top)", SourceRect.Y, DestRect.Y);
      AddAnimation(sb, ghost, "Width", srcW, destW);
      AddAnimation(sb, ghost, "Height", srcH, destH);
      AddAnimation(sb, ghost, "Opacity", StartOpacity, EndOpacity);

      await sb.RunAsync(token);
    }
    finally
    {
      ctx.FlyingCanvas.Children.Remove(ghost);
    }
  }

  private void AddAnimation(Storyboard sb, DependencyObject target, string propertyPath, double from, double to)
  {
    DoubleAnimation anim = new()
    {
      From = from,
      To = to,
      Duration = Duration ?? DefaultDuration,
      EasingFunction = Ease
    };

    Storyboard.SetTarget(anim, target);
    Storyboard.SetTargetProperty(anim, new PropertyPath(propertyPath));
    sb.Children.Add(anim);
  }
}
