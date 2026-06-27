using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia;
using Everlong.Nester.Dialog;
using Everlong.Nester.Presentation;

namespace Everlong.Nester.Controls;

/// <summary>
///   A view for displaying the numpad dialog.
/// </summary>
public partial class NumpadDialogView : UserControl, ISceneTransition
{
  private readonly ISceneTransition _transition = new SlideFromBottomTransition();

  /// <summary>
  ///   Initializes a new instance of the <see cref="NumpadDialogView" /> class.
  /// </summary>
  public NumpadDialogView()
  {
    Focusable = true;
    InitializeComponent();
    AttachedToVisualTree += OnAttachedToVisualTree;
    AddHandler(TextInputEvent, HandleTextInput, RoutingStrategies.Tunnel, true);
    AddHandler(KeyDownEvent, HandleKeyDown, RoutingStrategies.Tunnel, true);
  }

  private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
  {
    // Give the dialog root focus so physical keyboard input works immediately.
    Dispatcher.UIThread.Post(() => Focus(), DispatcherPriority.Input);
  }

  private void HandleTextInput(object? sender, TextInputEventArgs e)
  {
    if (e.Handled)
    {
      return;
    }

    e.Handled = TryHandleText(e.Text);
  }

  private void HandleKeyDown(object? sender, KeyEventArgs e)
  {
    if (e.Handled || DataContext is not NumpadDialogSession session)
    {
      return;
    }

    switch (e.Key)
    {
      case Key.Enter:
        e.Handled = TryExecute(session.SmartConfirmCommand, null);
        break;
      case Key.Back:
        e.Handled = TryExecute(session.BackspaceCommand, null);
        break;
      case Key.Delete:
        e.Handled = TryExecute(session.ClearCommand, null);
        break;
      case Key.Decimal:
        e.Handled = TryAppendKey(session, ".");
        break;
      case Key.Add:
        e.Handled = TryAppendKey(session, "+");
        break;
      case Key.Subtract:
        e.Handled = TryAppendKey(session, "-");
        break;
      case Key.Multiply:
        e.Handled = TryAppendKey(session, "*");
        break;
      case Key.Divide:
        e.Handled = TryAppendKey(session, "/");
        break;
      case Key.C when e.KeyModifiers == KeyModifiers.None:
        e.Handled = TryExecute(session.ClearCommand, null);
        break;
    }
  }

  private bool TryHandleText(string? text)
  {
    if (DataContext is not NumpadDialogSession session || string.IsNullOrEmpty(text))
    {
      return false;
    }

    var handled = false;
    foreach (char value in text)
    {
      handled |= TryHandleCharacter(session, value);
    }

    return handled;
  }

  private static bool TryHandleCharacter(NumpadDialogSession session, char value)
  {
    return value switch
    {
      >= '0' and <= '9' => TryAppendKey(session, value.ToString()),
      '.' or ',' => TryAppendKey(session, "."),
      '+' => TryAppendKey(session, "+"),
      '-' => TryAppendKey(session, "-"),
      '*' or 'x' or 'X' or '×' => TryAppendKey(session, "*"),
      '/' or '÷' => TryAppendKey(session, "/"),
      _ => false
    };
  }

  private static bool TryAppendKey(NumpadDialogSession session, string key)
  {
    return TryExecute(session.AppendKeyCommand, key);
  }

  private static bool TryExecute(ICommand? command, object? parameter)
  {
    if (command?.CanExecute(parameter) != true)
    {
      return false;
    }

    command.Execute(parameter);
    return true;
  }
  Task ISceneTransition.AnimateEnterAsync(TransitionContext ctx, CancellationToken token) => _transition.AnimateEnterAsync(ctx, token);
  Task ISceneTransition.AnimateExitAsync(TransitionContext ctx, CancellationToken token) => _transition.AnimateExitAsync(ctx, token);
}
