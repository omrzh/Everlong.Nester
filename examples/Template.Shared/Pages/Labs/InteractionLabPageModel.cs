using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Everlong.DI;
using Everlong.Globalization;
using Everlong.Nester.ComponentModel;
using Everlong.Nester.Dialog;
using Everlong.Nester.Notice;
using Everlong.Nester.Primitives;
using Everlong.Nester.Routing;
using NesterApp.Dialogs;
using NesterApp.Models;
using NesterApp.Pages.Shell;
using NesterApp.Properties;
using System.Net;

namespace NesterApp.Pages.Labs;

/// <summary>
///   The interaction lab: every feedback channel (toast / snackbar /
///   notification), every built-in dialog, the custom sessions, ICU messages
///   and the view-mapping samples, with one command each.  This page is the
///   executable reference for the interaction surface.
/// </summary>
/// <remarks>
///   Your seam: nothing here is demoware you must keep — copy the command whose
///   effect you want, and delete the page once your own pages cover it.
/// </remarks>
[Routable]
[Layout<MainLayoutModel>]
[Transient]
public partial class InteractionLabPageModel : RoutableModel
{
  [Inject] private partial INoticeService Notice { get; }

  /// <summary>
  ///   Demo of mapping different data types to corresponding views using DataTemplate mechanism.
  /// </summary>
  public LabsStrings LabsStrings => Lang.Labs;

  [ObservableProperty]
  public partial List<object> MappingSamples { get; private set; }

  // ── ICU MessageFormat demo ──────────────────────────────────────────────────
  public string IcuName
  {
    get;
    set
    {
      if (SetProperty(ref field, value))
      {
        OnIcuInputChanged();
      }
    }
  } = "Nester";

  public int IcuItemCount
  {
    get;
    set
    {
      if (SetProperty(ref field, value))
      {
        OnIcuInputChanged();
      }
    }
  } = 3;

  public string IcuGender
  {
    get;
    set
    {
      if (SetProperty(ref field, value))
      {
        OnIcuInputChanged();
      }
    }
  } = "male";

  public string IcuInterpolationOutput => IcuMessageFormatter.Format(
    Lang.Labs.IcuInterpolationResult,
    new Dictionary<string, object?> { ["name"] = IcuName });

  public string IcuPluralOutput => IcuMessageFormatter.Format(
    Lang.Labs.IcuPluralResult,
    new Dictionary<string, object?> { ["count"] = IcuItemCount });

  public string IcuSelectOutput => IcuMessageFormatter.Format(
    Lang.Labs.IcuSelectResult,
    new Dictionary<string, object?> { ["gender"] = IcuGender });

  protected override void OnRoutedTo(IRoutingContext context, bool isFirstRouted)
  {
    if (!isFirstRouted)
    {
      return;
    }

    MappingSamples =
    [
      new TextMessage(Lang.Labs.MappingTextMessage),
      new ImageMessage("https://avatars.githubusercontent.com/u/6182367?s=200&v=4", Lang.Labs.MappingImageAlt),
      new AlertMessage(Lang.Labs.Warning, Lang.Labs.MappingAlertWarningMessage, AlertSeverity.Warning),
      new AlertMessage(Lang.Labs.Error, Lang.Labs.MappingAlertErrorMessage, AlertSeverity.Error),
      new Circle { Radius = 30 },
      new Rectangle { Width = 60, Height = 40 },
      new List<IShape>
      {
        new Circle { Radius = 50 },
        new Rectangle { Width = 60, Height = 80 }
      }
    ];
  }

  private void OnIcuInputChanged()
  {
    OnPropertyChanged(nameof(IcuInterpolationOutput));
    OnPropertyChanged(nameof(IcuPluralOutput));
    OnPropertyChanged(nameof(IcuSelectOutput));
  }

  [RelayCommand]
  private void ChangeName()
  {
    IcuName =
      IcuName == "Nester" ? "Avalonia" : IcuName == "Avalonia" ? "WPF" : "Nester";
  }

  [RelayCommand]
  private void AddItem()
  {
    IcuItemCount++;
  }

  [RelayCommand]
  private void RemoveItem()
  {
    if (IcuItemCount > 0)
    {
      IcuItemCount--;
    }
  }

  [RelayCommand]
  private void CycleGender()
  {
    IcuGender =
      IcuGender == "male" ? "female" : IcuGender == "female" ? "other" : "male";
  }

  // ── Notifications ───────────────────────────────────────────────────────────
  // ShowNotification: persistent panel notification (title + body, optional severity).
  // These stack in a notification panel that the user can dismiss individually.

  [RelayCommand]
  private void ShowInfoNotification()
  {
    Notice.Notify(Lang.Labs.Info, Lang.Labs.InfoNotificationMessage);
  }

  [RelayCommand]
  private void ShowSuccessNotification()
  {
    Notice.Notify(Lang.Labs.Success, Lang.Labs.SuccessNotificationMessage, NotificationLevel.Success);
  }

