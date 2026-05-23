using Everlong.Nester.ComponentModel;
using Everlong.Nester.Extensions.Properties;

using Everlong.Nester.Routing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Everlong.Nester.Dialog;

/// <summary>
///   Represents an option item displayed in option-select dialogs.
/// </summary>
public record DialogOptionItem(object Value)
{
  /// <summary>
  ///   Wraps a value into a <see cref="DialogOptionItem" /> for use in option-select dialogs.
  /// </summary>
  /// <param name="value">The value to wrap.</param>
  /// <returns>A <see cref="DialogOptionItem" /> instance.</returns>
  public static DialogOptionItem Wrap<TOption>(TOption value)
  {
    ArgumentNullException.ThrowIfNull(value);
    return new DialogOptionItem(value);
  }
}

/// <summary>
///   A dialog session for selecting one option from a provided list.
/// </summary>
public partial class OptionSelectDialogSession : DialogSessionBase<object?>
{
  /// <summary>Creates a session with default placeholder options.</summary>
  public OptionSelectDialogSession() : this(
  [
    new DialogOptionItem("Option 1"),
    new DialogOptionItem("Option 2"),
    new DialogOptionItem("Option 3")
  ])
  {
  }

  /// <summary>Creates a session with the supplied options.</summary>
  /// <param name="items">The options to display.</param>
  public OptionSelectDialogSession(IEnumerable<DialogOptionItem> items)
  {
    Items = items.ToList();
  }

  [ObservableProperty] public partial string? Prompt { get; set; }

  /// <summary>
  ///   Gets or sets the heading shown above the prompt.
  /// </summary>
  [ObservableProperty] public partial string? Title { get; set; }

  /// <summary>
  ///   Gets the options.
  /// </summary>
  public IReadOnlyList<DialogOptionItem> Items { get; }

  [RelayCommand]
  private void Select(DialogOptionItem? option)
  {
    Close(option?.Value);
  }
}

/// <summary>
///   Configuration options for <see cref="OptionSelectDialogSession" /> extension methods.
/// </summary>
public sealed class SelectOptions : DialogOptions
{
}

public static partial class DialogSessionExtensions
{
  extension(IRouter router)
  {
    /// <summary>
    ///   Shows an option-select dialog and returns the selected value.
    /// </summary>
    /// <typeparam name="TOption">The option value type.</typeparam>
    /// <param name="options">The source options.</param>
    /// <param name="message">The message content displayed in the dialog.</param>
    /// <param name="title">The title displayed in the dialog.</param>
    /// <param name="sessionOptions">Optional session-level configuration overrides.</param>
    /// <returns>The selected value, or default when canceled.</returns>
    public async Task<TOption?> SelectFromOptionsAsync<TOption>(IEnumerable<TOption> options,
                                                                string message,
                                                                string? title = null,
                                                                SelectOptions? sessionOptions = null)
    {
      sessionOptions ??= new SelectOptions();
      IEnumerable<DialogOptionItem> items = options.Select(DialogOptionItem.Wrap);
      OptionSelectDialogSession session = new(items)
      {
        Title = title ?? Lang.Dialog.OptionSelect.Title,
        Prompt = message
      };

      TOption? selected = await router.ShowAsync<TOption>(session, sessionOptions.ToDimmer());
      return selected != null ? selected : default;
    }
  }
}
