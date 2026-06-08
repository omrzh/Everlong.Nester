using System.Windows;

namespace Everlong.Nester.Presentation;

public partial record TransitionContext
{
  /// <summary>Captures <paramref name="target" />'s bounds in the flying canvas' coordinates.</summary>
  public PRect CaptureRelativeRect(PVisual target)
  {
    Size size = target.RenderSize;
    return new PRect(target.TranslatePoint(new Point(0, 0), FlyingCanvas), size);
  }
}
