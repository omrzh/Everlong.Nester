using Everlong.Nester.Threading;
using Everlong.Nester.Routing;
using Xunit;

namespace Everlong.Nester.Tests.Routing;

/// <summary>
///   The routing-entry UI-thread bridge: a route/present invoked off the
///   UI thread is bridged to it before it runs (the model is created and
///   the pipeline runs on the main thread); on the main thread the call
///   runs inline; without a bound dispatcher (harness / headless) the call
///   runs directly.
/// </summary>
[Collection("RealShell")]
public class RouterThreadingTests : IDisposable
{
  /// <summary>
  ///   A dispatcher that "lands" on the main thread inside a bridged
  ///   callback — mirrors the real platform dispatcher where the callback
  ///   executes with <see cref="CheckAccess"/> true.
  /// </summary>
  private sealed class LandingDispatcher : IMainDispatcher
  {
    private readonly bool _startsOnMainThread;

    internal LandingDispatcher(bool startsOnMainThread) => _startsOnMainThread = startsOnMainThread;

    internal int InvokeCount { get; private set; }

    public bool CheckAccess() => _startsOnMainThread || _insideBridge;

    private bool _insideBridge;

    public void Post(Action action, DispatchPriority priority = DispatchPriority.Normal)
      => throw new NotSupportedException();

    public Task InvokeAsync(Action action, DispatchPriority priority = DispatchPriority.Normal)
    {
      InvokeCount++;
      _insideBridge = true;
      try
      {
        action();
        return Task.CompletedTask;
      }
      finally
      {
        _insideBridge = false;
      }
    }

    public Task InvokeAsync(Func<Task> callback, DispatchPriority priority = DispatchPriority.Normal)
    {
      InvokeCount++;
      _insideBridge = true;
      try
      {
        return callback();
      }
      finally
      {
        _insideBridge = false;
      }
    }
  }

  public RouterThreadingTests()
  {
    MainDispatcher.ResetForTesting();
  }

  public void Dispose()
  {
    MainDispatcher.ResetForTesting();
  }

  private static Request PageRequest()
    => new(typeof(TestContent), null);

  [Fact]
  public async Task RouteAsync_FromNonMainThread_BridgesToMainThread()
  {
    var dispatcher = new LandingDispatcher(startsOnMainThread: false);
    MainDispatcher.BindInstance(dispatcher);
    (_, TestRouter router) = RouterTestHost.Create();

    await router.RouteAsync(PageRequest());

    Assert.True(dispatcher.InvokeCount >= 1);   // the call was bridged
    Assert.NotNull(router.Model!.Current);      // and the route ran on the "main" side
  }

  [Fact]
  public async Task PresentAsync_FromNonMainThread_BridgesAndReturnsModel()
  {
    var dispatcher = new LandingDispatcher(startsOnMainThread: false);
    MainDispatcher.BindInstance(dispatcher);
    (_, TestRouter router) = RouterTestHost.Create();

    IRouter present = router.Derive();
    await present.RouteAsync(PageRequest());

    Assert.True(dispatcher.InvokeCount >= 1);
    present.Completion!.Complete("ok");
    Assert.Equal("ok", await present.Completion!);
  }

  [Fact]
  public async Task RouteAsync_FromMainThread_RunsInline()
  {
    var dispatcher = new LandingDispatcher(startsOnMainThread: true);
    MainDispatcher.BindInstance(dispatcher);
    (_, TestRouter router) = RouterTestHost.Create();

    await router.RouteAsync(PageRequest());

    Assert.Equal(0, dispatcher.InvokeCount);   // no bridge on the main thread
    Assert.NotNull(router.Model!.Current);
  }

  [Fact]
  public async Task RouteAsync_WithoutDispatcher_RunsDirectly()
  {
    (_, TestRouter router) = RouterTestHost.Create();

    await router.RouteAsync(PageRequest());

    Assert.NotNull(router.Model!.Current);
  }

  /// <summary>A route whose target is a distinct instance — each push lands a fresh entry.</summary>
  private static Request DistinctPage()
    => new(typeof(TestContent), null, [Target.Of(typeof(TestContent), instance: new TestContent())]);

  [Fact]
  public async Task Trim_FromNonMainThread_Throws()
  {
    var dispatcher = new LandingDispatcher(startsOnMainThread: false);
    MainDispatcher.BindInstance(dispatcher);
    (_, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(DistinctPage());
    await router.RouteAsync(DistinctPage());   // a back entry exists

    Assert.Throws<InvalidOperationException>(() => router.Model!.TrimBackward());
  }

  [Fact]
  public async Task Trim_FromMainThread_RunsInline()
  {
    var dispatcher = new LandingDispatcher(startsOnMainThread: true);
    MainDispatcher.BindInstance(dispatcher);
    (_, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(DistinctPage());
    await router.RouteAsync(DistinctPage());

    Assert.True(router.Model!.TrimBackward());   // runs on the "main" side, no bridge
    Assert.False(router.Model.CanGoBack);
  }

  [Fact]
  public async Task Trim_WithoutDispatcher_RunsDirectly()
  {
    (_, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(DistinctPage());
    await router.RouteAsync(DistinctPage());

    Assert.True(router.Model!.TrimBackward());   // headless — the trim degrades to the caller's thread
    Assert.False(router.Model.CanGoBack);
  }

  [Fact]
  public async Task Teardown_FromNonMainThread_Throws()
  {
    var dispatcher = new LandingDispatcher(startsOnMainThread: false);
    MainDispatcher.BindInstance(dispatcher);
    (_, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(PageRequest());

    Assert.Throws<InvalidOperationException>(() => router.CloseSync(null));
  }

  [Fact]
  public async Task Teardown_OnTheMainThread_RunsInline()
  {
    var dispatcher = new LandingDispatcher(startsOnMainThread: true);
    MainDispatcher.BindInstance(dispatcher);
    (_, TestRouter router) = RouterTestHost.Create();
    await router.RouteAsync(PageRequest());

    router.CloseSync(null);

    Assert.True(router.Model!.IsClosed);
  }
}
