using Everlong.Nester.ComponentModel;
using System.Globalization;
using Everlong.Nester.Extensions.Properties;
using Microsoft.Extensions.DependencyInjection;

using Everlong.Nester.Routing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Everlong.DI;
using Everlong.Nester.Notice;

namespace Everlong.Nester.Dialog;

/// <summary>
///   A dialog session for numeric input: decimal values, range validation,
///   and expression evaluation.
/// </summary>
public partial class NumpadDialogSession : DialogSessionBase<decimal?>, IInjectable
{
  internal INoticeService ToastService { get; private set; } = NullNoticeService.Instance;

  /// <summary>
  ///   Gets or sets the optional unit suffix displayed alongside the value.
  /// </summary>
  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(DisplayValue))]
  [NotifyPropertyChangedFor(nameof(RangeHint))]
  public partial string? Unit { get; set; }

  /// <summary>
  ///   Gets or sets the confirmed numeric value.
  /// </summary>
  [ObservableProperty]
  public partial decimal Value { get; set; }

  /// <summary>
  ///   Gets or sets the text displayed in the input field, which can contain numbers, operators, or decimal points.
  /// </summary>
  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(CanConfirm))]
  [NotifyPropertyChangedFor(nameof(DisplayValue))]
  [NotifyPropertyChangedFor(nameof(ResultHint))]
  [NotifyPropertyChangedFor(nameof(SmartButtonText))]
  [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
  [NotifyCanExecuteChangedFor(nameof(EvaluateCommand))]
  [NotifyCanExecuteChangedFor(nameof(AppendKeyCommand))]
  [NotifyCanExecuteChangedFor(nameof(SmartConfirmCommand))]
  public partial string InputText { get; set; } = "0";

  /// <summary>
  ///   Gets or sets the heading shown above the input.
  /// </summary>
  [ObservableProperty] public partial string? Title { get; set; }

  /// <summary>
  ///   Gets or sets the guidance text shown below the heading.
  /// </summary>
  [ObservableProperty] public partial string? Message { get; set; }

  /// <summary>
  ///   Gets or sets the text displayed on the confirm button.
  /// </summary>
  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(SmartButtonText))]
  public required partial string ConfirmText { get; set; }

  /// <summary>
  ///   Gets or sets the minimum allowed value for numeric input.
  /// </summary>
  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(RangeHint))]
  public partial decimal Min { get; set; }

  /// <summary>
  ///   Gets or sets the maximum allowed value for numeric input.
  /// </summary>
  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(RangeHint))]
  public partial decimal Max { get; set; } = 100000m;

  /// <summary>
  ///   Gets or sets the maximum number of decimal places allowed in a single numeric segment.
  /// </summary>
  [ObservableProperty]
  [NotifyCanExecuteChangedFor(nameof(AppendKeyCommand))]
  public partial int MaxDecimalPlaces { get; set; } = 3;

  /// <summary>
  ///   Gets or sets the maximum number of integer digits allowed in a single numeric segment.
  /// </summary>
  [ObservableProperty]
  public partial int MaxIntegerDigits { get; set; } = 8;

  /// <summary>
  ///   Gets or sets whether decimal point input is allowed.
  /// </summary>
  [ObservableProperty]
  [NotifyCanExecuteChangedFor(nameof(AppendKeyCommand))]
  public partial bool AllowDecimal { get; set; } = true;

  /// <summary>
  ///   Gets a value indicating whether the current input can be confirmed.
  /// </summary>
  public bool CanConfirm => TryEvaluateInput(out _);

  /// <summary>
  ///   Gets the formatted allowed range text.
  /// </summary>
  public string RangeHint => Lang.Dialog.Numpad.FormatRangeHint(
    FormatValue(Min), FormatValue(Max), FormatUnitSuffix());

  /// <summary>
  ///   Gets the formatted display value with optional unit suffix.
  /// </summary>
  public string DisplayValue => string.IsNullOrWhiteSpace(Unit) ? InputText : $"{InputText} {Unit}";

  /// <summary>
  ///   Gets the formatted result hint showing the evaluated expression or error indicator.
  /// </summary>
  public string ResultHint => TryEvaluateInput(out decimal parsed)
                                ? Lang.Dialog.Numpad.FormatResultHint(FormatValue(parsed),
                                                                      FormatUnitSuffix())
                                : Lang.Dialog.Numpad.ResultHintInvalid;

  /// <summary>
  ///   Gets the text for the smart confirm button.
  /// </summary>
  public string SmartButtonText => IsExpression() ? "=" : ConfirmText;

  /// <summary>Gets the tooltip of the clear key.</summary>
  public string ClearToolTip => Lang.Dialog.Numpad.ClearToolTip;

  /// <summary>Gets the tooltip of the backspace key.</summary>
  public string BackspaceToolTip => Lang.Dialog.Numpad.BackspaceToolTip;

  /// <inheritdoc />
  public virtual void Inject(IServiceProvider services)
  {
    ToastService = services.GetRequiredService<INoticeService>();
  }


  [RelayCommand(CanExecute = nameof(CanAppendKey))]
  private void AppendKey(string key)
  {
    if (string.IsNullOrWhiteSpace(key))
    {
      return;
    }

    if (key.Length != 1)
    {
      return;
    }

    char token = key[0];
    if (char.IsDigit(token))
    {
      InputText = BuildNextDigitValue(token);
      return;
    }

    if (token == '.')
    {
      if (!CanAppendDecimalPoint())
      {
        return;
      }

      InputText = BuildNextDecimalPointValue();
      return;
    }

    if (!IsOperator(token))
    {
      return;
    }

    InputText = BuildNextOperatorValue(token);
  }

  [RelayCommand]
  private void Clear()
  {
    InputText = "0";
  }

  [RelayCommand]
  private void Backspace()
  {
    if (InputText.Length <= 1)
    {
      InputText = "0";
      return;
    }

    InputText = InputText[..^1];
    if (InputText == "-" || InputText == "-0" || InputText == string.Empty)
    {
      InputText = "0";
    }
  }

  [RelayCommand(CanExecute = nameof(CanConfirm))]
  private void Evaluate()
  {
    if (!TryEvaluateInput(out decimal parsed))
    {
      return;
    }

    InputText = FormatValue(parsed);
  }

  [RelayCommand(CanExecute = nameof(CanConfirm))]
  private void Confirm()
  {
    if (!TryEvaluateInput(out decimal parsed))
    {
      ToastService.Toast(Lang.Dialog.Numpad.InvalidExpressionError, ToastLevel.Error);
      return;
    }

    if (parsed < Min || parsed > Max)
    {
      ToastService.Toast(Lang.Dialog.Numpad.FormatOutOfRangeWarning(RangeHint), ToastLevel.Warning);
      return;
    }

    Value = parsed;
    Close(Value);
  }

  [RelayCommand(CanExecute = nameof(CanConfirm))]
  private void SmartConfirm()
  {
    if (IsExpression())
    {
      Evaluate();
    }
    else
    {
      Confirm();
    }
  }

  partial void OnValueChanged(decimal value)
  {
    InputText = FormatValue(value);
  }

  private bool TryEvaluateInput(out decimal value)
  {
    return TryEvaluateExpression(GetEvaluatableText(), out value);
  }

  private bool CanAppendKey(string key)
  {
    if (string.IsNullOrWhiteSpace(key))
    {
      return false;
    }

    if (key.Length != 1)
    {
      return false;
    }

    char token = key[0];
    if (token == '.')
    {
      return CanAppendDecimalPoint();
    }

    if (char.IsDigit(token))
    {
      return BuildNextDigitValue(token) != InputText;
    }

    return IsOperator(token);
  }

  private string BuildNextDigitValue(char digit)
  {
    string text = GetEditableText();
    int segmentStart = FindCurrentSegmentStart(text);
    string segment = text[segmentStart..];

    switch (segment)
    {
      case "" or "-":
        return $"{text}{digit}";
      case "0":
        return $"{text[..segmentStart]}{digit}";
      case "-0":
        return $"{text[..segmentStart]}-{digit}";
    }

    int dotIndex = segment.IndexOf('.');
    if (dotIndex >= 0)
    {
      string fraction = segment[(dotIndex + 1)..];
      if (fraction.Length >= MaxDecimalPlaces)
      {
        return InputText;
      }

      return $"{text}{digit}";
    }

    string integerPart = dotIndex >= 0 ? segment[..dotIndex] : segment;
    string unsignedIntegerPart = integerPart.TrimStart('-');
    if (unsignedIntegerPart.Length >= MaxIntegerDigits)
    {
      return InputText;
    }

    return $"{text}{digit}";
  }

  private string BuildNextOperatorValue(char op)
  {
    string text = GetEditableText();
    char tail = text[^1];
    if (IsOperator(tail))
    {
      if (op == '-' && tail is '+' or '-' or '*' or '/')
      {
        bool allowUnary = text.Length == 1 || !IsOperator(text[^2]);
        if (allowUnary)
        {
          return $"{text}{op}";
        }
      }

      return $"{text[..^1]}{op}";
    }

    return tail == '.' ? text : $"{text}{op}";
  }

  private bool CanAppendDecimalPoint()
  {
    if (!AllowDecimal || MaxDecimalPlaces <= 0)
    {
      return false;
    }

    string text = GetEditableText();
    int segmentStart = FindCurrentSegmentStart(text);
    string segment = text[segmentStart..];
    if (segment.Contains('.'))
    {
      return false;
    }

    return !segment.EndsWith('.');
  }

  private string BuildNextDecimalPointValue()
  {
    string text = GetEditableText();
    int segmentStart = FindCurrentSegmentStart(text);
    string segment = text[segmentStart..];
    if (segment == string.Empty || segment == "-")
    {
      return $"{text}0.";
    }

    return $"{text}.";
  }

  private static int FindCurrentSegmentStart(string text)
  {
    for (int i = text.Length - 1; i >= 0; i--)
    {
      if (!IsOperator(text[i]))
      {
        continue;
      }

      if (i == 0)
      {
        return 0;
      }

      if (IsOperator(text[i - 1]))
      {
        continue;
      }

      return i + 1;
    }

    return 0;
  }

  private string FormatUnitSuffix()
  {
    return string.IsNullOrWhiteSpace(Unit) ? string.Empty : $" {Unit}";
  }

  private string FormatValue(decimal value)
  {
    string format = MaxDecimalPlaces <= 0 ? "0" : $"0.{new string('#', MaxDecimalPlaces)}";
    return value.ToString(format, CultureInfo.InvariantCulture);
  }

  private string GetEditableText()
  {
    string text = string.IsNullOrWhiteSpace(InputText) ? "0" : InputText.Trim();
    return text == string.Empty ? "0" : text;
  }

  private string GetEvaluatableText()
  {
    return GetEditableText();
  }

  private bool IsExpression()
  {
    string text = GetEditableText();
    for (int i = 0; i < text.Length; i++)
    {
      if (IsOperator(text[i]))
      {
        if (text[i] == '-' && i == 0)
        {
          continue;
        }

        return true;
      }
    }

    return false;
  }

  private static bool IsOperator(char value)
  {
    return value is '+' or '-' or '*' or '/';
  }

  private static int GetPrecedence(char op)
  {
    return op is '*' or '/' ? 2 : 1;
  }

  private static bool TryEvaluateExpression(string expression, out decimal result)
  {
    result = 0m;
    Stack<decimal> values = new();
    Stack<char> ops = new();
    int i = 0;
    bool expectOperand = true;

    while (i < expression.Length)
    {
      char c = expression[i];
      if (char.IsWhiteSpace(c))
      {
        i++;
        continue;
      }

      if (expectOperand)
      {
        decimal sign = 1m;
        if (c is '+' or '-')
        {
          sign = c == '-' ? -1m : 1m;
          i++;
          if (i >= expression.Length)
          {
            return false;
          }

          c = expression[i];
        }

        if (!char.IsDigit(c) && c != '.')
        {
          return false;
        }

        int start = i;
        int dotCount = 0;
        while (i < expression.Length && (char.IsDigit(expression[i]) || expression[i] == '.'))
        {
          if (expression[i] == '.')
          {
            dotCount++;
            if (dotCount > 1)
            {
              return false;
            }
          }

          i++;
        }

        string token = expression[start..i];
        if (!decimal.TryParse(token, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed))
        {
          return false;
        }

        values.Push(sign * parsed);
        expectOperand = false;
        continue;
      }

      if (!IsOperator(c))
      {
        return false;
      }

      while (ops.Count > 0 && GetPrecedence(ops.Peek()) >= GetPrecedence(c))
      {
        if (!TryApplyTopOperator(values, ops))
        {
          return false;
        }
      }

      ops.Push(c);
      expectOperand = true;
      i++;
    }

    if (expectOperand)
    {
      return false;
    }

    while (ops.Count > 0)
    {
      if (!TryApplyTopOperator(values, ops))
      {
        return false;
      }
    }

    if (values.Count != 1)
    {
      return false;
    }

    result = values.Pop();
    return true;
  }

  private static bool TryApplyTopOperator(Stack<decimal> values, Stack<char> ops)
  {
    if (values.Count < 2 || ops.Count == 0)
    {
      return false;
    }

    decimal right = values.Pop();
    decimal left = values.Pop();
    char op = ops.Pop();
    decimal value;
    switch (op)
    {
      case '+':
        value = left + right;
        break;
      case '-':
        value = left - right;
        break;
      case '*':
        value = left * right;
        break;
      case '/':
        if (right == 0m)
        {
          return false;
        }

        value = left / right;
        break;
      default:
        return false;
    }

    values.Push(value);
    return true;
  }

  private sealed class NullNoticeService : INoticeService
  {
    public static readonly INoticeService Instance = new NullNoticeService();

    public void Toast(string message, ToastLevel level = ToastLevel.Information, TimeSpan? duration = null) { }
    public void Show(string message, string? actionText = null, TimeSpan? duration = null) { }
    public Task<SnackbarResult> ShowAsync(string message, string? actionText = null, TimeSpan? duration = null)
      => Task.FromResult(SnackbarResult.Dismissed);
    public void Notify(string title, string message, NotificationLevel level = NotificationLevel.Information,
                       TimeSpan? duration = null, string? actionText = null)
    { }
    public Task<NotificationResult> NotifyAsync(string title, string message, NotificationLevel level = NotificationLevel.Information,
                                                TimeSpan? duration = null, string? actionText = null)
      => Task.FromResult(NotificationResult.Dismissed);
  }
}

