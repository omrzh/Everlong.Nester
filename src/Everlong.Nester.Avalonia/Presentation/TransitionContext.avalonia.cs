using Avalonia;

namespace Everlong.Nester.Presentation;

partial record TransitionContext
{
  /// <summary>Captures <paramref name="target" />'s bounds in the flying canvas' coordinates.</summary>
  public PRect CaptureRelativeRect(PVisual target)
  {
    var size = target.Bounds.Size;
    return new PRect(target.TranslatePoint(default, FlyingCanvas) ?? default, size);
  }
}