  [RelayCommand]
  private void ShowWarningNotification()
  {
    Notice.Notify(Lang.Labs.Warning, Lang.Labs.WarningNotificationMessage, NotificationLevel.Warning);
  }

  [RelayCommand]
  private void ShowErrorNotification()
  {
    Notice.Notify(Lang.Labs.Error, Lang.Labs.ErrorNotificationMessage, NotificationLevel.Error);
  }

  // ── Snackbars ────────────────────────────────────────────────────────────────
  // Show: brief bottom bar message; fire-and-forget.
  // ShowAsync: returns SnackbarResult so you can react to "Undo" action clicks.

  [RelayCommand]
  private void ShowSimpleSnackbar()
  {
    Notice.Show(Lang.Labs.SimpleSnackbarMessage);
  }

  [RelayCommand]
  private async Task ShowActionSnackbar()
  {
    SnackbarResult result = await Notice.ShowAsync(Lang.Labs.ActionSnackbarMessage, Lang.Labs.ActionSnackbarUndo,
                                                   TimeSpan.FromSeconds(5));
    if (result == SnackbarResult.ActionInvoked)
    {
      Notice.Toast(Lang.Labs.ActionSnackbarUndoClickedMessage, ToastLevel.Success);
    }
  }

  // ── Toasts ───────────────────────────────────────────────────────────────────
  // ShowToast: small floating message that auto-dismisses after a timeout.

  [RelayCommand]
  private void ShowInfoToast()
  {
    Notice.Toast(Lang.Labs.InfoToastMessage);
  }

  [RelayCommand]
  private void ShowSuccessToast()
  {
    Notice.Toast(Lang.Labs.SuccessToastMessage, ToastLevel.Success, TimeSpan.FromSeconds(3));
  }

  [RelayCommand]
  private void ShowErrorToast()
  {
    Notice.Toast(Lang.Labs.ErrorToastMessage, ToastLevel.Error);
  }

  #region Dialogs

  // ── Built-in Dialogs ─────────────────────────────────────────────────────────
  // AlertAsync: one-button informational overlay. Awaitable so you can sequence code after dismiss.
  [RelayCommand]
  private async Task ShowAlert()
  {
    await Router.AlertAsync(Lang.Labs.AlertDialogMessage);
  }

  // ConfirmAsync: two-button yes/no overlay; returns bool.
  [RelayCommand]
  private async Task ShowConfirm()
  {
    bool result = await Router.ConfirmAsync(Lang.Labs.ConfirmDialogMessage,
                                            new ConfirmOptions { Title = Lang.Labs.ConfirmDialogTitle });
    Notice.Toast(
      result ? Lang.Labs.ConfirmDialogAcceptedMessage : Lang.Labs.ConfirmDialogCanceledMessage,
      result ? ToastLevel.Success : ToastLevel.Warning);
  }

  [RelayCommand]
  private async Task ShowDeferrableConfirm()
  {
    bool result = await Router.ConfirmAsync(Lang.Labs.DeferrableConfirmMessage, new ConfirmOptions
    {
      CountdownSeconds = 5
    });
    if (result)
    {
      Notice.Toast(Lang.Labs.DeferrableConfirmAccepted);
    }
  }

  // Sessions carry their own options — set them before showing, or subclass the session.
  // The default presentation is light-dismiss: clicking the backdrop closes
  // (see ShowMandatoryDialog for the mandatory variant).
  [RelayCommand]
  private async Task ShowCustomOptions()
  {
    await Router.ShowAsync<bool>(new MyConfirmDialogSession
    {
      Title = Lang.Labs.CustomDialogTitle,
      Message = Lang.Labs.CustomOptionsDialogMessage
    });
  }

  // ── Custom Dialog Session ─────────────────────────────────────────────────────
  // ShowAsync<T>: show a dialog driven by your own DialogSessionBase<TResult> subclass.
  // The session object carries parameters (Message) and calls Close(result) to complete.
  [RelayCommand]
  private async Task ShowNestedDialog()
  {
    bool confirm = await Router.ShowAsync<bool>(new MyConfirmDialogSession
    {
      Title = Lang.Labs.CustomDialogTitle,
      Message = Lang.Labs.NestedDialogMessage
    });
  }

  // ── Mandatory Dialog Demos ──────────────────────────────────────────────────
  // The presentation's dismissal policy is the dimmer's business: the
  // mandatory variant pins LightDismiss off (backdrop taps refuse and
  // shake); the default ShowAsync presents light-dismiss (tap to close).
  [RelayCommand]
  private async Task ShowMandatoryDialog()
  {
    await Router.ShowAsync<bool>(new AnchorDemoSession { Title = Lang.Labs.Mandatory },
                                 new DefaultDimmerModel { LightDismiss = false });
  }

  [RelayCommand]
  private async Task ShowNonMandatoryDialog()
  {
    await Router.ShowAsync(new AnchorDemoSession { Title = Lang.Labs.NonMandatory });
  }

