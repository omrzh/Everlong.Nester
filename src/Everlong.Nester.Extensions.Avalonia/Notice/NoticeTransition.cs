using Everlong.Nester.Presentation;
// NOTE: Single-source file — the Extensions WPF project compiles this exact
// file via <Compile Include> in Everlong.Nester.Extensions.Wpf.csproj.
// Edit it here only; never create a WPF-side copy (the two builds would drift).

namespace Everlong.Nester.Notice;

/// <summary>
///   Position-aware director contract for notice items.  The scene player
///   prefers the view's own <see cref="ISceneTransition" />; views without
///   one fall back to <see cref="DefaultNoticeDirector" />.
/// </summary>
internal interface INoticeDirector
{
  /// <summary>
  ///   Plays the enter scene for a notice item anchored at
  ///   <paramref name="position" />.
  /// </summary>
  Task AnimateEnterAsync(TransitionContext context, NoticePosition position, CancellationToken token);

  /// <summary>
  ///   Plays the exit scene for a notice item anchored at
  ///   <paramref name="position" />.
  /// </summary>
  Task AnimateExitAsync(TransitionContext context, NoticePosition position, CancellationToken token);
}

/// <summary>
///   Default notice director: slides in from the edge matching the item's
///   vertical anchor (bottom-positioned items rise, top-positioned items
///   drop) while fading, and exits the same way it entered.
/// </summary>
internal sealed class DefaultNoticeDirector : INoticeDirector
{
  /// <summary>The shared instance — the director is stateless.</summary>
  public static readonly DefaultNoticeDirector Instance = new();

  private static readonly TimeSpan EnterDuration = TimeSpan.FromMilliseconds(450);
  private static readonly TimeSpan ExitDuration = TimeSpan.FromMilliseconds(400);

  public Task AnimateEnterAsync(TransitionContext context, NoticePosition position, CancellationToken token)
  {
    PControl target = context.ArrivingChain[^1];
    return Task.WhenAll(
      TransitionEffects.SlideInAsync(target, DirectionOf(position), 60, (int)EnterDuration.TotalMilliseconds, token),
      TransitionEffects.FadeInAsync(target, (int)EnterDuration.TotalMilliseconds, token));
  }

  public Task AnimateExitAsync(TransitionContext context, NoticePosition position, CancellationToken token)
  {
    PControl target = context.DepartingChain[^1];
    return Task.WhenAll(
      TransitionEffects.SlideOutAsync(target, DirectionOf(position), 60, (int)ExitDuration.TotalMilliseconds, token),
      TransitionEffects.FadeOutAsync(target, (int)ExitDuration.TotalMilliseconds, token));
  }

  private static SlideDirection DirectionOf(NoticePosition position)
    => position is NoticePosition.TopLeft or NoticePosition.TopCenter or NoticePosition.TopRight
      ? SlideDirection.TopToBottom
      : SlideDirection.BottomToTop;
}
