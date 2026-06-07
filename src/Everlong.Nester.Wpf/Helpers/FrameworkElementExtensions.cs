using System.Windows;

namespace Everlong.Nester.Helpers;

/// <summary>
///   WPF <see cref="FrameworkElement" /> helpers — the Loaded-event wait
///   (wire a control into the visual tree only after it is ready, present
///   after the first rendered frame, …).
/// </summary>
public static class FrameworkElementExtensions
{
  /// <summary>
  ///   Waits until the element is LOADED (returns immediately when already
  ///   loaded).  Cancellation detaches the handler (no leak) and cancels the
  ///   returned task.
  /// </summary>
  public static Task EnsureLoadedAsync(this FrameworkElement element, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(element);
    if (element.IsLoaded)
      return Task.CompletedTask;

    var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    RoutedEventHandler handler = null!;
    handler = (_, _) =>
    {
      tcs.TrySetResult();
      element.Loaded -= handler;
    };
    element.Loaded += handler;

    if (cancellationToken.CanBeCanceled)
    {
      cancellationToken.Register(() =>
      {
        element.Loaded -= handler;
        tcs.TrySetCanceled();
      });
    }

    return tcs.Task;
  }
}
