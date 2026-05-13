using Everlong.Nester.Intent;
using Everlong.Nester.Presentation;
using Everlong.Nester.Routing;
using Everlong.Nester.Shell;

namespace Everlong.Nester.Tests.Routing;

/// <summary>Distinct layout types — a chain's shell is a different type from its content.</summary>
internal sealed class LayoutAlpha : TestContent
{
  public LayoutAlpha()
  {
  }
}

internal sealed class LayoutBeta : TestContent
{
  public LayoutBeta()
  {
  }
}

/// <summary>Distinct content/page types for exercising chain diffs and traversal.</summary>
internal sealed class PageAlpha : TestContent
{
  public PageAlpha()
  {
  }
}

internal sealed class PageBeta : TestContent
{
  public PageBeta()
  {
  }
}

internal sealed class PageGamma : TestContent
{
  public PageGamma()
  {
  }
}

internal sealed class PageDelta : TestContent
{
  public PageDelta()
  {
  }
}

/// <summary>A typed argument payload for the entry hand-over.</summary>
internal sealed record TestArgs(string Value) : Args;

/// <summary>
///   A test participant implementing every routing lifecycle contract plus a
///   mandatory sender-aware veto.
/// </summary>
internal class TestContent : IAdaptiveParameterized, IArrived, IDeparting, IDeparted, IReleasable, IIntentHandler
{
  internal bool Mandatory { get; init; }

  public IArgs? ReceivedArgs { get; private set; }

  /// <summary>The <see cref="IRoutingContext.Direction"/> answer at the latest arrival.</summary>
  public RoutingDirection? DirectionOnArrived { get; private set; }

  /// <summary>The <see cref="IRoutingContext.IsElevated"/> answer at the latest arrival.</summary>
  public bool IsElevatedOnArrived { get; private set; }

  /// <summary>The <see cref="IRoutingContext.Direction"/> answer at the latest navigation departure.</summary>
  public RoutingDirection? DirectionOnDeparting { get; private set; }

  /// <summary>The number of arrivals observed.</summary>
  public int ArrivalCount { get; private set; }

  /// <summary>The request completion surface seen at the latest arrival.</summary>
  public IRouterCompletion? SeenCompletion { get; private set; }

  public bool Departing { get; private set; }

  public bool Departed { get; private set; }

  public bool Released { get; private set; }

  public bool Arrived { get; private set; }

  public IArgs? EngagedArgs => ReceivedArgs;

  public bool IsAdaptable(IArgs? requested) => true;

  public void DeliverArgs(IArgs? args)
  {
    ReceivedArgs = args;
  }

  public Task OnArrivedAsync(IRoutingContext context)
  {
    Arrived = true;
    ArrivalCount++;
    DirectionOnArrived = context.Direction;
    IsElevatedOnArrived = context.IsElevated;
    SeenCompletion = context.Features.Get<IRouterCompletion>();
    return Task.CompletedTask;
  }

  public void OnDeparting(IRoutingContext context)
  {
    Departing = true;
    DirectionOnDeparting = context.Direction;
  }

  public void OnDeparted(IRoutingContext context) => Departed = true;

  public void Release() => Released = true;

  /// <summary>Vetoes only an external close when mandatory — never its own.</summary>
  public ValueTask HandleAsync(IntentContext context, IntentDelegate next)
  {
    if (context.Intent is BackIntent && Mandatory && !ReferenceEquals(context.Sender, this))
    {
      context.Veto();
      return ValueTask.CompletedTask;
    }
    return next(context);
  }
}

/// <summary>A concrete <see cref="ILocator"/> for tests.</summary>
internal sealed record Request(
  Type Target, IArgs? Args, IReadOnlyList<ITarget>? Chain = null)
  : ILocator
{
  private IReadOnlyList<ITarget>? _nodes;

  private IReadOnlyList<ITarget> Nodes
    => _nodes ??= Chain is { Count: > 0 } c ? c : [global::Everlong.Nester.Routing.Target.Of(Target, Args)];

  public IReadOnlyList<ITarget> Path => Nodes;
}

