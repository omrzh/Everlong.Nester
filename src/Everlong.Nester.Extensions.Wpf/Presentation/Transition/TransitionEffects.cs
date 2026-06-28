using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Everlong.Nester.Presentation;
/// <summary>
///   Static animation methods for page and dialog transitions.
///   Use directly on a <see cref="PControl"/>,
///   or via <see cref="TransitionContextExtensions"/> for context-aware overloads.
/// </summary>
public static class TransitionEffects
{
  internal static readonly IEasingFunction DefaultEase = new SplineEasingFunction(0.4, 0.0, 0.2, 1.0);
  internal static readonly IEasingFunction DefaultExitEase = new SplineEasingFunction(0.0, 0.0, 1.0, 1.0);

  // ── Public animation entry points ──

  /// <summary>Slides <paramref name="target" /> in from <paramref name="from" /> by <paramref name="offset" /> pixels.</summary>
  public static Task SlideInAsync(PControl target,
                                  SlideDirection from,
                                  double offset,
                                  int durationMs,
                                  CancellationToken token)
  {
    var (dx, dy) = DirectionToVector(from, offset);
    return SlideCore(target, dx, dy, durationMs, token);
  }

  /// <summary>Slides <paramref name="target" /> out towards <paramref name="to" /> by <paramref name="offset" /> pixels.</summary>
  public static Task SlideOutAsync(PControl target,
                                   SlideDirection to,
                                   double offset,
                                   int durationMs,
                                   CancellationToken token)
  {
    var (dx, dy) = DirectionToVector(to, offset);
    return SlideOutCore(target, -dx, -dy, durationMs, token);
  }

  /// <summary>Fades <paramref name="target" /> in from zero opacity.</summary>
  public static Task FadeInAsync(PControl target,
                                 int durationMs,
                                 CancellationToken token)
  {
    target.Opacity = 0;

    var d = TimeSpan.FromMilliseconds(durationMs);
    var tcs = new TaskCompletionSource();
    var anim = new DoubleAnimation(0, 1, new Duration(d));

    anim.Completed += (_, _) => tcs.TrySetResult();

    using CancellationTokenRegistration reg = token.Register(() => tcs.TrySetCanceled());
    target.BeginAnimation(UIElement.OpacityProperty, anim);
    return tcs.Task;
  }

  /// <summary>Fades <paramref name="target" /> out to zero opacity.</summary>
  public static Task FadeOutAsync(PControl target,
                                  int durationMs,
                                  CancellationToken token)
  {
    target.Opacity = 1;

    var d = TimeSpan.FromMilliseconds(durationMs);
    var tcs = new TaskCompletionSource();
    var anim = new DoubleAnimation(1, 0, new Duration(d));

    anim.Completed += (_, _) => tcs.TrySetResult();

    using CancellationTokenRegistration reg = token.Register(() => tcs.TrySetCanceled());
    target.BeginAnimation(UIElement.OpacityProperty, anim);
    return tcs.Task;
  }

  /// <summary>Zooms <paramref name="target" /> in from <paramref name="fromScale" /> with a fade.</summary>
  public static async Task ZoomInAsync(PControl target,
                                       double fromScale,
                                       int durationMs,
                                       CancellationToken token)
  {
    target.RenderTransformOrigin = new Point(0.5, 0.5);
    target.Opacity = 0;
    var transform = new ScaleTransform(fromScale, fromScale);
    target.RenderTransform = transform;

    var d = TimeSpan.FromMilliseconds(durationMs);
    var tcs = new TaskCompletionSource();
    int pending = 3;

    void OnCompleted(object? s, EventArgs e)
    {
      if (Interlocked.Decrement(ref pending) == 0)
        tcs.TrySetResult();
    }

    IEasingFunction ease = DefaultEase;

    var sxAnim = new DoubleAnimation(fromScale, 1.0, new Duration(d))
    { EasingFunction = ease };
    sxAnim.Completed += OnCompleted;
    transform.BeginAnimation(ScaleTransform.ScaleXProperty, sxAnim);

    var syAnim = new DoubleAnimation(fromScale, 1.0, new Duration(d))
    { EasingFunction = ease };
    syAnim.Completed += OnCompleted;
    transform.BeginAnimation(ScaleTransform.ScaleYProperty, syAnim);

    var fadeAnim = new DoubleAnimation(0, 1, new Duration(d))
    { EasingFunction = ease };
    fadeAnim.Completed += OnCompleted;
    target.BeginAnimation(UIElement.OpacityProperty, fadeAnim);

    await using (token.Register(() => tcs.TrySetCanceled()))
      await tcs.Task;
  }

