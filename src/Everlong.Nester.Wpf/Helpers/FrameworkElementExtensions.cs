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
  ///   loaded).  Cancellation detaches the handler and cancels the returned
  ///   task; the wait releases the cancellation registration when it settles.
  /// </summary>
  public static Task EnsureLoadedAsync(this FrameworkElement element, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(element);
    if (element.IsLoaded)
      return Task.CompletedTask;

    var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    CancellationTokenRegistration registration = default;

    void OnLoaded(object sender, RoutedEventArgs e)
    {
      element.Loaded -= OnLoaded;
      registration.Dispose();
      tcs.TrySetResult();
    }

    element.Loaded += OnLoaded;

    if (cancellationToken.CanBeCanceled)
    {
      registration = cancellationToken.Register(() =>
      {
        element.Loaded -= OnLoaded;
        tcs.TrySetCanceled();
      });
    }

    return tcs.Task;
  }
}
