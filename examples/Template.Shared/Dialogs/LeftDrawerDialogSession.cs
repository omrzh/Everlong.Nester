using Everlong.Nester.ComponentModel;
using NesterApp.Properties;

namespace NesterApp.Dialogs;

/// <summary>
///   Left-drawer session — a light-dismiss dialog: the backdrop tap closes
///   it.  The drawer reveals with its own view default animation.
/// </summary>
public sealed class LeftDrawerDialogSession : DialogSessionBase<object?>
{
  /// <summary>Gets the heading the drawer view renders.</summary>
  public string Title { get; } = Lang.Labs.LeftDrawer;
}

/// <summary>
///   User-owned presentation strategy read back by the dialog view's
///   animation director.  Its type
///   and meaning are this app's business, not the framework's.
/// </summary>
public sealed class LeftDrawerPresentation
{
  public int DurationMs { get; init; } = 250;
}