internal static class RouterTestHost
{
  internal static (FakeShell shell, TestRouter router) Create(Action<Exception>? reporter = null)
  {
    var shell = new FakeShell { ErrorReporter = reporter };
    var router = new TestRouter(shell);
    return (shell, router);
  }
}

/// <summary>A router on the fake shell — leases plain test layers.</summary>
internal class TestRouter : RouterBase
{
  internal TestRouter(IShell shell)
    : base(shell.Services, new TestStackModel())
  {
  }

  internal TestRouter(IShell shell, IServiceProvider services)
    : base(services, new TestStackModel())
  {
  }

  internal TestRouter(IShell shell, IRoutingView view)
    : base(shell.Services, new TestStackModel(view))
  {
  }

  internal TestRouter(IShell shell, IServiceProvider services, IRoutingView view)
    : base(services, new TestStackModel(view))
  {
  }

  internal TestRouter(IShell shell, Func<IConvergenceScene, Task>? reveal)
    : this(shell, new TestNavigationView(reveal: reveal))
  {
  }

  internal TestRouter(IShell shell, IServiceProvider services, Func<IConvergenceScene, Task>? reveal)
    : this(shell, services, new TestNavigationView(reveal: reveal))
  {
  }

  private readonly Action<IConvergenceContext>? _observe;

  internal TestRouter(IShell shell, Action<IConvergenceContext>? observe)
    : this(shell)
  {
    _observe = observe;
  }

  internal TestRouter(IShell shell, IServiceProvider services, Action<IConvergenceContext>? observe)
    : this(shell, services)
  {
    _observe = observe;
  }

  /// <inheritdoc />
  protected override Task OnConvergenceAsync(IConvergenceContext ctx, ConvergeNext next)
  {
    _observe?.Invoke(ctx);
    return next(ctx);
  }
}

/// <summary>A concrete model for tests.</summary>
internal sealed class TestStackModel : RouterStack
{
  internal TestStackModel(IRoutingView? view = null) => View = view ?? new TestNavigationView();

  /// <inheritdoc />
  protected internal override IRoutingView View { get; }

  /// <inheritdoc />
  protected internal override Location CreateLocation(Type type, object instance, IArgs? args)
    => new TestLocation(type, args, instance);
}

/// <summary>A concrete node for tests — the view slot typed as <see langword="object" />.</summary>
internal sealed class TestLocation(Type type, IArgs? args, object instance) : Location(type, args, instance)
{
  /// <summary>The assembled view — the typed face of the base visual slot.</summary>
  public object? View { get => Presenter; set => Presenter = value; }
}

/// <summary>
///   A test navigation view — carries the status and runs optional
///   platform-stage stand-ins (assemble and reveal).
/// </summary>
internal sealed class TestNavigationView : IRoutingView
{
  private readonly Action<IReadOnlyList<Location>>? _assemble;
  private readonly Func<IConvergenceScene, Task>? _reveal;

  internal TestNavigationView(Action<IReadOnlyList<Location>>? assemble = null,
                              Func<IConvergenceScene, Task>? reveal = null)
  {
    _assemble = assemble;
    _reveal = reveal;
  }

  public ILocation? Location { get; private set; }
  public IRouter? Router { get; private set; }

  void IRoutingView.SetLocation(ILocation? location) => Location = location;

  void IRoutingView.SetRouter(IRouter? router) => Router = router;

  public void AssembleViews(IReadOnlyList<Location> chain) => _assemble?.Invoke(chain);

  public Task RevealAsync(IConvergenceScene scene) => _reveal?.Invoke(scene) ?? Task.CompletedTask;

  public void ReleaseViews(IReadOnlyList<Location> released)
  {
  }
}
