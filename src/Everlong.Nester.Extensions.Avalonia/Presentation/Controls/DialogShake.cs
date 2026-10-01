using Avalonia.Controls;
using Avalonia.Media;
using Everlong.Nester.Helpers;
namespace Everlong.Nester.Presentation;

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
  /// <remarks>Plays the system beep on Windows; a no-op elsewhere, where no playback path is wired.</remarks>
  public static void PlayBlockedSound()
  {
    if (OperatingSystem.IsWindows())
      Win32Helper.MessageBeep(MB_SIMPLE);
  }

  /// <summary>The <c>MB_SIMPLE</c> sound identifier of <c>MessageBeep</c>.</summary>
  private const uint MB_SIMPLE = 0xFFFFFFFF;
}