  /// <summary>Zooms <paramref name="target" /> out to <paramref name="toScale" /> with a fade.</summary>
  public static async Task ZoomOutAsync(PControl target,
                                        double toScale,
                                        int durationMs,
                                        CancellationToken token)
  {
    target.RenderTransformOrigin = new Point(0.5, 0.5);
    target.Opacity = 1;
    var transform = new ScaleTransform(1.0, 1.0);
    target.RenderTransform = transform;

    var d = TimeSpan.FromMilliseconds(durationMs);
    var tcs = new TaskCompletionSource();
    int pending = 3;

    void OnCompleted(object? s, EventArgs e)
    {
      if (Interlocked.Decrement(ref pending) == 0)
        tcs.TrySetResult();
    }

    IEasingFunction ease = DefaultExitEase;

    var sxAnim = new DoubleAnimation(1.0, toScale, new Duration(d))
    { EasingFunction = ease };
    sxAnim.Completed += OnCompleted;
    transform.BeginAnimation(ScaleTransform.ScaleXProperty, sxAnim);

    var syAnim = new DoubleAnimation(1.0, toScale, new Duration(d))
    { EasingFunction = ease };
    syAnim.Completed += OnCompleted;
    transform.BeginAnimation(ScaleTransform.ScaleYProperty, syAnim);

    var fadeAnim = new DoubleAnimation(1, 0, new Duration(d))
    { EasingFunction = ease };
    fadeAnim.Completed += OnCompleted;
    target.BeginAnimation(UIElement.OpacityProperty, fadeAnim);

    await using (token.Register(() => tcs.TrySetCanceled()))
      await tcs.Task;
  }

  // ── Element-level flight helpers ──

  /// <summary>Places <paramref name="element" /> at <paramref name="sourceRect" /> — measured against <paramref name="rootBody" /> — and makes it invisible.</summary>
  public static void PrepareFromRect(UIElement element, PRect sourceRect, PVisual rootBody)
  {
    var pos = element.TranslatePoint(new Point(0, 0), rootBody);
    element.RenderTransform = new TranslateTransform(
      sourceRect.X - pos.X,
      sourceRect.Y - pos.Y);
    element.Opacity = 0;
  }

  /// <summary>Animates <paramref name="element" /> from its current offset back to its natural position and fades it in.</summary>
  public static async Task FlyToOriginAsync(UIElement element, int durationMs, CancellationToken token)
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

    element.RenderTransform = new TranslateTransform(startX, startY);
    element.Opacity = 0;

    var d = TimeSpan.FromMilliseconds(durationMs);
    var tcs = new TaskCompletionSource();
    int pending = 0;

    void OnCompleted(object? s, EventArgs e)
    {
      if (Interlocked.Decrement(ref pending) == 0)
        tcs.TrySetResult();
    }

    if (startX != 0)
    {
      Interlocked.Increment(ref pending);
      var xAnim = new DoubleAnimation(startX, 0, new Duration(d)) { EasingFunction = DefaultEase };
      xAnim.Completed += OnCompleted;
      ((TranslateTransform)element.RenderTransform).BeginAnimation(TranslateTransform.XProperty, xAnim);
    }

    if (startY != 0)
    {
      Interlocked.Increment(ref pending);
      var yAnim = new DoubleAnimation(startY, 0, new Duration(d)) { EasingFunction = DefaultEase };
      yAnim.Completed += OnCompleted;
      ((TranslateTransform)element.RenderTransform).BeginAnimation(TranslateTransform.YProperty, yAnim);
    }

