using Everlong.Nester.ComponentModel;
using System.Globalization;
using Everlong.Nester.Extensions.Properties;

using Everlong.Nester.Routing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Everlong.Nester.Dialog;

/// <summary>
///   A dialog session for date and time selection with per-component text
///   inputs and min/max range constraints.
/// </summary>
public partial class DateTimePickerDialogSession : DialogSessionBase<DateTime?>
{
  /// <summary>Creates a session initialized to the current local date and time.</summary>
  public DateTimePickerDialogSession() { }

  /// <summary>
  ///   Gets or sets the heading shown above the inputs.
  /// </summary>
  [ObservableProperty] public partial string? Title { get; set; }

  /// <summary>
  ///   Gets or sets the guidance text shown below the heading.
  /// </summary>
  [ObservableProperty] public partial string? Message { get; set; }

  /// <summary>
  ///   Gets or sets the year as text input.
  /// </summary>
  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(CanConfirm))]
  [NotifyPropertyChangedFor(nameof(PreviewText))]
  [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
  public partial string YearText { get; set; } = DateTime.Now.Year.ToString(CultureInfo.InvariantCulture);

  /// <summary>
  ///   Gets or sets the month as text input (1-12).
  /// </summary>
  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(CanConfirm))]
  [NotifyPropertyChangedFor(nameof(PreviewText))]
  [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
  public partial string MonthText { get; set; } = DateTime.Now.ToString("MM", CultureInfo.InvariantCulture);

  /// <summary>
  ///   Gets or sets the day as text input.
  /// </summary>
  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(CanConfirm))]
  [NotifyPropertyChangedFor(nameof(PreviewText))]
  [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
  public partial string DayText { get; set; } = DateTime.Now.ToString("dd", CultureInfo.InvariantCulture);

  /// <summary>
  ///   Gets or sets the hour as text input (0-23).
  /// </summary>
  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(CanConfirm))]
  [NotifyPropertyChangedFor(nameof(PreviewText))]
  [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
  public partial string HourText { get; set; } = DateTime.Now.ToString("HH", CultureInfo.InvariantCulture);

  /// <summary>
  ///   Gets or sets the minute as text input (0-59).
  /// </summary>
  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(CanConfirm))]
  [NotifyPropertyChangedFor(nameof(PreviewText))]
  [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
  public partial string MinuteText { get; set; } = DateTime.Now.ToString("mm", CultureInfo.InvariantCulture);

  /// <summary>
  ///   Gets or sets the second as text input (0-59).
  /// </summary>
  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(CanConfirm))]
  [NotifyPropertyChangedFor(nameof(PreviewText))]
  [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
  public partial string SecondText { get; set; } = DateTime.Now.ToString("ss", CultureInfo.InvariantCulture);

  /// <summary>
  ///   Gets or sets the minimum selectable date/time, or <see langword="null" /> for no minimum.
  /// </summary>
  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(CanConfirm))]
  [NotifyPropertyChangedFor(nameof(RangeHint))]
  [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
  public partial DateTime? MinValue { get; set; }

  /// <summary>
  ///   Gets or sets the maximum selectable date/time, or <see langword="null" /> for no maximum.
  /// </summary>
  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(CanConfirm))]
  [NotifyPropertyChangedFor(nameof(RangeHint))]
  [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
  public partial DateTime? MaxValue { get; set; }

  /// <summary>
  ///   Gets or sets the text for the confirm button.
  /// </summary>
  [ObservableProperty] public required partial string ConfirmText { get; set; }

  /// <summary>
  ///   Gets or sets the text for the "set to now" button.
  /// </summary>
  [ObservableProperty] public required partial string NowText { get; set; }

  /// <summary>Gets the label of the year input.</summary>
  public string YearLabel => Lang.Dialog.DateTimePicker.YearLabel;

  /// <summary>Gets the label of the month input.</summary>
  public string MonthLabel => Lang.Dialog.DateTimePicker.MonthLabel;

  /// <summary>Gets the label of the day input.</summary>
  public string DayLabel => Lang.Dialog.DateTimePicker.DayLabel;

  /// <summary>Gets the label of the hour input.</summary>
  public string HourLabel => Lang.Dialog.DateTimePicker.HourLabel;

  /// <summary>Gets the label of the minute input.</summary>
  public string MinuteLabel => Lang.Dialog.DateTimePicker.MinuteLabel;

  /// <summary>Gets the label of the second input.</summary>
  public string SecondLabel => Lang.Dialog.DateTimePicker.SecondLabel;

  /// <summary>
  ///   Gets a value indicating whether the current date/time input is valid and can be confirmed.
  /// </summary>
  public bool CanConfirm => TryBuildDateTime(out _);

  /// <summary>
  ///   Gets a preview string of the current date/time selection in "yyyy-MM-dd HH:mm:ss" format, or an error message if
  ///   invalid.
  /// </summary>
  public string PreviewText => TryBuildDateTime(out DateTime dt)
                                 ? dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                                 : Lang.Dialog.DateTimePicker.InvalidInput;

  /// <summary>
  ///   Gets a hint string showing the valid date/time range constraints.
  /// </summary>
  public string RangeHint
  {
    get
    {
      if (MinValue is null && MaxValue is null)
      {
        return Lang.Dialog.DateTimePicker.UnlimitedRange;
      }

      string min = MinValue?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? "-∞";
      string max = MaxValue?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? "+∞";
      return Lang.Dialog.DateTimePicker.FormatRangeHint(min, max);
    }
  }

  [RelayCommand]
  private void SetNow()
  {
    Apply(DateTime.Now);
  }

  [RelayCommand(CanExecute = nameof(CanConfirm))]
  private void Confirm()
  {
    if (!TryBuildDateTime(out DateTime dateTime))
    {
      return;
    }

    Close(dateTime);
  }

  /// <summary>
  ///   Initializes the date/time picker with the specified date/time value.
  /// </summary>
  /// <param name="value">The date/time to set in the picker.</param>
  public void Apply(DateTime value)
  {
    YearText = value.ToString("yyyy", CultureInfo.InvariantCulture);
    MonthText = value.ToString("MM", CultureInfo.InvariantCulture);
    DayText = value.ToString("dd", CultureInfo.InvariantCulture);
    HourText = value.ToString("HH", CultureInfo.InvariantCulture);
    MinuteText = value.ToString("mm", CultureInfo.InvariantCulture);
    SecondText = value.ToString("ss", CultureInfo.InvariantCulture);
  }

  private bool TryBuildDateTime(out DateTime value)
  {
    value = default;
    if (!int.TryParse(YearText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int year) ||
        !int.TryParse(MonthText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int month) ||
        !int.TryParse(DayText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int day) ||
        !int.TryParse(HourText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int hour) ||
        !int.TryParse(MinuteText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int minute) ||
        !int.TryParse(SecondText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int second))
    {
      return false;
    }

    if (year < 1 || year > 9999 || month is < 1 or > 12 || hour is < 0 or > 23 || minute is < 0 or > 59 ||
        second is < 0 or > 59)
    {
      return false;
    }

    if (day < 1 || day > DateTime.DaysInMonth(year, month))
    {
      return false;
    }

    value = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Local);
    if (MinValue is not null && value < MinValue.Value)
    {
      return false;
    }

    if (MaxValue is not null && value > MaxValue.Value)
    {
      return false;
    }

    return true;
  }
}

