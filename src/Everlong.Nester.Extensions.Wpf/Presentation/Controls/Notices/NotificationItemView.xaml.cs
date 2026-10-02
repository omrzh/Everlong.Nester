using System.ComponentModel;

namespace Everlong.Nester.Presentation;

/// <summary>
///   A view for displaying a notification item.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public partial class NotificationItemView : FeedbackItemViewBase
{
  /// <summary>
  ///   Initializes a new instance of the <see cref="NotificationItemView" /> class.
  /// </summary>
  public NotificationItemView()
  {
    InitializeComponent();
  }
}
