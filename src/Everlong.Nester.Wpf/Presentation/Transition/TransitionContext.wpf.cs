using System.Windows;

namespace Everlong.Nester.Presentation;

public partial record TransitionContext
{
  /// <summary>Captures <paramref name="target" />'s bounds in the flying canvas' coordinates.</summary>
  /// <remarks>With no plane there is no frame to express the position in, and the rectangle is anchored at the origin.</remarks>
  public PRect CaptureRelativeRect(PVisual target)
  {
    Size size = target.RenderSize;
    Point origin = FlyingCanvas is { } frame ? target.TranslatePoint(new Point(0, 0), frame) : default;
    return new PRect(origin, size);
  }
}