    Interlocked.Increment(ref pending);
    var fadeAnim = new DoubleAnimation(0, 1, new Duration(d)) { EasingFunction = DefaultEase };
    fadeAnim.Completed += OnCompleted;
    element.BeginAnimation(UIElement.OpacityProperty, fadeAnim);

    await using (token.Register(() => tcs.TrySetCanceled()))
      await tcs.Task;
  }

  // ── Internal helpers ──

  private static (double dx, double dy) DirectionToVector(SlideDirection direction, double offset)
  {
    return direction switch
    {
      SlideDirection.BottomToTop => (0d, offset),
      SlideDirection.TopToBottom => (0d, -offset),
      SlideDirection.LeftToRight => (-offset, 0d),
      SlideDirection.RightToLeft => (offset, 0d),
      _ => (0d, 0d),
    };
  }

  internal static async Task SlideCore(PVisual target,
                                       double startX,
                                       double startY,
                                       int durationMs,
                                       CancellationToken token)
  {
    target.RenderTransform = new TranslateTransform(startX, startY);
    target.Opacity = 1;

    bool animateX = startX != 0;
    bool animateY = startY != 0;

    var tcs = new TaskCompletionSource();
    int pending = 0;

    void OnCompleted(object? s, EventArgs e)
    {
      if (Interlocked.Decrement(ref pending) == 0)
        tcs.TrySetResult();
    }

    var d = TimeSpan.FromMilliseconds(durationMs);
    if (animateX)
    {
      Interlocked.Increment(ref pending);
      var xAnim = new DoubleAnimation(startX, 0, new Duration(d)) { EasingFunction = DefaultEase };
      xAnim.Completed += OnCompleted;
      ((TranslateTransform)target.RenderTransform).BeginAnimation(TranslateTransform.XProperty, xAnim);
    }

    if (animateY)
    {
      Interlocked.Increment(ref pending);
      var yAnim = new DoubleAnimation(startY, 0, new Duration(d)) { EasingFunction = DefaultEase };
      yAnim.Completed += OnCompleted;
      ((TranslateTransform)target.RenderTransform).BeginAnimation(TranslateTransform.YProperty, yAnim);
    }

    if (pending == 0)
      return;

    await using (token.Register(() => tcs.TrySetCanceled()))
      await tcs.Task;
  }

  internal static async Task SlideOutCore(
    UIElement target,
    double endX,
    double endY,
    int durationMs,
    CancellationToken token)
  {
    target.RenderTransform = new TranslateTransform(0, 0);
    target.Opacity = 1;

    bool animateX = endX != 0;
    bool animateY = endY != 0;

    var tcs = new TaskCompletionSource();
    int pending = 0;

    var d = TimeSpan.FromMilliseconds(durationMs);

    void OnCompleted(object? s, EventArgs e)
    {
      if (Interlocked.Decrement(ref pending) == 0)
        tcs.TrySetResult();
    }

    if (animateX)
    {
      Interlocked.Increment(ref pending);
      var xAnim = new DoubleAnimation(0, endX, new Duration(d)) { EasingFunction = DefaultExitEase };
      xAnim.Completed += OnCompleted;
      ((TranslateTransform)target.RenderTransform).BeginAnimation(TranslateTransform.XProperty, xAnim);
    }

    if (animateY)
    {
      Interlocked.Increment(ref pending);
      var yAnim = new DoubleAnimation(0, endY, new Duration(d)) { EasingFunction = DefaultExitEase };
      yAnim.Completed += OnCompleted;
      ((TranslateTransform)target.RenderTransform).BeginAnimation(TranslateTransform.YProperty, yAnim);
    }

    if (pending == 0)
    {
      target.Opacity = 0;
      return;
    }

    await using (token.Register(() => tcs.TrySetCanceled()))
      await tcs.Task;
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
