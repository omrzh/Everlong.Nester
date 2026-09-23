using Everlong.Nester.Routing;
using Xunit;

namespace Everlong.Nester.Tests.Routing;

/// <summary>
///   The transfer's moving sides (`docs/design/routing.md`): the arriving
///   side spans from the first positional difference down to the target, so a
///   revision re-engages the node it changed and everything below it, while
///   the prefix above the change is never re-entered.
/// </summary>
public class MovingSidesTests
{
  /// <summary>An adaptive layout that records the ceremony it receives.</summary>
  private sealed class AdaptiveChrome : IAdaptiveParameterized, IArriving, IArrived, IRoutable
  {
    private IArgs? _engaged;

    public IArgs? EngagedArgs => _engaged;

    internal int Arriving { get; private set; }

    internal int Arrived { get; private set; }

    internal int RoutedFrom { get; private set; }

    bool IAdaptiveParameterized.IsAdaptable(IArgs? requested) => true;

    void IParameterized.DeliverArgs(IArgs? args) => _engaged = args;

    Task IArriving.OnArrivingAsync(IRoutingContext context)
    {
      Arriving++;
      return Task.CompletedTask;
    }

    Task IArrived.OnArrivedAsync(IRoutingContext context)
    {
      Arrived++;
      return Task.CompletedTask;
    }

    void IRoutable.OnRoutedTo(IRoutingContext context)
    {
    }

    void IRoutable.OnRoutedFrom(IRoutingContext context) => RoutedFrom++;
  }

  /// <summary>A rigid leaf that records the ceremony it receives.</summary>
  private sealed class RecordingLeaf : IArriving, IArrived, IRoutable
  {
    internal int Arriving { get; private set; }

    internal int Arrived { get; private set; }

    internal int RoutedFrom { get; private set; }

    Task IArriving.OnArrivingAsync(IRoutingContext context)
    {
      Arriving++;
      return Task.CompletedTask;
    }

    Task IArrived.OnArrivedAsync(IRoutingContext context)
    {
      Arrived++;
      return Task.CompletedTask;
    }

    void IRoutable.OnRoutedTo(IRoutingContext context)
    {
    }

    void IRoutable.OnRoutedFrom(IRoutingContext context) => RoutedFrom++;
  }

  /// <summary>An adaptive leaf that records the ceremony it receives.</summary>
  private sealed class AdaptiveLeaf : IAdaptiveParameterized, IArriving, IArrived
  {
    private IArgs? _engaged;

    public IArgs? EngagedArgs => _engaged;

    internal int Arriving { get; private set; }

    internal int Arrived { get; private set; }

    bool IAdaptiveParameterized.IsAdaptable(IArgs? requested) => true;

    void IParameterized.DeliverArgs(IArgs? args) => _engaged = args;

    Task IArriving.OnArrivingAsync(IRoutingContext context)
    {
      Arriving++;
      return Task.CompletedTask;
    }

    Task IArrived.OnArrivedAsync(IRoutingContext context)
    {
      Arrived++;
      return Task.CompletedTask;
    }
  }

  private static Request Chained(Type leaf, IArgs? chromeArgs = null, IArgs? leafArgs = null)
    => new(leaf, leafArgs,
           [Target.Of(typeof(AdaptiveChrome), chromeArgs), Target.Of(leaf, leafArgs)]);

  [Fact]
  public async Task Absorb_AncestorRevision_ReengagesItsDescendants()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var chrome = new AdaptiveChrome();
    var leaf = new RecordingLeaf();
    await router.RouteAsync(new Request(typeof(RecordingLeaf), null,
      [Target.Of(typeof(AdaptiveChrome), new TestArgs("f1"), chrome),
       Target.Of(typeof(RecordingLeaf), null, leaf)]));

    Assert.Equal(1, leaf.Arrived);
    Assert.Equal(1, chrome.Arrived);

    // The ancestor is revised in place; the leaf's own request is unchanged.
    await router.RouteAsync(Chained(typeof(RecordingLeaf), new TestArgs("f2")));

    Assert.Same(leaf, router.Model!.CurrentChain![1].Instance);
    Assert.Equal(0, leaf.RoutedFrom);                  // it never left the chain
    Assert.Equal(2, chrome.Arrived);                   // the revised node replays too

    // The moving suffix spans from the first change down — the leaf is
    // re-engaged and replays its arrival.
    Assert.Equal(2, leaf.Arriving);
    Assert.Equal(2, leaf.Arrived);
  }

  [Fact]
  public async Task Absorb_LeafRevision_LeavesThePrefixUntouched()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var chrome = new AdaptiveChrome();
    var leaf = new AdaptiveLeaf();
    await router.RouteAsync(new Request(typeof(AdaptiveLeaf), new TestArgs("a1"),
      [Target.Of(typeof(AdaptiveChrome), new TestArgs("f1"), chrome),
       Target.Of(typeof(AdaptiveLeaf), new TestArgs("a1"), leaf)]));

    await router.RouteAsync(Chained(typeof(AdaptiveLeaf), new TestArgs("f1"), new TestArgs("a2")));

    // The change sits at the leaf, so the prefix above it is not re-entered.
    Assert.Equal(1, chrome.Arriving);
    Assert.Equal(2, leaf.Arriving);
    Assert.Equal(2, leaf.Arrived);
  }

  [Fact]
  public async Task Refresh_ReengagesTheWholeChain()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var chrome = new AdaptiveChrome();
    var leaf = new RecordingLeaf();
    await router.RouteAsync(new Request(typeof(RecordingLeaf), null,
      [Target.Of(typeof(AdaptiveChrome), new TestArgs("f1"), chrome),
       Target.Of(typeof(RecordingLeaf), null, leaf)]));

    await shell.DispatchIntent(null, new RefreshIntent());
    await router.WaitIdleAsync();

    // A refresh re-engages every position: nothing departs, nothing mounts.
    Assert.Equal(2, chrome.Arriving);
    Assert.Equal(2, leaf.Arriving);
    Assert.Equal(0, chrome.RoutedFrom);
    Assert.Equal(0, leaf.RoutedFrom);
  }
}
