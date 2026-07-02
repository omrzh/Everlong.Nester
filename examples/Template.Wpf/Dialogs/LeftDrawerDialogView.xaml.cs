using System.Windows.Controls;

using Everlong.Nester.Presentation;
namespace NesterApp.Dialogs;

/// <summary>
///   The drawer's view — its own animation director (<see cref="ISceneTransition" />).
///   The direction is NOT guessed by the framework: the view slides left
///   with its own default animation.
/// </summary>
[ViewFor<LeftDrawerDialogSession>]
public partial class LeftDrawerDialogView : UserControl, ISceneTransition
{
  public LeftDrawerDialogView()
  {
    InitializeComponent();
  }

  public Task AnimateEnterAsync(TransitionContext ctx, CancellationToken token)
  {
    ctx.RevealBefore(this);
    return this.SlideInAsync(SlideDirection.LeftToRight, ActualWidth, 250, token);
  }

  public Task AnimateExitAsync(TransitionContext ctx, CancellationToken token)
  {
    // The drawer exits itself — RightToLeft is the symmetric return of the
    // LeftToRight entrance (movement direction, not "from" direction).
    return this.SlideOutAsync(SlideDirection.RightToLeft, ActualWidth, 250, token);
  }
}
