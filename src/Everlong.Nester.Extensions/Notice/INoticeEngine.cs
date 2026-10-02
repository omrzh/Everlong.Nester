using System.ComponentModel;

namespace Everlong.Nester.Notice;

/// <summary>
///   The notice engine port — drives the three concurrent entry stacks
///   (toast / snackbar / banner) and their scene lifecycle against the
///   mounted panel.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface INoticeEngine
{
  /// <summary>Binds the mounted panel — from now on every show presents into it.</summary>
  void AttachPanel(INoticePanel panel);

  /// <summary>Shows a toast entry.  Must run on the UI thread.</summary>
  void ShowToast(ToastEntry entry, NoticeServiceOptions options);

  /// <summary>Shows a snackbar entry.  Must run on the UI thread.</summary>
  void ShowSnackbar(SnackbarEntry entry, NoticeServiceOptions options);

  /// <summary>Shows a notification banner entry.  Must run on the UI thread.</summary>
  void ShowNotification(NotificationEntry entry, NoticeServiceOptions options);
}