  // ── Selection Dialog ──────────────────────────────────────────────────────────
  // SelectFromOptionsAsync: shows a list picker; supports both string items and
  // typed objects (view-mapped via DataTemplate, same as MappingSamples above).
  [RelayCommand]
  private async Task ShowSelection()
  {
    string[] options = new[] { Lang.Labs.SelectionOption1, Lang.Labs.SelectionOption2, Lang.Labs.SelectionOption3 };
    string? result =
      await Router.SelectFromOptionsAsync(options, Lang.Labs.SelectionPrompt, Lang.Labs.SelectionTitle);
    if (result is null)
    {
      return;
    }

    Notice.Toast(IcuMessageFormatter.Format(Lang.Labs.SelectionResultMessage,
                                            new Dictionary<string, object?> { ["option"] = result }),
                 ToastLevel.Success);

    IShape[] shapeOptions = new IShape[] { new Circle { Radius = 20 }, new Rectangle { Width = 40, Height = 30 } };
    IShape? selectedShape =
      await Router.SelectFromOptionsAsync(shapeOptions, Lang.Labs.ShapeSelectionPrompt,
                                          Lang.Labs.ShapeSelectionTitle);
    switch (selectedShape)
    {
      case Circle:
        Notice.Toast(Lang.Labs.ShapeSelectedCircleMessage);
        break;
      case Rectangle:
        Notice.Toast(Lang.Labs.ShapeSelectedRectangleMessage);
        break;
    }
  }

  // ── Loading / Progress Dialogs ────────────────────────────────────────────────
  // CreateWait: shows a blocking overlay. The IWaitScope returned is IDisposable —
  // the dialog closes automatically when the using block exits.
  [RelayCommand]
  private async Task ShowGlobalLoading()
  {
    using WaitDialogSession wait = Router.CreateWait(Lang.Labs.Loading);
    await Task.Delay(2500);
  }

  // WaitDialogOptions.Maximum enables a progress bar; Report() updates the label,
  // Advance(n) increments the progress value.
  [RelayCommand]
  private async Task ShowWait()
  {
    using WaitDialogSession wait =
      Router.CreateWait(Lang.Labs.WaitDialogTitle,
                        new WaitDialogOptions { Message = Lang.Labs.WaitDialogMessage, Maximum = 100 });
    await Task.Delay(1200);
    wait.Report(Lang.Labs.WaitDialogProgressMessage);
    wait.Advance(50);
    await Task.Delay(1200);
    wait.Advance(50);
    await Task.Delay(300);
  }

  // ── Custom presentation (view self-animated + session policy) ───────────
  [RelayCommand]
  private async Task ShowLeftDrawer()
  {
    await Router.ShowAsync(new LeftDrawerDialogSession());
  }

  // ── Specialty Input Dialogs ───────────────────────────────────────────────────
  // InputNumberAsync: numpad-style decimal input with optional min/max/unit.
  [RelayCommand]
  private async Task ShowNumpad()
  {
    NumpadOptions numpadOptions = new()
    {
      Message = Lang.Labs.InputWeightMessage,
      InitialValue = 500m,
      Min = 0m,
      Max = 5000m,
      Unit = Lang.Labs.InputWeightUnit
    };

    decimal? number = await Router.InputNumberAsync(Lang.Labs.InputWeightTitle,
                                                    numpadOptions);
  }

  // SignHandwrittenNameAsync: captures a handwritten signature as a bitmap.
  [RelayCommand]
  private async Task SignName()
  {
    SignNameResult? result = await Router.SignHandwrittenNameAsync();
  }

  // PickColorAsync: color picker overlay; returns a ValueColor (platform-agnostic).
  // Call .ToColor() to convert to the platform's native Color type.
  [RelayCommand]
  private async Task PickColor()
  {
    ValueColor? color = await Router.PickColorAsync();
    // Convert to platform-specific color type if needed
    // for avalonia, it's "Avalonia.Media.Color"
    // for Wpf, it's "System.Windows.Media.Color"
    // Color? pColor = color?.ToColor();
  }

  // PickDateTimeAsync: date-and-time picker overlay.
  [RelayCommand]
  private async Task PickDateTime()
  {
    DateTime? dateTime = await Router.PickDateTimeAsync();
  }

  // ComposeIpv4Async: four-octet IP address input with numeric spinners.
  [RelayCommand]
  private async Task PickIpv4()
  {
    IPAddress? address = await Router.ComposeIpv4Async(new Ipv4ComposerOptions
    {
      InitialIpAddress = IPAddress.Parse("192.168.1.1")
    });
  }

  // PreviewAsync: full-screen image viewer. ImagePreviewInput.FromPathOrUrl
  // accepts a local file path or a remote URL (requires an HttpClient for URLs).
  [RelayCommand]
  private async Task PreviewImage()
  {
    const string previewImageUrl = "https://picsum.photos/400/300";
    using HttpClient httpClient = new();
    await Router.PreviewAsync(ImagePreviewInput.FromPathOrUrl(previewImageUrl, httpClient));
  }

  #endregion
}
