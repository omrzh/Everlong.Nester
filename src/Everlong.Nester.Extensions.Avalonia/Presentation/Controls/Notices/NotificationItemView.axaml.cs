using Avalonia.Media;
namespace Everlong.Nester.Presentation;

/// <summary>
///   A view for displaying a notification item.
/// </summary>
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
  public async Task AnimateEnterAsync(TransitionContext context, CancellationToken token)
  {
    RenderTransform = new TranslateTransform(60, 0);
    try
    {
      await Task.WhenAll(
        AnimationFactory.CreateSlideAnimation(60, 0, true, EnterDuration).RunAsync(this, token),
        AnimationFactory.CreateDoubleAnimation(OpacityProperty, 0, 1, EnterDuration).RunAsync(this, token));
    }
    finally
    {
      // The slide-in ends at the identity transform — release the transform
      // so later layout (and the exit scene) starts clean.
      RenderTransform = null;
    }
  }

  /// <summary>
  ///   Default exit animation: returns the way it came — slides back out to
  ///   the right while fading out.
  /// </summary>
  public async Task AnimateExitAsync(TransitionContext context, CancellationToken token)
  {
    RenderTransform = new TranslateTransform();
    try
    {
      await Task.WhenAll(
        AnimationFactory.CreateSlideAnimation(0, 60, true, ExitDuration).RunAsync(this, token),
        AnimationFactory.CreateDoubleAnimation(OpacityProperty, 1, 0, ExitDuration).RunAsync(this, token));
    }
    finally
    {
      RenderTransform = null;
    }
  }
}
