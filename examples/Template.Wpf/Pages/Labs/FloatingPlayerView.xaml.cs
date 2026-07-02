using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Everlong.Nester.Presentation;

namespace NesterApp.Pages.Labs;

/// <summary>
///   The floating player card — a draggable, closable mock video player shown
///   on the Overlay layer while the video page is departed.
/// </summary>
[ViewFor<FloatingPlayerViewModel>]
public partial class FloatingPlayerView : UserControl
{
  private bool _dragging;
  private Point _dragStart;
  private Thickness _startMargin;

  public FloatingPlayerView()
  {
    InitializeComponent();
  }

  private void OnDragStart(object sender, MouseButtonEventArgs e)
  {
    _dragging = true;
    _dragStart = e.GetPosition(this);
    _startMargin = DraggableCard.Margin;
    DraggableCard.CaptureMouse();
    e.Handled = true;
  }

  private void OnDragMove(object sender, MouseEventArgs e)
  {
    if (!_dragging)
    {
      return;
    }

    Point pos = e.GetPosition(this);
    DraggableCard.Margin = new Thickness(
      _startMargin.Left + pos.X - _dragStart.X,
      _startMargin.Top + pos.Y - _dragStart.Y,
      0, 0);
  }

  private void OnDragEnd(object sender, MouseButtonEventArgs e)
  {
    if (!_dragging)
    {
      return;
    }

    _dragging = false;
    DraggableCard.ReleaseMouseCapture();
  }
}
