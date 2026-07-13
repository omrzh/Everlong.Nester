using Everlong.Nester.Presentation;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace NesterApp.Views;

/// <summary>
///   The fallback view for participant types without an authored view —
///   every page type resolves, so navigation never hits a missing view.
/// </summary>
public sealed class PlaceholderView : NesterView
{
  public PlaceholderView(string participantName)
  {
    Add(
      new Label { Text = $"«{participantName}» — placeholder, not implemented", X = Pos.Center(), Y = Pos.Center() },
      new Label { Text = "The type exists; the authored view comes later.", X = Pos.Center(), Y = Pos.Center() + 1 });
  }
}
