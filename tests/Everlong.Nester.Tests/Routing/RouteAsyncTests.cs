using Everlong.Nester.Routing;
using Xunit;

using Everlong.DI;
using Everlong.Nester.Intent;
using Everlong.Nester.Shell;
namespace Everlong.Nester.Tests.Routing;

public class RouteAsyncTests
{
  private static Request ChainOf(Type page, Args? args = null)
    => new(page, args, [Target.Of(page, args)]);

  [Fact]
  public async Task CollectionExpression_BuildsARoute()
  {
    // [nodeA, nodeB] compiles to a Locator through LocatorBuilder — the
    // collection-expression entry of the routing domain.
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();

    Locator route = [Target.Of(typeof(LayoutAlpha)), Target.Of(typeof(TestContent))];
    Assert.Equal(2, route.Path.Count);
    Assert.Equal(typeof(LayoutAlpha), route.Path[0].Type);

    await router.RouteAsync(route);
    Assert.IsType<TestContent>(router.Model!.Current);
  }

  [Fact]
  public async Task RouteAsync_EntersTarget_DeliversArgs()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(ChainOf(typeof(TestContent), new TestArgs("root")));

    var content = (TestContent)router.Model!.Current!;
    Assert.Equal(1, router.Stack.Count);
    Assert.Equal("root", Assert.IsType<TestArgs>(content.ReceivedArgs).Value);
    Assert.True(content.Arrived);
    Assert.Same(content, router.View.Location!.Instance);
    Assert.Contains(shell.Leases, lease => ReferenceEquals(lease.Content, router.View));
  }

  [Fact]
  public async Task RouteAsync_SameTypeSharesInstance()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(ChainOf(typeof(TestContent)));
    object first = router.Model!.Current!;

    // The identical chain is fully shared — no new instance.
    await router.RouteAsync(ChainOf(typeof(TestContent)));
    Assert.Same(first, router.Model.Current);
  }

  [Fact]
  public async Task RouteAsync_BackRestoresInstance_AndForward()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(ChainOf(typeof(PageAlpha)));
    object pageA = router.Model!.Current!;
    await router.RouteAsync(ChainOf(typeof(PageBeta)));   // different type → fresh
    object pageB = router.Model.Current!;
    Assert.NotSame(pageA, pageB);

    // Back restores the exact held instance (retain) — the outgoing page
    // is notified departed (back/forward is a traversal with lifecycle,
    // but the forward trail keeps its instances).
    Assert.Equal(IntentResult.Handled, await shell.DispatchIntent(null, new BackIntent()));
    Assert.Same(pageA, router.Model.Current);
    Assert.Same(pageA, router.View.Location!.Instance);
    Assert.True(((TestContent)pageB).Departing);
    Assert.True(((TestContent)pageB).Departed);

    // Forward moves on.
    Assert.Equal(IntentResult.Handled, await shell.DispatchIntent(null, new ForwardIntent()));
    Assert.Same(pageB, router.Model.Current);
    Assert.Same(pageB, router.View.Location!.Instance);
  }

  [Fact]
  public async Task RouteAsync_NewRoute_PushesForwardTrailAway()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(ChainOf(typeof(PageAlpha)));
    await router.RouteAsync(ChainOf(typeof(PageBeta)));
    object b = router.Model!.Current!;
    await shell.DispatchIntent(null, new BackIntent());   // current = pageA
    Assert.True(router.Model.CanGoForward);

    // A new route to the forward location lands a fresh entry and clears
    // the forward trail (the same-node equal re-route would be a no-op).
    await router.RouteAsync(ChainOf(typeof(PageBeta)));
    Assert.False(router.Model.CanGoForward);
    Assert.Equal(2, router.Stack.Count);
    Assert.Same(b, router.Model.Current);
  }

  [Fact]
  public async Task RouteAsync_NoHistory_BackConsumedWithoutBlowing()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(ChainOf(typeof(TestContent)));
    object a = router.Model!.Current!;

    Assert.Equal(IntentResult.Handled, await shell.DispatchIntent(null, new BackIntent()));
    Assert.Same(a, router.Model.Current);   // the ground is not blown away
  }

  [Fact]
  public async Task RouteAsync_DeliversArgs()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var args = new TestArgs("pay");
    await router.RouteAsync(new Request(typeof(TestContent), args));

    var content = (TestContent)router.Model!.Current!;
    Assert.Same(args, content.ReceivedArgs);
  }

  [Fact]
  public async Task RouteAsync_UnresolvableTarget_FaultsTheCaller()
  {
    var reported = new List<Exception>();
    (FakeShell shell, TestRouter router) = RouterTestHost.Create(reporter: reported.Add);

    // Nothing resolves the type — the pre-commit fault surfaces to the
    // caller's await instead of the error channel.
    var failure = await Assert.ThrowsAsync<InvalidOperationException>(
      () => router.RouteAsync(ChainOf(typeof(IRoutable))));

    Assert.Contains("No instance resolved", failure.Message);
    Assert.Empty(reported);
    Assert.Equal(0, router.Stack.Count);   // nothing committed
  }

  [Fact]
  public async Task RouteAsync_UnresolvedByContainer_LandsThroughTheResolveSeam()
  {
    (FakeShell shell, TestRouter _) = RouterTestHost.Create();
    var router = new SeamRouter(shell);

    await router.RouteAsync(ChainOf(typeof(ISeamContent)));

    Assert.Equal(typeof(ISeamContent), router.AskedType);
    Assert.IsType<SeamContent>(router.Model!.Current!);
  }

  [Fact]
  public async Task RouteAsync_ContainerResolved_SkipsTheResolveSeam()
  {
    (FakeShell shell, TestRouter _) = RouterTestHost.Create();
    var router = new SeamRouter(shell);

    await router.RouteAsync(ChainOf(typeof(TestContent)));

    Assert.Null(router.AskedType);
  }

  [Fact]
  public async Task RouteAsync_InjectableWithoutInjector_FaultsTheCaller()
  {
    var reported = new List<Exception>();
    (FakeShell shell, TestRouter router) = RouterTestHost.Create(reporter: reported.Add);

    // The protocol claim without its driver — a pre-commit fault to the caller.
    var failure = await Assert.ThrowsAsync<InvalidOperationException>(
      () => router.RouteAsync(ChainOf(typeof(InjectedContent))));

    Assert.Contains("IInjectable", failure.Message);
    Assert.Empty(reported);
    Assert.Equal(0, router.Stack.Count);   // nothing committed
  }

  [Fact]
  public async Task RouteAsync_InjectableParticipant_MemberInjectionHonored()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var injector = new RecordingInjector(shell.Provider);
    shell.Provider.Register<IInjector>(injector);

    await router.RouteAsync(ChainOf(typeof(InjectedContent)));

    var page = Assert.IsType<InjectedContent>(router.Model!.Current!);
    Assert.True(page.Injected);                      // the protocol ran at materialization
    Assert.Same(page, injector.Injected.Single());   // the injector drove the claim
  }

  [Fact]
  public async Task RouteAsync_ThrowingDelivery_FaultsTheCaller_Uncommitted()
  {
    var reported = new List<Exception>();
    (FakeShell shell, TestRouter router) = RouterTestHost.Create(reporter: reported.Add);
    int navigated = 0;
    router.Stack.PropertyChanged += (_, e) =>
    {
      if (e.PropertyName == nameof(IRouterStack.Location))
        navigated++;
    };

    // The compute-phase fault — the requester's await, not the error channel.
    var failure = await Assert.ThrowsAsync<InvalidOperationException>(
      () => router.RouteAsync(ChainOf(typeof(ThrowingDeliveryContent), new TestArgs("x"))));

    Assert.Contains("Delivery rejected.", failure.Message);
    Assert.Empty(reported);            // nothing rides the error channel
    Assert.Equal(0, navigated);        // nothing published — no phantom navigation
    Assert.Equal(0, router.Stack.Count);   // nothing committed
  }

  [Fact]
  public async Task RouteAsync_ThrowingMidListDelivery_AbortsTheTransaction()
  {
    var reported = new List<Exception>();
    (FakeShell shell, TestRouter router) = RouterTestHost.Create(reporter: reported.Add);

    var a = new TestContent();
    await router.RouteAsync(new Request(typeof(ConditionalDeliveryContent), null,
      [Target.Of(typeof(TestContent), new TestArgs("a1"), a),
       Target.Of(typeof(ConditionalDeliveryContent), new TestArgs("b1"))]));

    var b = (ConditionalDeliveryContent)router.Model!.Current!;
    b.ThrowOnNextDelivery = new InvalidOperationException("Mid-list delivery rejected.");

    // The delivery runs before the commit — the mid-list throw faults the
    // caller and nothing lands.  The earlier node was already delivered, so
    // its side effect stands (no rollback past user code).
    var failure = await Assert.ThrowsAsync<InvalidOperationException>(
      () => router.RouteAsync(new Request(typeof(ConditionalDeliveryContent), null,
        [Target.Of(typeof(TestContent), new TestArgs("a2")),
         Target.Of(typeof(ConditionalDeliveryContent), new TestArgs("b2"))])));

    Assert.Contains("Mid-list delivery rejected.", failure.Message);
    Assert.Empty(reported);                            // the fault rides the caller, not the channel
    Assert.Equal(1, router.Stack.Count);               // nothing committed
    Assert.Same(a, router.Model.CurrentChain![0].Instance);
    Assert.Equal(new TestArgs("a2"), a.ReceivedArgs);  // delivered before the throw — no rollback
    Assert.Equal(new TestArgs("a2"), router.Model.CurrentChain[0].Args);
    Assert.Same(b, router.Model.CurrentChain[1].Instance);
    Assert.Equal(new TestArgs("b1"), router.Model.CurrentChain[1].Args);  // never re-delivered
  }

  [Fact]
  public async Task RouteAsync_InstanceRider_GetsMemberInjection()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var injector = new RecordingInjector(shell.Provider);
    shell.Provider.Register<IInjector>(injector);

    // The caller supplies the context instance; the framework supplies its
    // dependencies — the rider walks the same injection gate as a resolved
    // participant.
    var rider = new InjectedContent();
    await router.RouteAsync(new Request(typeof(InjectedContent), null,
      [Target.Of(typeof(InjectedContent), instance: rider)]));

    Assert.Same(rider, router.Model!.Current);   // the rider itself landed
    Assert.True(rider.Injected);                  // and was completed by injection
  }

  [Fact]
  public async Task RoutedToNotifyThrow_AbortsTheTransaction_NothingCommits()
  {
    var reported = new List<Exception>();
    (FakeShell shell, TestRouter router) = RouterTestHost.Create(reporter: reported.Add);
    int navigated = 0;
    router.Stack.PropertyChanged += (_, e) =>
    {
      if (e.PropertyName == nameof(IRouterStack.Location))
        navigated++;
    };

    // A membership-edge throw is a pre-commit fault — the notify phase runs
    // before the commit, so nothing lands and nothing is published.
    var failure = await Assert.ThrowsAsync<InvalidOperationException>(
      () => router.RouteAsync(ChainOf(typeof(ThrowingOnRoutedTo))));

    Assert.Contains("routed-to boom", failure.Message);
    Assert.Empty(reported);            // the fault rides the requester's await
    Assert.Equal(0, navigated);        // the surface notification never ran
    Assert.Equal(0, router.Stack.Count);   // nothing committed
  }

  [Fact]
  public async Task RoutedToNotifyThrow_OnIntentDispatch_FaultsTheDispatchCaller()
  {
    var reported = new List<Exception>();
    (FakeShell shell, TestRouter router) = RouterTestHost.Create(reporter: reported.Add);

    // A truth-pipeline fault is not the router's to report or consume — it
    // propagates out of the intent dispatch into the chain's error handling
    // (the fake shell has no layers catch, so the caller sees it).
    var failure = await Assert.ThrowsAsync<InvalidOperationException>(
      () => shell.DispatchIntent(null, new RouteIntent(ChainOf(typeof(ThrowingOnRoutedTo)))).AsTask());

    Assert.Contains("routed-to boom", failure.Message);
    Assert.Empty(reported);            // the router did not report it
    Assert.Equal(0, router.Stack.Count);   // nothing committed
  }

  [Fact]
  public async Task RoutedFromNotifyThrow_OnTraversalIntent_FaultsTheDispatchCaller()
  {
    var reported = new List<Exception>();
    (FakeShell shell, TestRouter router) = RouterTestHost.Create(reporter: reported.Add);
    await router.RouteAsync(ChainOf(typeof(PageAlpha)));
    await router.RouteAsync(ChainOf(typeof(ThrowingOnRoutedFrom)));

    // A traversal intent's transaction is observed at its landing — a
    // truth-pipeline fault propagates out of the dispatch instead of dying
    // on an unobserved transaction (nothing else awaits it).
    var failure = await Assert.ThrowsAsync<InvalidOperationException>(
      () => shell.DispatchIntent(null, new BackIntent()).AsTask());

    Assert.Contains("routed-from boom", failure.Message);
    Assert.Empty(reported);            // the router did not report it
    Assert.Equal(2, router.Stack.Count);   // the traversal did not run
  }

  [Fact]
  public async Task BodyChangedNotifyThrow_AbortsTheTransaction_NothingCommits()
  {
    var reported = new List<Exception>();
    (FakeShell shell, TestRouter router) = RouterTestHost.Create(reporter: reported.Add);

    var failure = await Assert.ThrowsAsync<InvalidOperationException>(
      () => router.RouteAsync(ChainOf(typeof(ThrowingOnBodyChanged))));

    Assert.Contains("body boom", failure.Message);
    Assert.Empty(reported);
    Assert.Equal(0, router.Stack.Count);
  }

  [Fact]
  public async Task SurfaceSubscriberThrow_ReportsThroughTheErrorChannel_CommitStands()
  {
    var reported = new List<Exception>();
    (FakeShell shell, TestRouter router) = RouterTestHost.Create(reporter: reported.Add);
    var boom = new InvalidOperationException("surface boom");
    router.Stack.PropertyChanged += (_, _) => throw boom;

    // The surface raise is post-commit and isolated — a failing subscriber
    // reports through the error channel; the commit stands and the caller's
    // await completes normally.
    await router.RouteAsync(ChainOf(typeof(TestContent)));

    var e = Assert.Single(reported);
    Assert.Same(boom, e);              // reported as-is — no aggregation wrapper
    Assert.Equal(1, router.Stack.Count);   // the navigation committed
    Assert.IsType<TestContent>(router.Model!.Current);
  }

  /// <summary>A participant whose delivery always throws — the materialization fault shape.</summary>
  private sealed class ThrowingDeliveryContent : IParameterized
  {
    public IArgs? EngagedArgs => null;

    public void DeliverArgs(IArgs? args)
      => throw new InvalidOperationException("Delivery rejected.");
  }

  /// <summary>A participant whose delivery throws on demand — the mid-list fault shape.</summary>
  private sealed class ConditionalDeliveryContent : IAdaptiveParameterized
  {
    internal Exception? ThrowOnNextDelivery { get; set; }

    internal IArgs? Received { get; private set; }

    public IArgs? EngagedArgs => Received;

    public bool IsAdaptable(IArgs? requested) => true;

    public void DeliverArgs(IArgs? args)
    {
      if (ThrowOnNextDelivery is { } e)
      {
        ThrowOnNextDelivery = null;
        throw e;
      }

      Received = args;
    }
  }

  /// <summary>A participant whose membership edge always throws — the notify-phase fault shape.</summary>
  private sealed class ThrowingOnRoutedTo : IRoutable
  {
    public void OnRoutedTo(IRoutingContext context) => throw new InvalidOperationException("routed-to boom");

    public void OnRoutedFrom(IRoutingContext context)
    {
    }
  }

  /// <summary>A participant whose departure edge always throws — a traversal's leaving side faults.</summary>
  private sealed class ThrowingOnRoutedFrom : IRoutable
  {
    public void OnRoutedTo(IRoutingContext context)
    {
    }

    public void OnRoutedFrom(IRoutingContext context) => throw new InvalidOperationException("routed-from boom");
  }

  /// <summary>A participant whose body-change notification always throws.</summary>
  private sealed class ThrowingOnBodyChanged : IBodyChanged
  {
    public void OnBodyChanged(IReadOnlyList<object> bodyChain) => throw new InvalidOperationException("body boom");
  }

  /// <summary>
  ///   A participant claiming the injection protocol — by default the test
  ///   provider carries no driver for the claim.
  /// </summary>
  private sealed class InjectedContent : TestContent, IInjectable
  {
    internal bool Injected { get; private set; }

    public void Inject(IServiceProvider services) => Injected = true;
  }

  /// <summary>Drives the protocol the way a real injector does and records the drives.</summary>
  private sealed class RecordingInjector(IServiceProvider services) : IInjector
  {
    internal List<IInjectable> Injected { get; } = [];

    public void Inject(IInjectable instance)
    {
      Injected.Add(instance);
      instance.Inject(services);   // the drive — the instance's own members wire here
    }
  }

  /// <summary>
  ///   A participant only the seam can supply — the test provider constructs
  ///   any concrete type, so an interface shape is the one thing the
  ///   container cannot resolve.
  /// </summary>
  private interface ISeamContent;

  /// <summary>The seam's answer — the instance the chain lands on.</summary>
  private sealed class SeamContent : ISeamContent;

  /// <summary>A router that answers the resolve seam — records the ask, supplies only <see cref="SeamContent" />.</summary>
  private sealed class SeamRouter(IShell shell) : TestRouter(shell)
  {
    internal Type? AskedType { get; private set; }

    protected internal override object? ResolveParticipant(Type type)
    {
      AskedType = type;
      return type == typeof(ISeamContent) ? new SeamContent() : null;
    }
  }
}
