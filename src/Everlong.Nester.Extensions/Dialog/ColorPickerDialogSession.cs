using Everlong.Nester.ComponentModel;
using Everlong.Nester.Primitives;
using Everlong.Nester.Extensions.Properties;

using Everlong.Nester.Routing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Everlong.Nester.Dialog;

/// <summary>
///   Options for <see cref="ColorPickerDialogSession" />.
/// </summary>
public sealed class ColorPickerOptions : DialogOptions
{
  /// <summary>
  ///   Gets or sets the override text for the confirm button.
  /// </summary>
  public string ConfirmText { get; init; } = Lang.Dialog.ColorPicker.ConfirmText;

  /// <summary>
  ///   Gets or sets the override text for the reset button.
  /// </summary>
  public string ResetText { get; init; } = Lang.Dialog.ColorPicker.ResetText;

  /// <summary>
  ///   Gets or sets the initial color. Defaults to opaque black (<c>#FF000000</c>).
  /// </summary>
  public ValueColor InitialColor { get; init; } = new(255, 0, 0, 0);
}

/// <summary>
///   A dialog session for selecting an ARGB color.
/// </summary>
public partial class ColorPickerDialogSession : DialogSessionBase<ValueColor?>
{
  /// <summary>Creates a session with default color channel values.</summary>
  public ColorPickerDialogSession() { }

  /// <summary>
  ///   Gets or sets the heading shown above the channels.
  /// </summary>
  [ObservableProperty] public partial string? Title { get; set; }

  /// <summary>
  ///   Gets or sets the guidance text shown below the heading.
  /// </summary>
  [ObservableProperty] public partial string? Message { get; set; }

  /// <summary>
  ///   Gets or sets the text for the confirm button.
  /// </summary>
  [ObservableProperty] public required partial string ConfirmText { get; set; }

  /// <summary>
  ///   Gets or sets the text for the reset button.
  /// </summary>
  [ObservableProperty] public required partial string ResetText { get; set; }

  /// <summary>
  ///   Gets or sets the alpha channel value (0-255).
  /// </summary>
  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(PreviewHex))]
  [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
  public partial double Alpha { get; set; } = 255d;

  /// <summary>
  ///   Gets or sets the red channel value (0-255).
  /// </summary>
  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(PreviewHex))]
  [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
  public partial double Red { get; set; }

  /// <summary>
  ///   Gets or sets the green channel value (0-255).
  /// </summary>
  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(PreviewHex))]
  [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
  public partial double Green { get; set; }

  /// <summary>
  ///   Gets or sets the blue channel value (0-255).
  /// </summary>
  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(PreviewHex))]
  [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
  public partial double Blue { get; set; }

  /// <summary>
  ///   Gets the hexadecimal representation of the currently selected color (ARGB format).
  /// </summary>
  public string PreviewHex =>
    new ValueColor(ToChannel(Alpha), ToChannel(Red), ToChannel(Green), ToChannel(Blue)).ToHexArgb();

  [RelayCommand(CanExecute = nameof(CanConfirm))]
  private void Confirm()
  {
    ValueColor color = new(ToChannel(Alpha), ToChannel(Red), ToChannel(Green), ToChannel(Blue));
    Close(color);
  }

  [RelayCommand]
  private void Reset()
  {
    Alpha = 255d;
    Red = 0d;
    Green = 0d;
    Blue = 0d;
  }

  private bool CanConfirm()
  {
    return IsValidChannel(Alpha) && IsValidChannel(Red) && IsValidChannel(Green) && IsValidChannel(Blue);
  }

  private static bool IsValidChannel(double value)
  {
    return value >= 0d && value <= 255d;
  }

  private static byte ToChannel(double value)
  {
    double rounded = Math.Round(value);
    if (rounded < 0d)
    {
      return 0;
    }

    if (rounded > 255d)
    {
      return 255;
    }

    return (byte)rounded;
  }
}

public static partial class DialogSessionExtensions
{
  extension(IRouter router)
  {
    /// <summary>
    ///   Shows a color picker dialog allowing the user to select an ARGB color.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The dialog message.</param>
    /// <param name="sessionOptions">Optional session-level overrides for initial color and button texts.</param>
    /// <returns>The selected color, or <see langword="null" /> if the dialog was canceled.</returns>
    public Task<ValueColor?> PickColorAsync(string? title = null,
                                            string? message = null,
                                            ColorPickerOptions? sessionOptions = null)
    {
      sessionOptions ??= new ColorPickerOptions();
      ValueColor start = sessionOptions.InitialColor;
      ColorPickerDialogSession session = new()
      {
        Title = title ?? Lang.Dialog.ColorPicker.Title,
        Message = message,
        ConfirmText = sessionOptions.ConfirmText,
        ResetText = sessionOptions.ResetText,
        Alpha = start.A,
        Red = start.R,
        Green = start.G,
        Blue = start.B
      };

      return router.ShowAsync<ValueColor?>(session, sessionOptions.ToDimmer());
    }
  }
}
