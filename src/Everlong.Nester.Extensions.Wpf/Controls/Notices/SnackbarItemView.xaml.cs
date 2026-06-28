using System.ComponentModel;

namespace Everlong.Nester.Controls;

/// <summary>
///   A view for displaying a snackbar notification.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public partial class SnackbarItemView : BottomSlideItemViewBase
{
  /// <summary>
  ///   Initializes a new instance of the <see cref="SnackbarItemView" /> class.
  /// </summary>
  public SnackbarItemView()
  {
    InitializeComponent();
  }
}
