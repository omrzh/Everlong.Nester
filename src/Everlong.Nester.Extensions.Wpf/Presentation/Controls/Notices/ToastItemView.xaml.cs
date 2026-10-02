using System.ComponentModel;

namespace Everlong.Nester.Presentation;

/// <summary>
///   A view for displaying a toast notification.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public partial class ToastItemView : FeedbackItemViewBase
{
  /// <summary>
  ///   Initializes a new instance of the <see cref="ToastItemView" /> class.
  /// </summary>
  public ToastItemView()
  {
    InitializeComponent();
  }
}