/// <summary>
///   Configuration options for <see cref="NumpadDialogSession" /> extension methods.
/// </summary>
public sealed class NumpadOptions : DialogOptions
{
  /// <summary>Gets or sets the initial value shown in the input.</summary>
  public decimal InitialValue { get; init; }

  /// <summary>Gets or sets the minimum allowed value. Defaults to <see cref="decimal.MinValue" />.</summary>
  public decimal Min { get; init; } = decimal.MinValue;

  /// <summary>Gets or sets the maximum allowed value. Defaults to <see cref="decimal.MaxValue" />.</summary>
  public decimal Max { get; init; } = decimal.MaxValue;

  /// <summary>Gets or sets the optional unit suffix displayed with the value.</summary>
  public string? Unit { get; init; }

  /// <summary>Gets or sets the maximum allowed decimal places. Defaults to <c>3</c>.</summary>
  public int MaxDecimalPlaces { get; init; } = 3;

  /// <summary>Gets or sets the maximum allowed integer digits for a single number segment. Defaults to <c>8</c>.</summary>
  public int MaxIntegerDigits { get; init; } = 8;

  /// <summary>Gets or sets whether decimal input is allowed. Defaults to <see langword="true" />.</summary>
  public bool AllowDecimal { get; init; } = true;

  /// <summary>
  ///   Gets or sets the optional guidance text shown below the title.
  /// </summary>
  public string? Message { get; init; } = Lang.Dialog.Numpad.Message;

