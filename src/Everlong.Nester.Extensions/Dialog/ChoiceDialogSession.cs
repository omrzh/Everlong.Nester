using Everlong.Nester.ComponentModel;
using Everlong.Nester.Extensions.Properties;

using Everlong.Nester.Routing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Everlong.Nester.Dialog;

/// <summary>
///   Represents the result of a three-action choice dialog.
/// </summary>
public enum DialogChoiceResult
{
  /// <summary>
  ///   No result was produced.
  /// </summary>
  None,

  /// <summary>
  ///   The primary action was selected.
  /// </summary>
  Primary,

  /// <summary>
  ///   The secondary action was selected.
  /// </summary>
  Secondary,

  /// <summary>
  ///   The cancel action was selected.
  /// </summary>
  Cancel
}

/// <summary>
///   Dialog session that provides primary, secondary, and cancel actions.
/// </summary>
public partial class ChoiceDialogSession : DialogSessionBase<DialogChoiceResult>
{
  [ObservableProperty] public partial string? Message { get; set; }

  /// <summary>
  ///   Gets or sets the heading shown above the message.
  /// </summary>
  [ObservableProperty] public partial string? Title { get; set; }

  /// <summary>
  ///   Gets or sets the text displayed on the primary action button.
  /// </summary>
  [ObservableProperty] public required partial string PrimaryText { get; set; }

  /// <summary>
  ///   Gets or sets the text displayed on the secondary action button.
  /// </summary>
  [ObservableProperty] public required partial string SecondaryText { get; set; }

  /// <summary>
  ///   Gets or sets the text displayed on the cancel action button.
  /// </summary>
  [ObservableProperty] public required partial string CancelText { get; set; }

  /// <summary>
  ///   Gets or sets whether the primary action button is enabled.
  /// </summary>
  [ObservableProperty] public partial bool IsPrimaryEnabled { get; set; } = true;

  /// <summary>
  ///   Gets or sets whether the secondary action button is enabled.
  /// </summary>
  [ObservableProperty] public partial bool IsSecondaryEnabled { get; set; } = true;

  /// <summary>
  ///   Gets or sets whether the cancel action button is enabled.
  /// </summary>
  [ObservableProperty] public partial bool IsCancelEnabled { get; set; } = true;

  [RelayCommand]
  private void Primary()
  {
    Close(DialogChoiceResult.Primary);
  }

  [RelayCommand]
  private void Secondary()
  {
    Close(DialogChoiceResult.Secondary);
  }

  [RelayCommand]
  private void Cancel()
  {
    Close(DialogChoiceResult.Cancel);
  }
}

/// <summary>
///   Configuration options for <see cref="ChoiceDialogSession" /> extension methods.
/// </summary>
public sealed class ChoiceOptions : DialogOptions
{
  /// <summary>Gets or sets the override for the primary action button text.</summary>
  public string PrimaryText { get; init; } = Lang.Dialog.Choice.PrimaryText;

  /// <summary>Gets or sets the override for the secondary action button text.</summary>
  public string SecondaryText { get; init; } = Lang.Dialog.Choice.SecondaryText;

  /// <summary>Gets or sets the override for the cancel button text.</summary>
  public string CancelText { get; init; } = Lang.Dialog.Choice.CancelText;

  /// <summary>
  ///   Gets or sets the initial enabled state of the primary button.
  /// </summary>
  public bool IsPrimaryEnabled { get; init; } = true;

  /// <summary>
  ///   Gets or sets the initial enabled state of the secondary button.
  /// </summary>
  public bool IsSecondaryEnabled { get; init; } = true;

  /// <summary>
  ///   Gets or sets the initial enabled state of the cancel button.
  /// </summary>
  public bool IsCancelEnabled { get; init; } = true;
}

public static partial class DialogSessionExtensions
{
  extension(IRouter router)
  {
    /// <summary>
    ///   Shows a choice dialog with three action buttons (Primary, Secondary, Cancel) and returns the selected result.
    /// </summary>
    /// <param name="message">The message content displayed in the dialog.</param>
    /// <param name="title">The title displayed in the dialog header.</param>
    /// <param name="sessionOptions">
    ///   Optional session-level configuration overrides for button labels and initial enabled
    ///   states.
    /// </param>
    /// <returns>A task that resolves to the selected <see cref="DialogChoiceResult" /> value.</returns>
    public Task<DialogChoiceResult> ChooseAsync(string message,
                                                string? title = null,
                                                ChoiceOptions? sessionOptions = null)
    {
      sessionOptions ??= new ChoiceOptions();
      ChoiceDialogSession session = new()
      {
        Title = title ?? Lang.Dialog.Choice.Title,
        Message = message,
        PrimaryText = sessionOptions.PrimaryText,
        SecondaryText = sessionOptions.SecondaryText,
        CancelText = sessionOptions.CancelText,
        IsPrimaryEnabled = sessionOptions.IsPrimaryEnabled,
        IsSecondaryEnabled = sessionOptions.IsSecondaryEnabled,
        IsCancelEnabled = sessionOptions.IsCancelEnabled
      };

      return router.ShowAsync<DialogChoiceResult>(session, sessionOptions.ToDimmer());
    }
  }
}
