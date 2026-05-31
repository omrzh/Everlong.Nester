using Avalonia;

namespace Everlong.Nester.Presentation;

partial record TransitionContext
{
  /// <summary>Captures <paramref name="target" />'s bounds in the flying canvas' coordinates.</summary>
  public Rect CaptureRelativeRect(Visual target)
  {
    var size = target.Bounds.Size;
    return new Rect(target.TranslatePoint(default, FlyingCanvas) ?? default, size);
  }
}
