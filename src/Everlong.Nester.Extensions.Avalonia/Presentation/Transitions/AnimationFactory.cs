using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Media;
using Avalonia.Styling;

// ReSharper disable once CheckNamespace
namespace Everlong.Nester.Presentation;
/// <summary>
///   Factory class for creating common scene-transition animations.
/// </summary>
internal static class AnimationFactory
{
  private static readonly SplineEasing DefaultEasing = new(0.4, 0.0, 0.2, 1.0);
  internal static readonly Easing DefaultExitEasing = new LinearEasing();

  /// <summary>
  ///   Creates a basic Double animation for a specific property.
  /// </summary>
  public static Animation CreateDoubleAnimation(
    AvaloniaProperty property,
    double from,
    double to,
    TimeSpan duration,
    Easing? easing = null)
  {
    return new Animation
    {
      Duration = duration,
      Easing = easing ?? DefaultEasing,
      FillMode = FillMode.Forward,
      Children =
      {
        new KeyFrame
        {
          Cue = new Cue(0d),
          Setters = { new Setter { Property = property, Value = from } }
        },
        new KeyFrame
        {
          Cue = new Cue(1d),
          Setters = { new Setter { Property = property, Value = to } }
        }
      }
    };
  }

  /// <summary>
  ///   Creates a slide animation using TranslateTransform.
  /// </summary>
  public static Animation CreateSlideAnimation(
    double fromDistance,
    double toDistance,
    bool isHorizontal,
    TimeSpan duration,
    Easing? easing = null)
  {
    StyledProperty<double> property = isHorizontal ? TranslateTransform.XProperty : TranslateTransform.YProperty;
    return CreateDoubleAnimation(property, fromDistance, toDistance, duration, easing);
  }

  /// <summary>
  ///   Creates a shake animation using TranslateTransform, suitable for indicating a rejected interaction.
  /// </summary>
  /// <remarks>
  ///   Animates <see cref="TranslateTransform.XProperty"/> through a damped horizontal oscillation:
  ///   0 → −12 → +12 → −8 → +8 → 0.
  ///   Use <see cref="FillMode.None"/> so the element returns to its original position when the animation ends.
  ///   Ensure the target's <c>RenderTransform</c> is a <see cref="TranslateTransform"/> before running.
  /// </remarks>
  public static Animation CreateShakeAnimation(TimeSpan duration)
  {
    return new Animation
    {
      Duration = duration,
      FillMode = FillMode.None,
      Children =
      {
        new KeyFrame { Cue = new Cue(0.0),  Setters = { new Setter(TranslateTransform.XProperty, 0.0) } },
        new KeyFrame { Cue = new Cue(0.2),  Setters = { new Setter(TranslateTransform.XProperty, -12.0) } },
        new KeyFrame { Cue = new Cue(0.4),  Setters = { new Setter(TranslateTransform.XProperty, 12.0) } },
        new KeyFrame { Cue = new Cue(0.6),  Setters = { new Setter(TranslateTransform.XProperty, -8.0) } },
        new KeyFrame { Cue = new Cue(0.8),  Setters = { new Setter(TranslateTransform.XProperty, 8.0) } },
        new KeyFrame { Cue = new Cue(1.0),  Setters = { new Setter(TranslateTransform.XProperty, 0.0) } }
      }
    };
  }

  /// <summary>
  ///   Creates a zoom animation using ScaleTransform.
  ///   Note: This assumes the target element uses ScaleTransform or the animation system can handle it.
  /// </summary>
  public static Animation CreateZoomAnimation(
    double fromScale,
    double toScale,
    TimeSpan duration,
    Easing? easing = null)
  {
    // Animate both X and Y scales
    return new Animation
    {
      Duration = duration,
      Easing = easing ?? DefaultEasing,
      FillMode = FillMode.Forward,
      Children =
      {
        new KeyFrame
        {
          Cue = new Cue(0d),
          Setters =
          {
            new Setter { Property = ScaleTransform.ScaleXProperty, Value = fromScale },
            new Setter { Property = ScaleTransform.ScaleYProperty, Value = fromScale }
          }
        },
        new KeyFrame
        {
          Cue = new Cue(1d),
          Setters =
          {
            new Setter { Property = ScaleTransform.ScaleXProperty, Value = toScale },
            new Setter { Property = ScaleTransform.ScaleYProperty, Value = toScale }
          }
        }
      }
    };
  }
}
