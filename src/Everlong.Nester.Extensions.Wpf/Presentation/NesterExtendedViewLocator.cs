using Everlong.Nester.Controls;
using Everlong.Nester.Dialog;
using Everlong.Nester.Notice;
namespace Everlong.Nester.Presentation;

/// <summary>
///   Default view locator for the extension packages' framework views:
///   the notice entries (toasts, snackbars and notifications), the dialog
///   sessions and the dimmer chrome model.
/// </summary>
/// <remarks>
///   Covers the notice entries (<see cref="ToastEntry" />,
///   <see cref="SnackbarEntry" />, <see cref="NotificationEntry" />), the
///   dimmer chrome model (<see cref="DefaultDimmerModel" />) and the dialog
///   session types: alert, confirm, wait, choice, option select, numpad,
///   signature pad, date/time picker, color picker, IPv4 composer and image
///   preview.  Register an instance in the application resources before the
///   app's own locators — a merged dictionary resolves last entry first, so
///   this one stays the fallback; types outside this set map to their own
///   views via <c>[ViewFor]</c> / <c>[Mapping]</c>.
/// </remarks>
public sealed class NesterExtendedViewLocator : ViewLocatorBase
{
  /// <summary>Registers the extension framework DataTemplates into this locator's resource dictionary.</summary>
  public NesterExtendedViewLocator()
  {
    AddTemplate<ToastEntry, ToastItemView>();
    AddTemplate<SnackbarEntry, SnackbarItemView>();
    AddTemplate<NotificationEntry, NotificationItemView>();
    AddTemplate<DefaultDimmerModel, DimmerLayout>();
    AddTemplate<AlertDialogSession, AlertDialogView>();
    AddTemplate<ConfirmDialogSession, ConfirmDialogView>();
    AddTemplate<WaitDialogSession, WaitDialogView>();
    AddTemplate<ChoiceDialogSession, ChoiceDialogView>();
    AddTemplate<OptionSelectDialogSession, OptionSelectDialogView>();
    AddTemplate<NumpadDialogSession, NumpadDialogView>();
    AddTemplate<SignaturePadDialogSession, SignaturePadDialogView>();
    AddTemplate<DateTimePickerDialogSession, DateTimePickerDialogView>();
    AddTemplate<ColorPickerDialogSession, ColorPickerDialogView>();
    AddTemplate<Ipv4ComposerDialogSession, Ipv4ComposeDialogView>();
    AddTemplate<ImagePreviewDialogSession, ImagePreviewDialogView>();
  }

  /// <inheritdoc/>
  public override PControl? Build(object? data)
  {
    if (data is null)
      return null;

    if (data is ToastEntry)
      return new ToastItemView();
    if (data is SnackbarEntry)
      return new SnackbarItemView();
    if (data is NotificationEntry)
      return new NotificationItemView();
    if (data is DefaultDimmerModel)
      return new DimmerLayout();
    if (data is AlertDialogSession)
      return new AlertDialogView();
    if (data is ConfirmDialogSession)
      return new ConfirmDialogView();
    if (data is WaitDialogSession)
      return new WaitDialogView();
    if (data is ChoiceDialogSession)
      return new ChoiceDialogView();
    if (data is OptionSelectDialogSession)
      return new OptionSelectDialogView();
    if (data is NumpadDialogSession)
      return new NumpadDialogView();
    if (data is SignaturePadDialogSession)
      return new SignaturePadDialogView();
    if (data is DateTimePickerDialogSession)
      return new DateTimePickerDialogView();
    if (data is ColorPickerDialogSession)
      return new ColorPickerDialogView();
    if (data is Ipv4ComposerDialogSession)
      return new Ipv4ComposeDialogView();
    if (data is ImagePreviewDialogSession)
      return new ImagePreviewDialogView();

    // Fast path miss: consult the resource dictionary (AddTemplate registrations)
    // before giving up — unmatched types return null so the chain's other
    // locators (user views) still get a chance.
    return base.Build(data);
  }
}
