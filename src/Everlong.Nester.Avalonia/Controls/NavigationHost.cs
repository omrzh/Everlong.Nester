// NOTE: Single-source file — the WPF project compiles this exact file via
// <Compile Include> in Everlong.Nester.Wpf.csproj.  Edit it here only;
// never create a WPF-side copy (the two builds would drift).


using Everlong.Nester.Routing;

namespace Everlong.Nester.Controls;

/// <summary>
///   The platform navigation view — a layout body that hosts the presented
///   chain's outermost view, carries the model's committed state and presents
///   the convergence runs through the platform stages.
/// </summary>
internal sealed class NavigationHost : LayoutBody, IRoutingView, IFocusPolicySurface
{
  private readonly AssembleViewsStage _assemble;
  private readonly PlatformRevealStage _reveal;

  internal NavigationHost()
  {
    _assemble = new AssembleViewsStage(this);
    _reveal = new PlatformRevealStage(this);
  }

  /// <inheritdoc />
  public FocusPolicy FocusPolicy
  {
    get;
    set
    {
      if (field == value)
        return;
      field = value;
      FocusPolicyChanged?.Invoke();
    }
  } = FocusPolicy.Reachable;

  /// <inheritdoc />
  public event Action? FocusPolicyChanged;

  public ILocation? Location { get; private set; }
  public IRouter? Router { get; private set; }

  void IRoutingView.SetLocation(ILocation? location)
  {
    Location = location;

    // The site is also mirrored on the host's own attached property — a
    // host-local write (not inherited) that lets a consumer discovering the
    // surface from outside read it.  The nav chrome (RouteLink /
    // RouteTreeItem) reads this host's router stack instead.
    SetValue(RoutingChannel.LocationProperty, location);
  }


  void IRoutingView.SetRouter(IRouter? router)
  {
    Router = router;
    SetValue(RoutingChannel.RouterProperty, router);
  }

  /// <inheritdoc />
  void IRoutingView.AssembleViews(IReadOnlyList<Location> chain) => _assemble.Assemble(chain);

  /// <inheritdoc />
  Task IRoutingView.RevealAsync(IConvergenceScene scene) => _reveal.RevealAsync(scene);

  /// <inheritdoc />
  void IRoutingView.ReleaseViews(IReadOnlyList<Location> released)
    => _reveal.RemoveReleasedViews(released);
}
