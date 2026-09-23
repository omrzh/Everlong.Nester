using Avalonia;
using Avalonia.Media;

namespace Everlong.Nester.Presentation;

/// <summary>
///   Static animation methods for page and dialog transitions.
///   Use directly on a <see cref="PControl"/>,
///   or via <see cref="TransitionContextExtensions"/> for context-aware overloads.
/// </summary>
public static class TransitionEffects
{
  // ── Public animation entry points ──

  /// <summary>Slides <paramref name="target" /> in from <paramref name="from" /> by <paramref name="offset" /> pixels.</summary>
  public static Task SlideInAsync(PControl target,
    SlideDirection from, double offset, int durationMs, CancellationToken token)
  {
    var (dx, dy) = DirectionToVector(from, offset);
    return SlideCore(target, dx, dy, durationMs, token);
  }

  /// <summary>Slides <paramref name="target" /> out towards <paramref name="to" /> by <paramref name="offset" /> pixels.</summary>
  public static Task SlideOutAsync(PControl target,
    SlideDirection to, double offset, int durationMs, CancellationToken token)
  {
    var (dx, dy) = DirectionToVector(to, offset);
    return SlideOutCore(target, -dx, -dy, durationMs, token);
  }

  /// <summary>Fades <paramref name="target" /> in from zero opacity.</summary>
  public static Task FadeInAsync(PControl target,
    int durationMs, CancellationToken token)
  {
    var d = TimeSpan.FromMilliseconds(durationMs);
    target.Opacity = 0;
    return AnimationFactory.CreateDoubleAnimation(PVisual.OpacityProperty, 0, 1, d)
      .RunAsync(target, token);
  }

  /// <summary>Fades <paramref name="target" /> out to zero opacity.</summary>
  public static Task FadeOutAsync(PControl target,
    int durationMs, CancellationToken token)
  {
    var d = TimeSpan.FromMilliseconds(durationMs);
    target.Opacity = 1;
    return AnimationFactory.CreateDoubleAnimation(PVisual.OpacityProperty, 1, 0, d,
      AnimationFactory.DefaultExitEasing).RunAsync(target, token);
  }

  /// <summary>Zooms <paramref name="target" /> in from <paramref name="fromScale" /> with a fade.</summary>
  public static async Task ZoomInAsync(PControl target,
    double fromScale, int durationMs, CancellationToken token)
  {
    target.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
    target.Opacity = 0;
    target.RenderTransform = new ScaleTransform(fromScale, fromScale);

    var d = TimeSpan.FromMilliseconds(durationMs);
    var scaleAnim = AnimationFactory.CreateZoomAnimation(fromScale, 1.0, d);
    var fadeAnim = AnimationFactory.CreateDoubleAnimation(PVisual.OpacityProperty, 0, 1, d);

    await Task.WhenAll(
      scaleAnim.RunAsync(target, token),
      fadeAnim.RunAsync(target, token));
  }

  /// <summary>Zooms <paramref name="target" /> out to <paramref name="toScale" /> with a fade.</summary>
  public static async Task ZoomOutAsync(PControl target,
    double toScale, int durationMs, CancellationToken token)
  {
    target.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
    target.Opacity = 1;
    target.RenderTransform = new ScaleTransform(1.0, 1.0);

    var d = TimeSpan.FromMilliseconds(durationMs);
    var ease = AnimationFactory.DefaultExitEasing;
    var scaleAnim = AnimationFactory.CreateZoomAnimation(1.0, toScale, d, ease);
    var fadeAnim = AnimationFactory.CreateDoubleAnimation(PVisual.OpacityProperty, 1, 0, d, ease);

    await Task.WhenAll(
      scaleAnim.RunAsync(target, token),
      fadeAnim.RunAsync(target, token));
  }

  // ── Element-level flight helpers ──

  /// <summary>Places <paramref name="element" /> at <paramref name="sourceRect" /> — measured against <paramref name="rootBody" /> — and makes it invisible.</summary>
  /// <remarks>Without a frame the element is left untouched.</remarks>
  public static void PrepareFromRect(PVisual element, PRect sourceRect, PVisual? rootBody)
  {
    if (rootBody is null)
      return;

    var pos = element.TranslatePoint(default, rootBody) ?? default;
    element.RenderTransform = new TranslateTransform(
      sourceRect.X - pos.X,
      sourceRect.Y - pos.Y);
    element.Opacity = 0;
  }

