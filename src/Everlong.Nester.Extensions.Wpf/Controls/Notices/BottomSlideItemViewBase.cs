using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows;
using Everlong.Nester.Presentation;
namespace Everlong.Nester.Controls;

/// <summary>
///   Base class for notice item views that enter from the bottom edge:
///   slides up while fading in, slides back down while fading out.
/// </summary>
public abstract class BottomSlideItemViewBase : FeedbackItemViewBase, ISceneTransition
{
  private static readonly TimeSpan EnterDuration = TimeSpan.FromMilliseconds(450);
  private static readonly TimeSpan ExitDuration = TimeSpan.FromMilliseconds(400);

  /// <inheritdoc />
  public Task AnimateEnterAsync(TransitionContext context, CancellationToken token)
    => AnimateAsync(0, 1, 60, 0, false, EnterDuration, token);

  /// <inheritdoc />
  public Task AnimateExitAsync(TransitionContext context, CancellationToken token)
    => AnimateAsync(1, 0, 0, 60, false, ExitDuration, token);

  private async Task AnimateAsync(double fromOpacity, double toOpacity,
                                  double from, double to, bool horizontal,
                                  TimeSpan duration, CancellationToken token)
  {
    RenderTransform = horizontal
      ? new TranslateTransform(from, 0)
      : new TranslateTransform(0, from);
    try
    {
      var storyboard = new Storyboard();

      var opacity = new DoubleAnimation(fromOpacity, toOpacity, duration)
      {
        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
      };
      Storyboard.SetTarget(opacity, this);
      Storyboard.SetTargetProperty(opacity, new PropertyPath(OpacityProperty));

      string axis = horizontal ? "X" : "Y";
      var slide = new DoubleAnimation(from, to, duration)
      {
        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
      };
      Storyboard.SetTarget(slide, this);
      Storyboard.SetTargetProperty(slide,
        new PropertyPath($"(UIElement.RenderTransform).(TranslateTransform.{axis})"));

      storyboard.Children.Add(opacity);
      storyboard.Children.Add(slide);

      var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
      storyboard.Completed += (_, _) => tcs.TrySetResult();
      using CancellationTokenRegistration registration = token.Register(() =>
      {
        storyboard.Stop();
        tcs.TrySetCanceled(token);
      });

      storyboard.Begin();
      await tcs.Task;
    }
    finally
    {
      RenderTransform = null;
    }
  }
}
