using System.ComponentModel;

namespace Everlong.Nester.Controls;

/// <summary>
///   A view for displaying a toast notification.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public partial class ToastItemView : BottomSlideItemViewBase
{
  /// <summary>
  ///   Initializes a new instance of the <see cref="ToastItemView" /> class.
  /// </summary>
  public ToastItemView()
  {
    InitializeComponent();
  }
}
