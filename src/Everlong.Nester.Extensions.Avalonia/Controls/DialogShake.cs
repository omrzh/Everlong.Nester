using Avalonia.Controls;
using Avalonia.Media;
using Everlong.Nester.Presentation;
namespace Everlong.Nester.Controls;

/// <summary>
///   The dimmer's refusal feedback — the shake animation and the
///   blocked-interaction sound.
/// </summary>
public static class DialogShake
{
  /// <summary>Shakes <paramref name="target" /> horizontally.</summary>
  public static async Task ShakeAsync(this Control target, CancellationToken token = default)
  {
    if (target.RenderTransform is not TranslateTransform)
      target.RenderTransform = new TranslateTransform();
    var anim = AnimationFactory.CreateShakeAnimation(TimeSpan.FromMilliseconds(400));
    await anim.RunAsync(target, token);
  }

  /// <summary>Plays the blocked-interaction sound.</summary>
  /// <remarks>Avalonia carries no playback path; the call returns immediately.</remarks>
  public static void PlayBlockedSound()
  {
    // TODO: the blocked-interaction sound has no Avalonia playback path yet
    // (the WPF twin plays SystemSounds.Beep).  The method is public API — keep
    // it, fill the body when the feedback is wired (docs/backlog.md).
  }
}
