using System.Collections.Frozen;
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
///   preview.  Register an instance in the application's DataTemplates after
///   the app's own locators — the first entry that matches wins, so this one
///   stays the fallback; types outside this set map to their own views via
///   <c>[ViewFor]</c> / <c>[Mapping]</c>.
/// </remarks>
public sealed class NesterExtendedViewLocator : IViewLocator
{
  private readonly FrozenSet<Type> _supportedTypes = new HashSet<Type>()
  {
    typeof(ToastEntry),
    typeof(SnackbarEntry),
    typeof(NotificationEntry),
    typeof(DefaultDimmerModel),
    typeof(AlertDialogSession),
    typeof(ConfirmDialogSession),
    typeof(WaitDialogSession),
    typeof(ChoiceDialogSession),
    typeof(OptionSelectDialogSession),
    typeof(NumpadDialogSession),
    typeof(SignaturePadDialogSession),
    typeof(DateTimePickerDialogSession),
    typeof(ColorPickerDialogSession),
    typeof(Ipv4ComposerDialogSession),
    typeof(ImagePreviewDialogSession)
  }.ToFrozenSet();

  /// <summary>
  ///   Determines whether the specified data is a supported framework type.
  /// </summary>
  /// <param name="data">The object to test, or <c>null</c>.</param>
  /// <returns><c>true</c> if the data is a non-null instance of a supported type; <c>false</c> otherwise.</returns>
  public bool Match(object? data) =>
    data is not null && _supportedTypes.Contains(data.GetType());

  /// <summary>
  ///   Builds and returns the default view for the specified framework type.
  /// </summary>
  /// <remarks>
  ///   For types not in <see cref="_supportedTypes"/>, this method returns <c>null</c>.
  /// </remarks>
  /// <param name="data">The object for which to build a view, or <c>null</c>.</param>
  /// <returns>A new view instance for the type, or <c>null</c> if the type is not supported.</returns>
  public PControl? Build(object? data)
  {
    if (data is null)
      return null;

    PControl? view = data switch
    {
      ToastEntry => new ToastItemView(),
      SnackbarEntry => new SnackbarItemView(),
      NotificationEntry => new NotificationItemView(),
      DefaultDimmerModel => new DimmerLayout(),
      AlertDialogSession => new AlertDialogView(),
      ConfirmDialogSession => new ConfirmDialogView(),
      WaitDialogSession => new WaitDialogView(),
      ChoiceDialogSession => new ChoiceDialogView(),
      OptionSelectDialogSession => new OptionSelectDialogView(),
      NumpadDialogSession => new NumpadDialogView(),
      SignaturePadDialogSession => new SignaturePadDialogView(),
      DateTimePickerDialogSession => new DateTimePickerDialogView(),
      ColorPickerDialogSession => new ColorPickerDialogView(),
      Ipv4ComposerDialogSession => new Ipv4ComposeDialogView(),
      ImagePreviewDialogSession => new ImagePreviewDialogView(),
      _ => null
    };

    return view;
  }
}
