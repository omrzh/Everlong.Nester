// NOTE: Single-source file — the Extensions WPF project compiles this exact
// file via <Compile Include> in Everlong.Nester.Extensions.Wpf.csproj.
// Edit it here only; never create a WPF-side copy (the two builds would drift).

namespace Everlong.Nester.Presentation;

/// <summary>
///   Animation entry points.  Enter helpers animate the view they are
///   invoked on; exit helpers on a <see cref="TransitionContext"/> animate
///   the departing head; exit helpers on a view animate that view.
/// </summary>
public static class TransitionContextExtensions
{

  extension(PControl target)
  {
    /// <summary>Slides the view in from <paramref name="from"/>.</summary>
    public Task SlideInAsync(SlideDirection from,
                             double offset,
                             int durationMs,
                             CancellationToken token)
      => TransitionEffects.SlideInAsync(target, from, offset, durationMs, token);

    /// <summary>Fades the view in.</summary>
    public Task FadeInAsync(int durationMs, CancellationToken token)
      => TransitionEffects.FadeInAsync(target, durationMs, token);

    /// <summary>Zooms the view in from <paramref name="fromScale"/>.</summary>
    public Task ZoomInAsync(double fromScale, int durationMs, CancellationToken token)
      => TransitionEffects.ZoomInAsync(target, fromScale, durationMs, token);
  }

  extension(TransitionContext ctx)
  {
    /// <summary>
    ///   Slides the departing head out.  Requires a departing chain
    ///   (exit or dismiss).  The arriving chain is not touched — the
    ///   framework reveals it after the director returns.
    /// </summary>
    public Task ExitWithSlideAsync(SlideDirection to,
                                   double offset,
                                   int durationMs,
                                   CancellationToken token)
      => TransitionEffects.SlideOutAsync(ctx.DepartingHead!, to, offset, durationMs, token);

    /// <summary>Fades the departing head out — mirror of <see cref="ExitWithSlideAsync"/>.</summary>
    public Task ExitWithFadeAsync(int durationMs, CancellationToken token)
      => TransitionEffects.FadeOutAsync(ctx.DepartingHead!, durationMs, token);

    /// <summary>Zooms the departing head out — mirror of <see cref="ExitWithSlideAsync"/>.</summary>
    public Task ExitWithZoomAsync(double toScale,
                                  int durationMs,
                                  CancellationToken token)
      => TransitionEffects.ZoomOutAsync(ctx.DepartingHead!, toScale, durationMs, token);

    /// <summary>
    ///   The arriving scene takes over with a slide: hides the departing
    ///   chain, reveals the arriving chain, and slides the arriving head in.
    ///   Requires an arriving chain.
    /// </summary>
    public Task EnterWithSlideAsync(SlideDirection from,
                                    double offset,
                                    int durationMs,
                                    CancellationToken token)
    {
      ctx.HideDeparting();
      ctx.ShowArriving();
      return TransitionEffects.SlideInAsync(ctx.ArrivingHead!, from, offset, durationMs, token);
    }

    /// <summary>
    ///   The arriving scene takes over with a fade — same three steps as
    ///   <see cref="EnterWithSlideAsync"/>.
    /// </summary>
    public Task EnterWithFadeAsync(int durationMs, CancellationToken token)
    {
      ctx.HideDeparting();
      ctx.ShowArriving();
      return TransitionEffects.FadeInAsync(ctx.ArrivingHead!, durationMs, token);
    }

    /// <summary>
    ///   The arriving scene takes over with a zoom — same three steps as
    ///   <see cref="EnterWithSlideAsync"/>.
    /// </summary>
    public Task EnterWithZoomAsync(double fromScale,
                                   int durationMs,
                                   CancellationToken token)
    {
      ctx.HideDeparting();
      ctx.ShowArriving();
      return TransitionEffects.ZoomInAsync(ctx.ArrivingHead!, fromScale, durationMs, token);
    }
  }

  extension(PControl target)
  {
    /// <summary>Slides the view out towards <paramref name="to"/>.</summary>
    public Task SlideOutAsync(SlideDirection to,
                              double offset,
                              int durationMs,
                              CancellationToken token)
      => TransitionEffects.SlideOutAsync(target, to, offset, durationMs, token);

    /// <summary>Fades the view out.</summary>
    public Task FadeOutAsync(int durationMs, CancellationToken token)
      => TransitionEffects.FadeOutAsync(target, durationMs, token);

    /// <summary>Zooms the view out to <paramref name="toScale"/>.</summary>
    public Task ZoomOutAsync(double toScale, int durationMs, CancellationToken token)
      => TransitionEffects.ZoomOutAsync(target, toScale, durationMs, token);
  }

  // ── Pass-through: an outer director that only reveals itself and
  //     delegates to the next inner director ──
  extension(ISceneTransition transition)
  {
    /// <summary>
    ///   The common pass-through pattern for an outer scene director: reveals
    ///   this view (<c>Opacity</c> = 1, hit-test on) and delegates the enter
    ///   transition to the next inner <see cref="ISceneTransition"/>.  When
    ///   there is no inner director, completes immediately.
    /// </summary>
    public Task PassThroughAsync(TransitionContext context, CancellationToken token)
    {
      if (transition is PControl view && context.NextDirectorAfter(view) is { } director)
      {
        // The frame takes no part in the inner animation: reveal it fully now.
        view.Opacity = 1;
        view.IsHitTestVisible = true;
        return director.AnimateEnterAsync(context.ScopedFrom(view), token);
      }

      return Task.CompletedTask;
    }

    /// <summary>
    ///   The exit counterpart of <see cref="PassThroughAsync"/>: delegates
    ///   the exit transition to the next inner director without touching
    ///   visibility.  When there is no inner director, completes immediately.
    /// </summary>
    public Task PassExitAsync(TransitionContext context, CancellationToken token)
    {
      if (transition is PControl view && context.NextDirectorAfter(view) is { } director)
        return director.AnimateExitAsync(context.ScopedFrom(view), token);

      return Task.CompletedTask;
    }
  }
}
