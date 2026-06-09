using System.Windows;
using System.Windows.Threading;
using Everlong.Nester.Threading;

namespace Everlong.Nester.Hosting;

internal sealed class WpfMainDispatcher : IMainDispatcher
{
  public void Post(Action action, DispatchPriority priority = DispatchPriority.Normal)
  {
    Application.Current?.Dispatcher.BeginInvoke(action, Map(priority));
  }

  public Task InvokeAsync(Action action, DispatchPriority priority = DispatchPriority.Normal)
    => Application.Current?.Dispatcher.InvokeAsync(action, Map(priority)).Task
       ?? Task.CompletedTask;

  public Task InvokeAsync(Func<Task> callback, DispatchPriority priority = DispatchPriority.Normal)
    => Application.Current?.Dispatcher.InvokeAsync(callback, Map(priority)).Task.Unwrap()
       ?? Task.CompletedTask;

  public bool CheckAccess()
    => Application.Current?.Dispatcher.CheckAccess() ?? false;

  private static DispatcherPriority Map(DispatchPriority priority) => priority switch
  {
    DispatchPriority.Low => DispatcherPriority.Background,
    DispatchPriority.High => DispatcherPriority.DataBind,
    DispatchPriority.Send => DispatcherPriority.Send,
    _ => DispatcherPriority.Normal,
  };
}
