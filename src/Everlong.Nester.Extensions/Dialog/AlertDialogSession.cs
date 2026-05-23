using Everlong.Nester.ComponentModel;
using Everlong.Nester.Extensions.Properties;
using Everlong.Nester.Routing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Everlong.Nester.Dialog;

/// <summary>
///   A dialog that displays a message and a confirmation button.
/// </summary>
public partial class AlertDialogSession : DialogSessionBase<bool>
{
  [ObservableProperty] public required partial string? Message { get; set; }

  /// <summary>
  ///   Gets or sets the heading shown above the message.
  /// </summary>
  [ObservableProperty] public partial string? Title { get; set; }

  /// <summary>
  ///   Gets or sets the button text.
  /// </summary>
  [ObservableProperty] public required partial string ButtonText { get; set; }

  /// <summary>
  ///   Command to close the dialog (returns true).
  /// </summary>
  [RelayCommand]
  private void Confirm() => Close(true);
}

public static partial class DialogSessionExtensions
{
  // An alert is acknowledged, not dismissed: the dimmer refuses external dismissal.
  private static DefaultDimmerModel MandatoryDimmer() => new() { LightDismiss = false };

  extension(IRouter router)
  {
    /// <summary>
    ///   Shows an alert dialog with a message and confirmation button.
    /// </summary>
    /// <param name="message">The message to display in the alert dialog.</param>
    /// <param name="title">The title of the alert dialog.</param>
    /// <param name="buttonText">The text to display on the confirmation button.</param>
    /// <returns>A task that completes when the user confirms the alert.</returns>
    public Task<bool> AlertAsync(string message,
                                 string? title,
                                 string? buttonText = null)
    {
      AlertDialogSession alertSession = new()
      {
        Message = message,
        Title = title ?? Lang.Dialog.Alert.Title,
        ButtonText = buttonText ?? Lang.Dialog.Alert.ButtonText
      };
      return router.ShowAsync<bool>(alertSession, MandatoryDimmer());
    }

    /// <summary>
    ///   Shows an alert dialog with a message and confirmation button.
    /// </summary>
    /// <param name="message">The message to display in the alert dialog.</param>
    /// <returns>A task that completes when the user confirms the alert.</returns>
    public Task<bool> AlertAsync(string message)
    {
      AlertDialogSession alertSession = new()
      {
        Message = message,
        Title = Lang.Dialog.Alert.Title,
        ButtonText = Lang.Dialog.Alert.ButtonText
      };
      return router.ShowAsync<bool>(alertSession, MandatoryDimmer());
    }
  }
}
