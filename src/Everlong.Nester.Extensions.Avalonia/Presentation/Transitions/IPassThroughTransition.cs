// NOTE: Single-source file — the Extensions WPF project compiles this exact
// file via <Compile Include> in Everlong.Nester.Extensions.Wpf.csproj.
// Edit it here only; never create a WPF-side copy (the two builds would drift).

namespace Everlong.Nester.Presentation;

/// <summary>
///   A director that owns no choreography of its own: it reveals itself and
///   hands the change to the node below it in the body tree.
/// </summary>
/// <remarks>
///   The two <see cref="ISceneTransition"/> members are defaulted to the
///   shipped pass-through.  An implementer is expected to be a
///   <see cref="PControl"/> that also implements <see cref="IBodyHolder"/>; a
///   director that is neither reveals only itself and delegates nothing.
/// </remarks>
public interface IPassThroughTransition : ISceneTransition
{
  /// <inheritdoc />
  Task ISceneTransition.AnimateEnterAsync(TransitionContext context, CancellationToken token)
    => this.PassThroughAsync(context, token);

  /// <inheritdoc />
  Task ISceneTransition.AnimateExitAsync(TransitionContext context, CancellationToken token)
    => this.PassExitAsync(context, token);
}
