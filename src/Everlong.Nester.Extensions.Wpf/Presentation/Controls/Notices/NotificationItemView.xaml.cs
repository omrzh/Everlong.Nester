using System.ComponentModel;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows;
namespace Everlong.Nester.Presentation;

/// <summary>
///   A view for displaying a notification item.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public partial class NotificationItemView : FeedbackItemViewBase, ISceneTransition
{
  private static readonly TimeSpan EnterDuration = TimeSpan.FromMilliseconds(450);
  private static readonly TimeSpan ExitDuration = TimeSpan.FromMilliseconds(400);

  /// <summary>
  ///   Initializes a new instance of the <see cref="NotificationItemView" /> class.
  /// </summary>
  public NotificationItemView()
  {
    InitializeComponent();
  }

  /// <summary>
  ///   Default enter animation: slides in from the right while fading in.
  ///   The scene player lays the item out at <c>Opacity = 0</c> before this
  ///   runs and restores it afterwards as a safety net.
  /// </summary>
  public Task AnimateEnterAsync(TransitionContext context, CancellationToken token)
    => AnimateAsync(0, 1, 60, 0, true, EnterDuration, token);

  /// <summary>
  ///   Default exit animation: returns the way it came — slides back out to
  ///   the right while fading out.
  /// </summary>
  public Task AnimateExitAsync(TransitionContext context, CancellationToken token)
    => AnimateAsync(1, 0, 0, 60, true, ExitDuration, token);

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
