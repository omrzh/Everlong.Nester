using Terminal.Gui.ViewBase;

namespace Everlong.Nester.Presentation;

/// <summary>
///   The lease surface of the Terminal.Gui platform — a full-screen
///   container that holds one tenant's body.
/// </summary>
public sealed class TuiLayer : View
{
  private View? _content;

  /// <summary>
  ///   Initializes a new instance of the <see cref="TuiLayer" /> class.
  /// </summary>
  public TuiLayer()
  {
    Width = Dim.Fill();
    Height = Dim.Fill();
  }

  /// <summary>The tenant's displayed body — one child at a time.</summary>
  internal View? Content
  {
    get => _content;
    set
    {
      if (ReferenceEquals(_content, value))
        return;
      if (_content is not null)
        Remove(_content);
      _content = value;
      if (value is not null)
        Add(value);
    }
  }
}
