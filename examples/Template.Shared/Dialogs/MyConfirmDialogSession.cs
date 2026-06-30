using CommunityToolkit.Mvvm.ComponentModel;
using Everlong.Nester.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Everlong.DI;
using Everlong.Nester.Dialog;
using Everlong.Nester.Routing;
using NesterApp.Properties;

namespace NesterApp.Dialogs;

/// <summary>
///   A custom dialog session — the smallest one that carries a result: inherit
///   <c>DialogSessionBase&lt;bool&gt;</c>, write the answer with <c>Close(result)</c>,
///   and the awaiting <c>Router.ShowAsync&lt;bool&gt;(...)</c> call receives it.
///   A dismissal (backdrop tap or back) settles <see langword="null" />.
/// </summary>
/// <remarks>
///   Your seam: a session is a model like any other — routable, arrival-hooked,
///   releasable — and its state lives on the model, never on the view.
/// </remarks>
public partial class MyConfirmDialogSession : DialogSessionBase<bool>
{
  [Inject] private partial IRouter Router { get; }

  /// <summary>Gets or sets the heading the dialog view renders.</summary>
  [ObservableProperty] public partial string? Title { get; set; }

  public MyConfirmDialogSession()
  {
    Title = Lang.Labs.ConfirmDialogTitle;
  }

  public required string? Message { get; set; }

  // Close(result) completes the Task<T> that ShowAsync returned to the caller.
  // The caller awaits it and receives the bool result.
  [RelayCommand] private void Yes() => Close(true);

  [RelayCommand] private void No() => Close();

  [RelayCommand]
  private async Task OpenNested()
  {
    // Dialogs can open further dialogs — Nester layers them in a dialog stack.
    await Router.ShowAsync<object?>(new NestedConfirmDialogSession { Message = Lang.Labs.NestedDialogMessage });
    Yes();
  }
}
