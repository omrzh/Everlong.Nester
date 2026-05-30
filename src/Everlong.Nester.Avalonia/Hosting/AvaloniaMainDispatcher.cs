using Avalonia.Threading;
using Everlong.Nester.Threading;

namespace Everlong.Nester.Hosting;

internal sealed class AvaloniaMainDispatcher : IMainDispatcher
{
  public void Post(Action action, DispatchPriority priority = DispatchPriority.Normal)
    => Dispatcher.UIThread.Post(action, Map(priority));

  public Task InvokeAsync(Action action, DispatchPriority priority = DispatchPriority.Normal)
    => Dispatcher.UIThread.InvokeAsync(action, Map(priority)).GetTask();

  public Task InvokeAsync(Func<Task> callback, DispatchPriority priority = DispatchPriority.Normal)
    => Dispatcher.UIThread.InvokeAsync(callback, Map(priority));

  public bool CheckAccess() => Dispatcher.UIThread.CheckAccess();

  private static DispatcherPriority Map(DispatchPriority priority) => priority switch
  {
    DispatchPriority.Low => DispatcherPriority.Background,
    DispatchPriority.High => DispatcherPriority.Render,
    DispatchPriority.Send => DispatcherPriority.Send,
    _ => DispatcherPriority.Normal,
  };
}
