using CommunityToolkit.Mvvm.ComponentModel;
using Everlong.DI;
using Everlong.Nester.Routing;
using Everlong.Nester.Shell;

namespace Everlong.Nester.ComponentModel;

/// <summary>
///   An observable, injection-wired base for routable models — tracks the
///   routing state of the derived model.
/// </summary>
public abstract partial class RoutableModel : ObservableObject, IRoutable, IArrived, IReleasable
{
  /// <summary>Shell-level access.</summary>
  [Inject] protected partial IShell Shell { get; }

  /// <summary>The routing entry — the navigation request surface.</summary>
  [Inject] public partial IRouter Router { get; }

  /// <summary>Gets whether the model has ever been routed onto a chain.</summary>
  public bool HasBeenRouted { get; private set; }

  /// <summary>Gets whether the model has ever completed an arrival.</summary>
  public bool HasBeenArrived { get; private set; }

  /// <summary>Gets whether the model is currently routed.</summary>
  public bool IsActiveLocation { get; private set; }

  /// <inheritdoc />
  void IRoutable.OnRoutedTo(IRoutingContext context)
  {
    IsActiveLocation = true;
    var isFirstRouted = !HasBeenRouted;
    OnRoutedTo(context, isFirstRouted);
    HasBeenRouted = true;
  }

  /// <inheritdoc />
  void IRoutable.OnRoutedFrom(IRoutingContext context)
  {
    IsActiveLocation = false;
    OnRoutedFrom(context);
  }

  /// <summary>Runs when the model is routed onto a chain.</summary>
  /// <param name="context">The routing context of the transition.</param>
  /// <param name="isFirstRouted"><see langword="true" /> for the model's first routing since construction.</param>
  protected virtual void OnRoutedTo(IRoutingContext context, bool isFirstRouted) { }

  /// <summary>Runs when the model is routed off a chain through a navigation decision.</summary>
  /// <param name="context">The routing context of the transition.</param>
  protected virtual void OnRoutedFrom(IRoutingContext context) { }

  /// <inheritdoc />
  Task IArrived.OnArrivedAsync(IRoutingContext context)
  {
    var isFirstArrived = !HasBeenArrived;
    HasBeenArrived = true;
    return OnArrivedAsync(context, isFirstArrived);
  }

  /// <summary>Runs when the model's arrival completes.</summary>
  /// <param name="context">The routing context of the arrival.</param>
  /// <param name="isFirstArrived"><see langword="true" /> for the model's first arrival since construction.</param>
  protected virtual Task OnArrivedAsync(IRoutingContext context, bool isFirstArrived) => Task.CompletedTask;

  /// <inheritdoc/>
  void IReleasable.Release()
  {
    IsActiveLocation = false;
    OnReleased();
  }

  /// <summary>
  ///   Releases the model — a terminal close or teardown is not a transition
  ///   and runs no departure edge, so the release tail flips the routing
  ///   state off before the model is forgotten. Override to run teardown logic.
  /// </summary>
  protected virtual void OnReleased() { }

}
