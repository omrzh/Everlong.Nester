using System.Windows.Media.Animation;

namespace Everlong.Nester.Presentation;

internal sealed class SplineEasingFunction : IEasingFunction
{
  private readonly double _x1, _y1, _x2, _y2;

  public SplineEasingFunction(double x1, double y1, double x2, double y2)
  {
    _x1 = x1;
    _y1 = y1;
    _x2 = x2;
    _y2 = y2;
  }

  public double Ease(double normalizedTime)
  {
    double t = normalizedTime;
    if (t <= 0)
      return 0;
    if (t >= 1)
      return 1;

    double cX = 3 * _x1;
    double bX = 3 * (_x2 - _x1) - cX;
    double aX = 1 - cX - bX;

    double cY = 3 * _y1;
    double bY = 3 * (_y2 - _y1) - cY;
    double aY = 1 - cY - bY;

    double x = t;
    for (int i = 0; i < 8; i++)
    {
      double f = ((aX * x + bX) * x + cX) * x - t;
      double df = (3 * aX * x + 2 * bX) * x + cX;
      if (Math.Abs(df) < 1e-6)
        break;
      x -= f / df;
    }

    if (x < 0)
      x = 0;
    if (x > 1)
      x = 1;

    return ((aY * x + bY) * x + cY) * x;
  }
}
