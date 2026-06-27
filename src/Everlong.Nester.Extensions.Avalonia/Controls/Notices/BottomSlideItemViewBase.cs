using Avalonia.Media;
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
  public async Task AnimateEnterAsync(TransitionContext context, CancellationToken token)
  {
    RenderTransform = new TranslateTransform(0, 60);
    try
    {
      await Task.WhenAll(
        AnimationFactory.CreateSlideAnimation(60, 0, false, EnterDuration).RunAsync(this, token),
        AnimationFactory.CreateDoubleAnimation(OpacityProperty, 0, 1, EnterDuration).RunAsync(this, token));
    }
    finally
    {
      // The rise-in ends at the identity transform — release the transform
      // so later layout (and the exit scene) starts clean.
      RenderTransform = null;
    }
  }

  /// <inheritdoc />
  public async Task AnimateExitAsync(TransitionContext context, CancellationToken token)
  {
    RenderTransform = new TranslateTransform();
    try
    {
      await Task.WhenAll(
        AnimationFactory.CreateSlideAnimation(0, 60, false, ExitDuration).RunAsync(this, token),
        AnimationFactory.CreateDoubleAnimation(OpacityProperty, 1, 0, ExitDuration).RunAsync(this, token));
    }
    finally
    {
      RenderTransform = null;
    }
  }
}
