namespace Everlong.Nester.Notice;

/// <summary>
///   The notice panel's visual options — where each channel's region anchors
///   and how far it keeps from the shell edge.
/// </summary>
/// <remarks>
///   These are the panel's options, not the service's: the service hands the
///   model to the panel and reads nothing from it.  An application that wants
///   a different look maps its own model to its own panel through the
///   template table.
/// </remarks>
public class NoticePanelModel
{
  /// <summary>Anchor position of the toast region. Default is <see cref="NoticePosition.TopCenter" />.</summary>
  public NoticePosition ToastPosition { get; set; } = NoticePosition.TopCenter;

  /// <summary>Anchor position of the snackbar region. Default is <see cref="NoticePosition.BottomCenter" />.</summary>
  public NoticePosition SnackbarPosition { get; set; } = NoticePosition.BottomCenter;

  /// <summary>Anchor position of the notification region. Default is <see cref="NoticePosition.TopRight" />.</summary>
  public NoticePosition NotificationPosition { get; set; } = NoticePosition.TopRight;

  /// <summary>Spacing between the toast region and the shell edge. Default is 16 on all sides.</summary>
  public Primitives.Thickness ToastMargin { get; set; } = new(16);

  /// <summary>Spacing between the snackbar region and the shell edge. Default is 16 on all sides.</summary>
  public Primitives.Thickness SnackbarMargin { get; set; } = new(16);

  /// <summary>Spacing between the notification region and the shell edge. Default is 16 on all sides.</summary>
  public Primitives.Thickness NotificationMargin { get; set; } = new(16);
}
