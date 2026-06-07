using System.Diagnostics;
using System.Windows.Threading;

namespace Everlong.Nester.Helpers;

/// <summary>Helpers over <see cref="DispatcherTimer" />.</summary>
public class DispatcherTimerHelper
{
  /// <summary>Runs <paramref name="action" /> once on the dispatcher thread, at <paramref name="priority" />, after <paramref name="interval" />.</summary>
  public static void RunOnce(Action action, TimeSpan interval, DispatcherPriority priority)
  {
    interval = (interval != TimeSpan.Zero) ? interval : TimeSpan.FromTicks(1);

    var timer = new DispatcherTimer(priority) { Interval = interval };

    timer.Tick += (_, _) =>
    {
      timer.Stop();
      try
      {
        action();
      }
      catch (Exception exception)
      {
        Debug.WriteLine(exception);
        throw;
      }
    };

    timer.Start();
  }
}
