using Terminal.Gui.ViewBase;

namespace Everlong.Nester.Presentation;

/// <summary>The desired surface size of a floating view (a dialog window).</summary>
public readonly record struct TerminalSurfaceSize(int Width, int Height);

/// <summary>
///   The view-model carrier of the Terminal.Gui surface — the base class of
///   views the locator resolves for chain participants.  The routing stages
///   attach the resolved participant as <see cref="DataContext" /> after the
///   view is built.
/// </summary>
public abstract class NesterView : View
{
  /// <summary>
  ///   Whether the routing stages stretch this view to fill its mount body
  ///   — <see langword="true" /> for pages and layouts; dialogs and other
  ///   free-sized surfaces set it to <see langword="false" /> and size
  ///   themselves.
  /// </summary>
  public bool StretchToBody { get; protected set; } = true;

  /// <summary>
  ///   The floating surface's content size — set together with
  ///   <see cref="StretchToBody" /> = <see langword="false" />.  The staging
  ///   reveal sizes the whole floating chain (the lease surface and the
  ///   layouts above this view) to this content plus margins, so the
  ///   surface clears only its own window rect.
  /// </summary>
  public TerminalSurfaceSize? FloatingSize { get; protected set; }

  /// <summary>The resolved participant this view presents — assigned once by the routing stages.</summary>
  public object? DataContext { get; private set; }

  /// <summary>Attaches the participant — <see cref="OnDataContextChanged" /> fires once per distinct value.</summary>
  public void AttachDataContext(object? dataContext)
  {
    if (ReferenceEquals(DataContext, dataContext))
      return;
    DataContext = dataContext;
    OnDataContextChanged();
  }

  /// <summary>Wires the view to the attached participant — runs once per engagement.</summary>
  protected virtual void OnDataContextChanged()
  {
  }
}
