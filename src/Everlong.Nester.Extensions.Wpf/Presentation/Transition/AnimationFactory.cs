using System.Windows;
using System.Windows.Media.Animation;

namespace Everlong.Nester.Presentation;
/// <summary>
///   Factory class for creating common scene-transition animations.
/// </summary>
internal static class AnimationFactory
{
  /// <summary>
  ///   Creates a basic Double animation for a specific property.
  /// </summary>
  public static Storyboard CreateDoubleAnimation(
    DependencyObject target,
    string propertyPath,
    double from,
    double to,
    TimeSpan duration,
    IEasingFunction? easing = null)
  {
    DoubleAnimation animation = new()
    {
      From = from,
      To = to,
      Duration = duration,
      EasingFunction = easing
    };

    Storyboard.SetTarget(animation, target);
    Storyboard.SetTargetProperty(animation, new PropertyPath(propertyPath));

    Storyboard sb = new();
    sb.Children.Add(animation);
    return sb;
  }

  /// <summary>
  ///   Creates the shake animation's key frames — a damped horizontal
  ///   oscillation: 0 → −12 → +12 → −8 → +8 → 0.
  ///   Use <see cref="FillBehavior.Stop"/> so the element returns to its
  ///   original position when the animation ends.
  /// </summary>
  public static DoubleAnimationUsingKeyFrames CreateShakeKeyFrames(TimeSpan duration)
  {
    DoubleAnimationUsingKeyFrames keyAnim = new()
    {
      Duration = duration,
      FillBehavior = FillBehavior.Stop
    };

    keyAnim.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(0.0)));
    keyAnim.KeyFrames.Add(new LinearDoubleKeyFrame(-12, KeyTime.FromPercent(0.2)));
    keyAnim.KeyFrames.Add(new LinearDoubleKeyFrame(12, KeyTime.FromPercent(0.4)));
    keyAnim.KeyFrames.Add(new LinearDoubleKeyFrame(-8, KeyTime.FromPercent(0.6)));
    keyAnim.KeyFrames.Add(new LinearDoubleKeyFrame(8, KeyTime.FromPercent(0.8)));
    keyAnim.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(1.0)));

    return keyAnim;
  }

  /// <summary>
  ///   Runs the storyboard asynchronously.
  /// </summary>
  public static async Task RunAsync(this Storyboard storyboard, CancellationToken token = default)
  {
    token.ThrowIfCancellationRequested();
    TaskCompletionSource<bool> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    var dispatcher = Application.Current?.Dispatcher;
    CancellationTokenRegistration registration = default;

    EventHandler? onCompleted = null;
    onCompleted = (s, e) =>
    {
      storyboard.Completed -= onCompleted;
      tcs.TrySetResult(true);
    };

    storyboard.Completed += onCompleted;

    if (token.CanBeCanceled)
    {
      registration = token.Register(() =>
      {
        if (dispatcher != null && !dispatcher.CheckAccess())
        {
          _ = dispatcher.BeginInvoke(new Action(() => CancelStoryboard(storyboard, onCompleted, tcs)));
          return;
        }

        CancelStoryboard(storyboard, onCompleted, tcs);
      });
    }

    try
    {
      if (dispatcher != null && !dispatcher.CheckAccess())
      {
        await dispatcher.InvokeAsync(storyboard.Begin);
      }
      else
      {
        storyboard.Begin();
      }

      await tcs.Task;
    }
    finally
    {
      registration.Dispose();
      storyboard.Completed -= onCompleted;
    }
  }

  private static void CancelStoryboard(Storyboard storyboard, EventHandler? onCompleted, TaskCompletionSource<bool> tcs)
  {
    storyboard.Stop();
    if (onCompleted != null)
    {
      storyboard.Completed -= onCompleted;
    }

    tcs.TrySetCanceled();
  }
}
