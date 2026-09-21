using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Everlong.Nester.Helpers;
using Xunit;

namespace Everlong.Nester.Tests.Helpers;

/// <summary>
///   The reveal's layout wait (<see cref="UIDispatcher.WaitForLayoutAsync" />):
///   one dispatcher pass is the whole wait for a view mounted into an attached
///   stack, and the Loaded event is the fallback for one that is not.
/// </summary>
public sealed class WaitForLayoutTests
{
  [AvaloniaFact]
  public async Task WaitForLayoutAsync_DetachedView_FallsBackToTheLoadedEvent()
  {
    var control = new ContentControl { Content = new Border { Width = 10, Height = 10 } };
    var window = new Window { Width = 200, Height = 200 };

    Task wait = UIDispatcher.WaitForLayoutAsync(control, CancellationToken.None);

    // Let the single pass run: the control is in no tree, so the wait has to
    // reach its Loaded-event fallback.
    await Dispatcher.UIThread.InvokeAsync(static () => { }, DispatcherPriority.Background);
    Assert.False(wait.IsCompleted);

    window.Content = control;
    window.Show();
    try
    {
      await wait;   // the event path completes when the control loads
      Assert.True(control.IsLoaded);
    }
    finally
    {
      window.Close();
    }
  }
}
