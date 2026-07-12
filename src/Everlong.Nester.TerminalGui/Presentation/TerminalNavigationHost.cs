using Everlong.Nester.Routing;

namespace Everlong.Nester.Presentation;

/// <summary>
///   The Terminal.Gui navigation view — a layout body that hosts the
///   presented chain's outermost view and presents the model's transactions
///   through the terminal stages.
/// </summary>
internal sealed class TerminalNavigationHost : TuiLayoutBody, IRoutingView
{
  private readonly TerminalAssembleStage _assemble;
  private readonly TerminalRevealStage _reveal;

  internal TerminalNavigationHost(IServiceProvider services)
  {
    _assemble = new TerminalAssembleStage(services);
    _reveal = new TerminalRevealStage(this);
  }

  /// <inheritdoc />
  public ILocation? Location { get; private set; }

  public IRouter? Router { get; private set; }

  /// <inheritdoc />
  void IRoutingView.SetLocation(ILocation? location) => Location = location;

  /// <inheritdoc />
  void IRoutingView.SetRouter(IRouter? router) => Router = router;

  /// <inheritdoc />
  void IRoutingView.AssembleViews(IReadOnlyList<Location> chain) => _assemble.Assemble(chain);

  /// <inheritdoc />
  Task IRoutingView.RevealAsync(IConvergenceScene scene) => _reveal.RevealAsync(scene);

  /// <inheritdoc />
  void IRoutingView.ReleaseViews(IReadOnlyList<Location> released)
    => _reveal.RemoveReleasedViews(released);
}
