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
  ///   loaded).  Cancellation detaches the handler and cancels the returned
  ///   task; the wait releases the cancellation registration when it settles.
  /// </summary>
  public static Task EnsureLoadedAsync(this PControl control, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(control);
    if (control.IsLoaded)
      return Task.CompletedTask;

    var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    CancellationTokenRegistration registration = default;

    void OnLoaded(object? sender, RoutedEventArgs e)
    {
      control.Loaded -= OnLoaded;
      registration.Dispose();
      tcs.TrySetResult();
    }

    control.Loaded += OnLoaded;

    if (cancellationToken.CanBeCanceled)
    {
      registration = cancellationToken.Register(() =>
      {
        control.Loaded -= OnLoaded;
        tcs.TrySetCanceled();
      });
    }

    return tcs.Task;
  }
}
