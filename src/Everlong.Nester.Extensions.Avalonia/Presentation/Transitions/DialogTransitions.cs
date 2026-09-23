// NOTE: Single-source file — the Extensions WPF project compiles this exact
// file via <Compile Include> in Everlong.Nester.Extensions.Wpf.csproj.
// Edit it here only; never create a WPF-side copy (the two builds would drift).
namespace Everlong.Nester.Presentation;

/// <summary>
///   The dialog default transition: the dialog rises from the bottom
///   (slide + fade in) and returns the same way on exit — it drops back
///   down, the symmetric counterpart of the entrance.  Pairing the enter
///   direction with the same exit direction would make the dialog float
///   away upward instead of returning where it came from.
/// </summary>
/// <param name="durationMs">The flight duration in milliseconds; <see langword="null" /> uses 200 ms.</param>
/// <param name="offset">The slide distance in pixels.</param>
public sealed class SlideFromBottomTransition(int? durationMs = null, double offset = 60) : ISceneTransition
{
  private readonly int _durationMs = durationMs ?? 200;

  /// <inheritdoc />
  public Task AnimateEnterAsync(TransitionContext ctx, CancellationToken token)
  {
    PControl target = ctx.ArrivingChain[^1];   // the dialog content
    ctx.RevealBefore(target);                        // the dimmer frame stays put
    return Task.WhenAll(
      TransitionEffects.SlideInAsync(target, SlideDirection.BottomToTop, offset, _durationMs, token),
      TransitionEffects.FadeInAsync(target, _durationMs, token));
  }

  /// <inheritdoc />
  public Task AnimateExitAsync(TransitionContext ctx, CancellationToken token)
  {
    // Exit returns the same way it came: downward (TopToBottom is the
    // opposite of the BottomToTop entrance).
    PControl target = ctx.DepartingChain[^1];
    return Task.WhenAll(
      TransitionEffects.SlideOutAsync(target, SlideDirection.TopToBottom, offset, _durationMs, token),
      TransitionEffects.FadeOutAsync(target, _durationMs, token));
  }
}

/// <summary>
///   The dialog fade-only transition — symmetric enter/exit fade without
///   any movement.
/// </summary>
public sealed class FadeTransition : ISceneTransition
{
  private readonly int _durationMs;

  /// <summary>
  ///   Initializes a new instance of the <see cref="FadeTransition" /> class.
  /// </summary>
  /// <param name="durationMs">The fade duration in milliseconds; <see langword="null" /> uses 200 ms.</param>
  public FadeTransition(int? durationMs = null)
  {
    _durationMs = durationMs ?? 200;
  }

  /// <inheritdoc />
  public Task AnimateEnterAsync(TransitionContext ctx, CancellationToken token)
  {
    PControl target = ctx.ArrivingChain[^1];
    ctx.RevealBefore(target);
    return TransitionEffects.FadeInAsync(target, _durationMs, token);
  }

  /// <inheritdoc />
  public Task AnimateExitAsync(TransitionContext ctx, CancellationToken token)
    => TransitionEffects.FadeOutAsync(ctx.DepartingChain[^1], _durationMs, token);
}
