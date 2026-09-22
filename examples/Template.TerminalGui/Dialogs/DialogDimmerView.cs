using Everlong.Nester.Presentation;

namespace NesterApp.Dialogs;

/// <summary>
///   The default dimmer's view — the full-screen backdrop layer that hosts
///   the dialog session above the navigation surface.
/// </summary>
public sealed class DialogDimmerView : NesterView, ILayoutControl
{
  private readonly TuiLayoutBody _body;

  public DialogDimmerView()
  {
    _body = new TuiLayoutBody();
    Add(_body);
  }

  /// <inheritdoc />
  public ILayoutBody GetLayoutBody() => _body;
}
