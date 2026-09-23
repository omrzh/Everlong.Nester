using Avalonia.Controls;
using Avalonia.Input;
using Everlong.Nester.Notice;

namespace Everlong.Nester.Presentation;

/// <summary>
///   Base class for item views that handle pointer events.
/// </summary>
public abstract class FeedbackItemViewBase : UserControl
{
  /// <inheritdoc />
  protected override void OnPointerEntered(PointerEventArgs e)
  {
    base.OnPointerEntered(e);
    if (DataContext is IPointerAware aware)
    {
      aware.OnPointerEnter();
    }
  }

  /// <inheritdoc />
  protected override void OnPointerExited(PointerEventArgs e)
  {
    base.OnPointerExited(e);
    if (DataContext is IPointerAware aware)
    {
      aware.OnPointerLeave();
    }
  }
}
