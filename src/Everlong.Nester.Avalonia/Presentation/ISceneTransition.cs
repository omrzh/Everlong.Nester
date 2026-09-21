// NOTE: Single-source file — the WPF project compiles this exact file via
// <Compile Include> in Everlong.Nester.Wpf.csproj.  Edit it here only;
// never create a WPF-side copy (the two builds would drift).
namespace Everlong.Nester.Presentation;

/// <summary>
///   A scene-change director — the view the framework invokes to
///   choreograph a change.
/// </summary>
/// <remarks>
///   The framework invokes the first implementing view of the moving side,
///   outermost first; a close hands the departing side instead.  An entering
///   view is mounted, laid out and invisible when the method runs, and a
///   departing view is visible.  A re-engaged view — the same view, its
///   transfer a <see cref="TransitionKind.Refresh" /> — is laid out and
///   visible: it never left the presentation.  The framework restores final
///   visibility after the method returns.
///
///   The framework gives the dispatcher one pass to lay the entering chain out,
///   waiting on the innermost entering view — the last one the mount cascade
///   reaches — and, when one pass was not enough, on that view's <c>Loaded</c>
///   event; both only while the stage has joined the visual tree.  An entering
///   view whose stack never does reaches the method unmeasured, and a director
///   that reads geometry must tolerate it.
/// </remarks>
public interface ISceneTransition
{
  /// <summary>
  ///   Called on the arriving director for a forward change or refresh.
  ///   An entering view is invisible and a re-engaged view is visible; the
  ///   departing chain is visible.
  /// </summary>
  Task AnimateEnterAsync(TransitionContext context, CancellationToken token);

  /// <summary>
  ///   Called on the departing director for a back change or layer
  ///   dismissal.  The departing chain is visible; the arriving chain is
  ///   invisible and is not revealed by the framework until the method
  ///   returns.
  /// </summary>
  Task AnimateExitAsync(TransitionContext context, CancellationToken token);
}

