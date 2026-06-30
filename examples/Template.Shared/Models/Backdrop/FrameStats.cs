namespace NesterApp.Backdrop;

/// <summary>Rolling frame statistics over a fixed sample window.</summary>
public sealed class FrameStats
{
  private const int Window = 120;

  private readonly double[] _samples = new double[Window];
  private int _next;
  private int _count;

  /// <summary>The newest frame duration, in milliseconds.</summary>
  public double FrameMs { get; private set; }

  /// <summary>The mean rate over the recorded window, in frames per second.</summary>
  public double Fps { get; private set; }

  /// <summary>The recorded sample count, capped by the window size.</summary>
  public int Count => _count;

  /// <summary>Raised after every recorded frame.</summary>
  public event EventHandler? Sampled;

  /// <summary>Records one frame duration and refreshes <see cref="FrameMs" /> and <see cref="Fps" />.</summary>
  public void Report(double frameMs)
  {
    FrameMs = frameMs;
    _samples[_next] = frameMs;
    _next = (_next + 1) % Window;
    if (_count < Window)
      _count++;

    double total = 0;
    for (int i = 0; i < _count; i++)
      total += _samples[i];

    double mean = total / _count;
    Fps = mean > 0 ? 1000d / mean : 0d;
    Sampled?.Invoke(this, EventArgs.Empty);
  }

  /// <summary>Reads a sample by age — 0 is the newest.</summary>
  public double this[int age]
  {
    get
    {
      if (age < 0 || age >= _count)
        return 0;

      return _samples[(_next - 1 - age + Window * 2) % Window];
    }
  }
}
