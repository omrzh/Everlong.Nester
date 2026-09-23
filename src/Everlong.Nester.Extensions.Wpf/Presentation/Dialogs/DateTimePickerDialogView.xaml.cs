using System.ComponentModel;
using System.Windows.Controls;

namespace Everlong.Nester.Presentation;

/// <summary>
///   A view for displaying the date and time picker dialog.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public partial class DateTimePickerDialogView : UserControl, ISceneTransition
{
  private readonly ISceneTransition _transition = new SlideFromBottomTransition();

  /// <summary>
  ///   Initializes a new instance of the <see cref="DateTimePickerDialogView" /> class.
  /// </summary>
  public DateTimePickerDialogView()
  {
    InitializeComponent();
  }

  Task ISceneTransition.AnimateEnterAsync(TransitionContext ctx, CancellationToken token) => _transition.AnimateEnterAsync(ctx, token);
  Task ISceneTransition.AnimateExitAsync(TransitionContext ctx, CancellationToken token) => _transition.AnimateExitAsync(ctx, token);
}
