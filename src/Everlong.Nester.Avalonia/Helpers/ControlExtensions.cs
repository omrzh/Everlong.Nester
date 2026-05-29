using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Everlong.Nester.Helpers;

/// <summary>
///   Avalonia <see cref="Control" /> helpers — the Loaded-event wait
///   (wire a control into the visual tree only after it is ready, present
///   after the first rendered frame, …).
/// </summary>
public static class ControlExtensions
{
  /// <summary>
  ///   Waits until the control is LOADED (returns immediately when already
  ///   loaded).  Cancellation detaches the handler (no leak) and cancels the
  ///   returned task.
  /// </summary>
  public static Task EnsureLoadedAsync(this PlatformControl control, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(control);
    if (control.IsLoaded)
      return Task.CompletedTask;

    var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    EventHandler<RoutedEventArgs>? handler = null;
    handler = (_, _) =>
    {
      tcs.TrySetResult();
      control.Loaded -= handler;
    };
    control.Loaded += handler;

    if (cancellationToken.CanBeCanceled)
    {
      cancellationToken.Register(() =>
      {
        control.Loaded -= handler;
        tcs.TrySetCanceled();
      });
    }

    return tcs.Task;
  }
}