  /// <summary>Animates <paramref name="element" /> from its current offset back to its natural position and fades it in.</summary>
  public static async Task FlyToOriginAsync(PVisual element, int durationMs, CancellationToken token)
  {
    if (element.RenderTransform is not TranslateTransform t)
      return;

    double startX = t.X;
    double startY = t.Y;

    if (startX == 0 && startY == 0)
    {
      element.Opacity = 1;
      return;
    }

    var d = TimeSpan.FromMilliseconds(durationMs);
    element.RenderTransform = new TranslateTransform(startX, startY);
    element.Opacity = 0;

    var tasks = new List<Task>(3);
    if (startX != 0)
      tasks.Add(AnimationFactory.CreateSlideAnimation(startX, 0, true, d)
                .RunAsync(element, token));
    if (startY != 0)
      tasks.Add(AnimationFactory.CreateSlideAnimation(startY, 0, false, d)
                .RunAsync(element, token));
    tasks.Add(AnimationFactory.CreateDoubleAnimation(PVisual.OpacityProperty, 0, 1, d)
              .RunAsync(element, token));

    await Task.WhenAll(tasks);
  }

  // ── Internal helpers ──

  private static (double dx, double dy) DirectionToVector(SlideDirection direction, double offset)
    => direction switch
    {
      SlideDirection.BottomToTop => (0d, offset),
      SlideDirection.TopToBottom => (0d, -offset),
      SlideDirection.LeftToRight => (-offset, 0d),
      SlideDirection.RightToLeft => (offset, 0d),
      _ => (0d, 0d),
    };

  internal static async Task SlideCore(
    PVisual target,
    double startX,
    double startY,
    int durationMs,
    CancellationToken token)
  {
    target.RenderTransform = new TranslateTransform(startX, startY);
    target.Opacity = 1;

    bool animateX = startX != 0;
    bool animateY = startY != 0;
    if (!animateX && !animateY)
      return;

    var d = TimeSpan.FromMilliseconds(durationMs);
    var tasks = new List<Task>(2);

    if (animateX)
      tasks.Add(AnimationFactory.CreateSlideAnimation(startX, 0, true, d)
                .RunAsync(target, token));
    if (animateY)
      tasks.Add(AnimationFactory.CreateSlideAnimation(startY, 0, false, d)
                .RunAsync(target, token));

    await Task.WhenAll(tasks);
  }

  internal static async Task SlideOutCore(
    PVisual target,
    double endX,
    double endY,
    int durationMs,
    CancellationToken token)
  {
    target.RenderTransform = new TranslateTransform(0, 0);
    target.Opacity = 1;

    bool animateX = endX != 0;
    bool animateY = endY != 0;
    if (!animateX && !animateY)
    {
      target.Opacity = 0;
      return;
    }

    var d = TimeSpan.FromMilliseconds(durationMs);
    var tasks = new List<Task>(2);

    if (animateX)
      tasks.Add(AnimationFactory.CreateSlideAnimation(0, endX, true, d, AnimationFactory.DefaultExitEasing)
                .RunAsync(target, token));
    if (animateY)
      tasks.Add(AnimationFactory.CreateSlideAnimation(0, endY, false, d, AnimationFactory.DefaultExitEasing)
                .RunAsync(target, token));

    await Task.WhenAll(tasks);
    target.Opacity = 0;
  }

  // ── Hero/composed flight helpers (deferred) ──────────────

  /// <summary>
  ///   Fly element from a source rectangle to its natural position with fade‑in.
  ///   Sets <c>RenderTransform</c> and <c>Opacity = 0</c> immediately.  Returns a
  ///   deferred function; the optional <c>delay</c> is baked in so callers can
  ///   compose staggered groups with a single <c>Task.WhenAll</c>.
  /// </summary>
  public static Func<CancellationToken, Task> FlyIn(
    this TransitionContext ctx,
    PControl element,
    PRect sourceRect,
    int? durationMs = null,
    int delayMs = 0)
  {
    var d = durationMs ?? 200;
    PrepareFromRect(element, sourceRect, ctx.FlyingCanvas);
    return async token =>
    {
      if (delayMs > 0)
        await Task.Delay(delayMs, token);
      await FlyToOriginAsync(element, d, token);
    };
  }

  /// <summary>
  ///   Rise element from below with fade‑in.
  ///   Sets a Y‑offset <c>TranslateTransform</c> and <c>Opacity = 0</c> immediately.
  ///   Returns a deferred function; the optional <c>delay</c> is baked in for
  ///   staggered composition.
  /// </summary>
  public static Func<CancellationToken, Task> RiseIn(
    this TransitionContext ctx,
    PControl element,
    double offsetY = 20,
    int? durationMs = null,
    int delayMs = 0)
  {
    var d = durationMs ?? 200;
    element.RenderTransform = new TranslateTransform(0, offsetY);
    element.Opacity = 0;
    return async token =>
    {
      if (delayMs > 0)
        await Task.Delay(delayMs, token);
      await FlyToOriginAsync(element, d, token);
    };
  }
}