  /// <summary>Gets or sets the override for the confirm button text.</summary>
  public string ConfirmText { get; init; } = Lang.Dialog.Numpad.ConfirmText;
}

/// <summary>
///   Extension methods for showing a numeric input dialog using the <see cref="IRouter" />.
/// </summary>
public static partial class DialogSessionExtensions
{
  extension(IRouter router)
  {
    /// <summary>
    ///   Shows a numpad dialog for numeric input.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="sessionOptions">
    ///   Optional session-level overrides for guidance text, initial value, range, precision, and
    ///   button labels.
    /// </param>
    /// <returns>A task that resolves to the confirmed value or <see langword="null" /> when canceled.</returns>
    public Task<decimal?> InputNumberAsync(string? title = null,
                                           NumpadOptions? sessionOptions = null)
    {
      sessionOptions ??= new NumpadOptions();
      NumpadDialogSession session = new()
      {
        Title = title ?? Lang.Dialog.Numpad.Title,
        Message = sessionOptions.Message,
        Min = sessionOptions.Min,
        Max = sessionOptions.Max,
        Unit = sessionOptions.Unit,
        ConfirmText = sessionOptions.ConfirmText,
        MaxDecimalPlaces = sessionOptions.MaxDecimalPlaces,
        MaxIntegerDigits = sessionOptions.MaxIntegerDigits,
        AllowDecimal = sessionOptions.AllowDecimal,
        Value = sessionOptions.InitialValue
      };

      return router.ShowAsync<decimal?>(session, sessionOptions.ToDimmer());
    }
  }
}
