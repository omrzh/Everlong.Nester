using System.Windows.Media;
namespace Everlong.Nester.Presentation;

/// <summary>
///   The dimmer's refusal feedback — the shake animation and the
///   blocked-interaction sound.
/// </summary>
public static class DialogShake
{
  /// <summary>Shakes <paramref name="target" /> horizontally.</summary>
  public static async Task ShakeAsync(this PControl target, CancellationToken token = default)
  {
    var transform = target.RenderTransform as TranslateTransform ?? new TranslateTransform();
    target.RenderTransform = transform;

    // The storyboard path does not drive this context; the scene transitions'
    // pattern applies the key frames directly to the transform and awaits the
    // timeline's Completed event.
    var keyAnim = AnimationFactory.CreateShakeKeyFrames(TimeSpan.FromMilliseconds(400));
    var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    keyAnim.Completed += (_, _) => tcs.TrySetResult(true);
    using (token.Register(() => tcs.TrySetCanceled()))
    {
      transform.BeginAnimation(TranslateTransform.XProperty, keyAnim);
      await tcs.Task;
    }
  }

  /// <summary>Plays the blocked-interaction sound.</summary>
  public static void PlayBlockedSound()
  {
    System.Media.SystemSounds.Beep.Play();
  }
}
