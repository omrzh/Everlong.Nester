using System.Collections;
using System.ComponentModel;

namespace Everlong.Nester.Notice;

/// <summary>
///   The notice engine port — drives the three concurrent entry stacks
///   (toast / snackbar / banner) and their scene lifecycle.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface INoticeEngine
{
  /// <summary>Toast entry views, in display order.</summary>
  IEnumerable ToastEntries { get; }

  /// <summary>Snackbar entry views, in display order.</summary>
  IEnumerable SnackbarEntries { get; }

  /// <summary>Notification banner entry views, in display order.</summary>
  IEnumerable BannerEntries { get; }

  /// <summary>Shows a toast entry.  Must run on the UI thread.</summary>
  void ShowToast(ToastEntry entry, NoticeServiceOptions options);

  /// <summary>Shows a snackbar entry.  Must run on the UI thread.</summary>
  void ShowSnackbar(SnackbarEntry entry, NoticeServiceOptions options);

  /// <summary>Shows a notification banner entry.  Must run on the UI thread.</summary>
  void ShowNotification(NotificationEntry entry, NoticeServiceOptions options);
}
