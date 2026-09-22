using System.Windows.Controls;
using System.Windows.Input;
using Everlong.Nester.Notice;

namespace Everlong.Nester.Presentation;

/// <summary>
///   Base class for item views that handle timer control on mouse hover.
/// </summary>
public abstract class FeedbackItemViewBase : UserControl
{
  /// <inheritdoc />
  protected override void OnMouseEnter(MouseEventArgs e)
  {
    base.OnMouseEnter(e);
    if (DataContext is IPointerAware aware)
    {
      aware.OnPointerEnter();
    }
  }

  /// <inheritdoc />
  protected override void OnMouseLeave(MouseEventArgs e)
  {
    base.OnMouseLeave(e);
    if (DataContext is IPointerAware aware)
    {
      aware.OnPointerLeave();
    }
  }
}
