using Everlong.Nester.ComponentModel;
using Everlong.Nester.Extensions.Properties;

using Everlong.Nester.Routing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Everlong.Nester.Dialog;

/// <summary>
///   A dialog for confirmation (Yes/No, OK/Cancel).
/// </summary>
public partial class ConfirmDialogSession : DialogSessionBase<bool>
{
  [ObservableProperty] public partial string? Message { get; set; }

  /// <summary>
  ///   Gets or sets the heading shown above the message.
  /// </summary>
  [ObservableProperty] public partial string? Title { get; set; }

  /// <summary>
  ///   Gets or sets the confirm button text.
  /// </summary>
  [ObservableProperty]
  public required partial string ConfirmText { get; set; }

  /// <summary>
  ///   Gets or sets the cancel button text.
  /// </summary>
  [ObservableProperty]
  public required partial string CancelText { get; set; }

  /// <summary>
  ///   Gets or sets whether the confirm button is enabled.
  /// </summary>
  [ObservableProperty]
  public required partial bool IsConfirmEnabled { get; set; }

  /// <summary>
  ///   Command to confirm the dialog (returns true).
  /// </summary>
  [RelayCommand]
  private void Confirm()
  {
    Close(true);
  }

  /// <summary>
  ///   Command to cancel the dialog (returns false).
  /// </summary>
  [RelayCommand]
  private void Cancel()
  {
    Close();
  }
}

/// <summary>
///   Options for <see cref="ConfirmDialogSession" />.
/// </summary>
public sealed class ConfirmOptions : DialogOptions
{
  /// <summary>Gets or sets the override for the confirmation button text.</summary>
  public string ConfirmText { get; init; } = Lang.Dialog.Confirm.ConfirmText;

  /// <summary>Gets or sets the override for the cancel button text.</summary>
  public string CancelText { get; init; } = Lang.Dialog.Confirm.CancelText;

  /// <summary>Gets or sets the override for the dialog title.</summary>
  public string Title { get; init; } = Lang.Dialog.Confirm.Title;

  /// <summary>
  ///   Gets or sets the countdown duration (in seconds) before the confirmation button is enabled.
  ///   Set to a value greater than zero to enable the confirmation button only after the countdown finishes.
  ///   A value of <see langword="0" /> disables the countdown and allows immediate confirmation.
  /// </summary>
  public int CountdownSeconds { get; init; }

  internal string GetConfirmTextWithCountdown(int secondsLeft)
  {
    // Only append countdown if secondsLeft > 0
    return secondsLeft > 0
             ? $"{ConfirmText} ({secondsLeft})"
             : ConfirmText;
  }
}

public static partial class DialogSessionExtensions
{
  extension(IRouter router)
  {
    /// <summary>
    ///   Shows a confirmation dialog allowing the user to confirm or cancel an action.
    /// </summary>
    /// <param name="message">The message to display in the dialog.</param>
    /// <param name="sessionOptions">Optional session-level configuration overrides.</param>
    /// <param name="timeProvider">Optional time provider for the countdown display.</param>
    /// <returns>A task that resolves to <see langword="true" /> if confirmed, <see langword="false" /> if canceled.</returns>
    public async Task<bool> ConfirmAsync(string message,
                                         ConfirmOptions? sessionOptions = null,
                                         TimeProvider? timeProvider = null)
    {
      // Ensure sessionOptions is initialized
      sessionOptions ??= new ConfirmOptions();
      timeProvider ??= TimeProvider.System;

      // Create session for the confirmation dialog
      ConfirmDialogSession session = new()
      {
        Message = message,
        Title = sessionOptions.Title,
        ConfirmText = sessionOptions.GetConfirmTextWithCountdown(sessionOptions.CountdownSeconds),
        CancelText = sessionOptions.CancelText,
        IsConfirmEnabled = sessionOptions.CountdownSeconds <= 0 // Disable button initially if countdown > 0
      };

      // Show the dialog — the countdown below runs WHILE it is open (the
      // show task settles when the dialog closes).
      Task<bool> show = router.ShowAsync<bool>(session, sessionOptions.ToDimmer());

      // If no countdown, return the result after the close.
      if (sessionOptions.CountdownSeconds <= 0)
      {
        return await show;
      }

      // Handle countdown logic and update confirm text — concurrently with
      // the dialog being shown; abort as soon as the show task settles
      // (e.g. the user confirmed or cancelled early).
      for (int secondsLeft = sessionOptions.CountdownSeconds; secondsLeft > 0; secondsLeft--)
      {
        // Wait one second, or abort as soon as the dialog closes.
        await Task.WhenAny(DelayAsync(timeProvider, TimeSpan.FromSeconds(1)), show);

        if (show.IsCompleted)
        {
          break;
        }

        // Update confirm text with remaining countdown
        session.ConfirmText = sessionOptions.GetConfirmTextWithCountdown(secondsLeft - 1);

        // Enable confirm button after countdown finishes
        if (secondsLeft == 1)
        {
          session.IsConfirmEnabled = true;
        }
      }

      // The dialog closes once the countdown settles or the user decides.
      return await show;
    }

    static Task DelayAsync(TimeProvider timeProvider, TimeSpan delay)
    {
      TaskCompletionSource tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
      DelayState state = new() { Tcs = tcs };
      state.Timer = timeProvider.CreateTimer(static s =>
      {
        DelayState st = (DelayState)s!;
        st.Tcs.TrySetResult();
        st.Timer?.Dispose();
        st.Timer = null;
      }, state, delay, Timeout.InfiniteTimeSpan);
      return tcs.Task;
    }
  }

  private sealed class DelayState
  {
    public TaskCompletionSource Tcs = null!;
    public ITimer? Timer;
  }
}
