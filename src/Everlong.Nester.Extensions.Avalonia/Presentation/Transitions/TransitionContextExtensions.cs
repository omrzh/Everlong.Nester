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
    ///   Slides the departing head out.  Requires a departing head
    ///   (exit or dismiss).  The arriving head is not touched — the
    ///   framework reveals it after the director returns.
    /// </summary>
    public Task ExitWithSlideAsync(SlideDirection to,
                                   double offset,
                                   int durationMs,
                                   CancellationToken token)
      => TransitionEffects.SlideOutAsync(ctx.Departing!, to, offset, durationMs, token);

    /// <summary>Fades the departing head out — mirror of <see cref="ExitWithSlideAsync"/>.</summary>
    public Task ExitWithFadeAsync(int durationMs, CancellationToken token)
      => TransitionEffects.FadeOutAsync(ctx.Departing!, durationMs, token);

    /// <summary>Zooms the departing head out — mirror of <see cref="ExitWithSlideAsync"/>.</summary>
    public Task ExitWithZoomAsync(double toScale,
                                  int durationMs,
                                  CancellationToken token)
      => TransitionEffects.ZoomOutAsync(ctx.Departing!, toScale, durationMs, token);

    /// <summary>
    ///   The arriving scene takes over with a slide: hides the departing
    ///   head, reveals the arriving head, and slides the arriving head in.
    ///   Requires an arriving head.
    /// </summary>
    public Task EnterWithSlideAsync(SlideDirection from,
                                    double offset,
                                    int durationMs,
                                    CancellationToken token)
    {
      ctx.HideDeparting();
      ctx.ShowArriving();
      return TransitionEffects.SlideInAsync(ctx.Arriving!, from, offset, durationMs, token);
    }

    /// <summary>
    ///   The arriving scene takes over with a fade — same three steps as
    ///   <see cref="EnterWithSlideAsync"/>.
    /// </summary>
    public Task EnterWithFadeAsync(int durationMs, CancellationToken token)
    {
      ctx.HideDeparting();
      ctx.ShowArriving();
      return TransitionEffects.FadeInAsync(ctx.Arriving!, durationMs, token);
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
      return TransitionEffects.ZoomInAsync(ctx.Arriving!, fromScale, durationMs, token);
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
    ///   The common pass-through pattern for an outer scene director: dips the
    ///   arriving node below this view, reveals this view and delegates the
    ///   enter transition to that node's director.  When there is no inner
    ///   director, completes immediately.
    /// </summary>
    public Task PassThroughAsync(TransitionContext context, CancellationToken token)
    {
      if (transition is not PControl view)
        return Task.CompletedTask;

      PControl? inner = TransitionTree.ArrivingBelow(view);

      // The lever moves one step down the arriving path: the node below is
      // dipped so the frame can show over an empty body while the inner scene
      // plays.  A refresh re-engaged that node in place, so it stays visible.
      if (inner is not null && context.Kind is not TransitionKind.Refresh)
      {
        inner.Opacity = 0;
        inner.IsHitTestVisible = false;
      }

      // The frame takes no part in the inner animation: reveal it fully now.
      view.Opacity = 1;
      view.IsHitTestVisible = true;

      return inner is ISceneTransition director
               ? director.AnimateEnterAsync(context with { Arriving = inner }, token)
               : Task.CompletedTask;
    }

    /// <summary>
    ///   The exit counterpart of <see cref="PassThroughAsync"/>: delegates
    ///   the exit transition to the departing node below this view without
    ///   touching visibility.  When there is no inner director, completes
    ///   immediately.
    /// </summary>
    public Task PassExitAsync(TransitionContext context, CancellationToken token)
    {
      if (transition is not PControl view)
        return Task.CompletedTask;

      PControl? inner = TransitionTree.DepartingBelow(view);
      return inner is ISceneTransition director
               ? director.AnimateExitAsync(context with { Departing = inner }, token)
               : Task.CompletedTask;
    }
  }
}
