using Everlong.Nester.Presentation;

namespace NesterApp.Dialogs;

/// <summary>
///   The default dimmer's view — the full-screen backdrop layer that hosts
///   the dialog session above the navigation surface.
/// </summary>
public sealed class DialogDimmerView : NesterView, IBodyHolder
{
  private readonly TuiBodyPanel _body;

  public DialogDimmerView()
  {
    _body = new TuiBodyPanel();
    Add(_body);
  }

  /// <inheritdoc />
  public IBodyPanel GetBodyPanel() => _body;
}
