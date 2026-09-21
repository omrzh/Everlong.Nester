using Avalonia;

namespace Everlong.Nester.Presentation;

partial record TransitionContext
{
  /// <summary>Captures <paramref name="target" />'s bounds in the flying canvas' coordinates.</summary>
  /// <remarks>With no plane there is no frame to express the position in, and the rectangle is anchored at the origin.</remarks>
  public PRect CaptureRelativeRect(PVisual target)
  {
    var size = target.Bounds.Size;
    var origin = FlyingCanvas is { } frame ? target.TranslatePoint(default, frame) ?? default : default;
    return new PRect(origin, size);
  }
}
