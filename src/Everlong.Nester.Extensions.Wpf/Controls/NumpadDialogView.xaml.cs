using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows;
using Everlong.Nester.Dialog;
using Everlong.Nester.Presentation;

namespace Everlong.Nester.Controls;

/// <summary>
///   A view for displaying the numpad dialog.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public partial class NumpadDialogView : UserControl, ISceneTransition
{
  /// <summary>
  ///   Initializes a new instance of the <see cref="NumpadDialogView" /> class.
  /// </summary>
  public NumpadDialogView()
  {
    Focusable = true;
    InitializeComponent();
    Loaded += OnLoaded;
    AddHandler(PreviewTextInputEvent, new TextCompositionEventHandler(HandlePreviewTextInput), true);
    AddHandler(PreviewKeyDownEvent, new KeyEventHandler(HandlePreviewKeyDown), true);
  }

  private void OnLoaded(object sender, RoutedEventArgs e)
  {
    // Give the dialog root focus so physical keyboard input works immediately.
    Dispatcher.BeginInvoke(() => Keyboard.Focus(this), DispatcherPriority.Input);
  }

  private void HandlePreviewTextInput(object sender, TextCompositionEventArgs e)
  {
    if (e.Handled)
    {
      return;
    }

    e.Handled = TryHandleText(e.Text);
  }

  private void HandlePreviewKeyDown(object sender, KeyEventArgs e)
  {
    if (e.Handled || DataContext is not NumpadDialogSession session)
    {
      return;
    }

    switch (e.Key == Key.System ? e.SystemKey : e.Key)
    {
      case Key.Return:
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
      case Key.C when Keyboard.Modifiers == ModifierKeys.None:
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

  private readonly ISceneTransition _transition = new SlideFromBottomTransition();
  Task ISceneTransition.AnimateEnterAsync(TransitionContext ctx, CancellationToken token)
        => _transition.AnimateEnterAsync(ctx, token);
  Task ISceneTransition.AnimateExitAsync(TransitionContext ctx, CancellationToken token)
      => _transition.AnimateExitAsync(ctx, token);
}