/// <summary>
///   Configuration options for <see cref="DateTimePickerDialogSession" /> extension methods.
/// </summary>
public sealed class DateTimePickerOptions : DialogOptions
{
  /// <summary>Gets or sets the initial date/time value shown in the picker, or <see langword="null" /> for current time.</summary>
  public DateTime InitialValue { get; init; } = DateTime.Now;

  /// <summary>Gets or sets the minimum selectable date/time, or <see langword="null" /> for no minimum.</summary>
  public DateTime? MinValue { get; set; }

  /// <summary>Gets or sets the maximum selectable date/time, or <see langword="null" /> for no maximum.</summary>
  public DateTime? MaxValue { get; set; }

  /// <summary>Gets or sets the override for the confirm button text.</summary>
  public string ConfirmText { get; init; } = Lang.Dialog.DateTimePicker.ConfirmText;

  /// <summary>Gets or sets the override for the "set to now" button text.</summary>
  public string NowText { get; init; } = Lang.Dialog.DateTimePicker.NowText;
}

public static partial class DialogSessionExtensions
{
  extension(IRouter router)
  {
    /// <summary>
    ///   Shows a date/time picker dialog allowing the user to select a date and time.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The dialog message.</param>
    /// <param name="sessionOptions">Optional session-level overrides for initial value, range constraints, and button labels.</param>
    /// <returns>The selected date/time, or <see langword="null" /> if the dialog was canceled.</returns>
    public Task<DateTime?> PickDateTimeAsync(string? title = null,
                                             string? message = null,
                                             DateTimePickerOptions? sessionOptions = null)
    {
      sessionOptions ??= new DateTimePickerOptions();
      DateTime start = sessionOptions.InitialValue;
      DateTimePickerDialogSession session = new()
      {
        Title = title ?? Lang.Dialog.DateTimePicker.Title,
        Message = message,
        ConfirmText = sessionOptions.ConfirmText,
        NowText = sessionOptions.NowText,
        MinValue = sessionOptions.MinValue,
        MaxValue = sessionOptions.MaxValue
      };

      session.Apply(start);
      return router.ShowAsync<DateTime?>(session, sessionOptions.ToDimmer());
    }
  }
}
