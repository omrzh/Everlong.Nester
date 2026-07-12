using Everlong.Nester.Presentation;
using Terminal.Gui.ViewBase;

namespace Everlong.Nester.Routing;

/// <summary>The Terminal.Gui router's navigation model.</summary>
internal sealed class TerminalPlatformStackModel(IServiceProvider services) : RouterStack
{
  /// <inheritdoc />
  protected override IRoutingView View { get; } = new TerminalNavigationHost(services);

  /// <inheritdoc />
  protected override Location CreateLocation(Type type, object instance, IArgs? args)
    => new TerminalLocation(type, args, instance);
}

/// <summary>
///   The Terminal.Gui realized node — the terminal view behind the base
///   visual slot and the mount child of its parent's layout body.
/// </summary>
internal sealed class TerminalLocation(Type type, IArgs? args, object instance)
  : Location(type, args, instance), IViewLocation<View>
{
  /// <summary>The assembled terminal view — the strongly-typed face of the base visual slot.</summary>
  public View? View { get => Presenter as View; set => Presenter = value; }

  /// <summary>The mount point inside this node's view — where the inner node's view mounts.</summary>
  public ILayoutBody<View>? Body { get; set; }
}
