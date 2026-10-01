// NOTE: Single-source file — the WPF project compiles this exact file via
// <Compile Include> in Everlong.Nester.Wpf.csproj.  Edit it here only;
// never create a WPF-side copy (the two builds would drift).
namespace Everlong.Nester.Presentation;

/// <summary>
///   A scene-change director — the view the framework invokes to
///   choreograph a change.
/// </summary>
/// <remarks>
///   The framework invokes the moving side's first-difference node when it
///   implements this interface; a view below it is never a candidate, and a
///   first difference that does not implement it cancels the Transition phase
///   — the change settles to its final visibility in the landing turn.  A
///   close hands the departing side instead.  Only the arriving head is dipped
///   to invisible before the method runs; a view below it stays as the mount
///   left it, and a director that needs the whole side hidden moves the lever
///   itself as it delegates.  A re-engaged view — the same view, its transfer
///   a <see cref="TransitionKind.Refresh" /> — is laid out and visible: it
///   never left the presentation.  The framework restores final visibility
///   after the method returns.
///
///   Before a director is invoked, the framework gives the dispatcher one
///   pass to lay the arriving side out, waiting on the innermost entering
///   view — the last one the mount cascade reaches — and, when one pass was
///   not enough, on that view's <c>Loaded</c> event; both only while the stage
///   has joined the visual tree.  A convergence with no director to invoke
///   takes no pass, and its entering views mount and show in the turn they
///   were prepared.  An entering view whose stack never joins the visual tree
///   reaches the method unmeasured, and a director that reads geometry must
///   tolerate it.
/// </remarks>
public interface ISceneTransition
{
  /// <summary>
  ///   Called on the arriving director for a forward change or refresh.
  ///   The arriving head is invisible; a re-engaged view is visible, and the
  ///   departing head stays visible.
  /// </summary>
  Task AnimateEnterAsync(TransitionContext context, CancellationToken token);

  /// <summary>
  ///   Called on the departing director for a back change or layer
  ///   dismissal.  The departing head is visible; the arriving head is
  ///   invisible and is not revealed by the framework until the method
  ///   returns.
  /// </summary>
  Task AnimateExitAsync(TransitionContext context, CancellationToken token);
}

