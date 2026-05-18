using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Everlong.Nester.Extensions.Properties;
using Everlong.Nester.Routing;

namespace Everlong.Nester.ComponentModel;

/// <summary>
///   The dialog session base — the dismissal and the presentation's result
///   write-back channel.  Result-carrying sessions derive from
///   <see cref="DialogSessionBase{TResult}" />.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract partial class DialogSessionBase : ObservableObject,
                                                  IArrived, IRoutable
{
  /// <summary>The label of the dismissal affordance.</summary>
  public string CloseText { get; init; } = Lang.Dialog.Close;

  /// <summary>
  ///   The presentation's result write-back channel — captured when the
  ///   session joins the chain (at the commit, before the arrival
  ///   converges, so a close racing the arrival still settles).
  /// </summary>
  [EditorBrowsable(EditorBrowsableState.Never)]
  public IRouterCompletion? Completion { get; set; }

  /// <summary>Dismisses the presentation without a result.</summary>
  [RelayCommand]
  public void Close()
  {
    Completion?.Complete(null);
  }

  /// <summary>Captures the presentation's result channel on joining the chain.</summary>
  void IRoutable.OnRoutedTo(IRoutingContext context)
  {
    Completion = context.Features.Get<IRouterCompletion>();
  }

  void IRoutable.OnRoutedFrom(IRoutingContext context)
  {
  }

  /// <summary>Arrival hook — overridable; the result channel is captured on joining the chain.</summary>
  public virtual Task OnArrivedAsync(IRoutingContext context)
  {
    return Task.CompletedTask;
  }
}

/// <summary>
///   The result-carrying session base — <see cref="Close(TResult?)" /> settles
///   the session with a typed result.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract partial class DialogSessionBase<TResult> : DialogSessionBase
{
  /// <summary>
  ///   Completes the session: writes the result to the presentation's
  ///   result channel and closes the hosting derived router.  The write is dropped
  ///   when no completion surface is captured.
  /// </summary>
  public void Close(TResult? result)
  {
    Completion?.Complete(result ?? default);
  }
}
