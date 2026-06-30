using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Everlong.Nester.Presentation;

namespace NesterApp.Dialogs;

/// <summary>
///   The palette's view.  It owns no state: the arrow keys and Enter are turned
///   into the session's commands here, exactly like a button click would be —
///   the filter text arrives from the model and leaves through the model.
/// </summary>
/// <remarks>
///   It also owns its own entrance and exit (<see cref="ISceneTransition" />).
///   The framework hands the change over and asks nothing about direction, so
///   "a palette drops in from the top edge" is the view's decision — and its
///   return leg is the same decision played backwards.
/// </remarks>
[ViewFor<WorkspacePaletteSession>]
public partial class WorkspacePaletteView : UserControl, ISceneTransition
{
  /// <summary>A palette arrives on a keystroke: short enough to read as immediate.</summary>
  private const int DurationMs = 140;

  /// <summary>Enough travel to say which edge it belongs to; not enough to be a flight.</summary>
  private const double Offset = 24;

  public WorkspacePaletteView()
  {
    InitializeComponent();
    AttachedToVisualTree += OnAttachedToVisualTree;
    AddHandler(KeyDownEvent, HandleKeyDown, RoutingStrategies.Tunnel, true);
  }

  /// <inheritdoc />
  public Task AnimateEnterAsync(TransitionContext context, CancellationToken token)
  {
    // The dimmer is the frame this view does not animate — reveal it up front,
    // then let the palette drop onto it.
    context.RevealBefore(this);
    return Task.WhenAll(
      this.SlideInAsync(SlideDirection.TopToBottom, Offset, DurationMs, token),
      this.FadeInAsync(DurationMs, token));
  }

  /// <inheritdoc />
  public Task AnimateExitAsync(TransitionContext context, CancellationToken token)
  {
    // Back where it came from: BottomToTop is the symmetric return of the
    // TopToBottom entrance (movement direction, not "from" direction).
    return Task.WhenAll(
      this.SlideOutAsync(SlideDirection.BottomToTop, Offset, DurationMs, token),
      this.FadeOutAsync(DurationMs, token));
  }

  private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
  {
    // Type immediately: the palette's first frame is a text cursor.
    Dispatcher.UIThread.Post(() => FilterBox.Focus(), DispatcherPriority.Input);
  }

  private void HandleKeyDown(object? sender, KeyEventArgs e)
  {
    if (e.Handled || DataContext is not WorkspacePaletteSession session)
      return;

    // The session moves the selection; the view only says which key happened.
    // A handled key is one the palette claimed — the dimmer keeps the rest.
    e.Handled = e.Key switch
    {
      Key.Enter => Execute(session.OpenCommand),
      Key.Down => Execute(session.NextCommand),
      Key.Up => Execute(session.PreviousCommand),
      _ => false,
    };
  }

  private static bool Execute(ICommand? command)
  {
    if (command?.CanExecute(null) != true)
      return false;

    command.Execute(null);
    return true;
  }
}
