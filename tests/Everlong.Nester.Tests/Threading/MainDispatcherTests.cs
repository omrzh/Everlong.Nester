using Everlong.Nester.Threading;
using Xunit;

namespace Everlong.Nester.Tests.Threading;

/// <summary>
///   Facade contracts: <see cref="MainDispatcher.TryPost"/> is the
///   degradable delivery entry — inline when unbound or already on the main
///   thread; <see cref="MainDispatcher.Post"/>/<see cref="MainDispatcher.InvokeAsync(Action, DispatchPriority)"/>/
///   <see cref="MainDispatcher.CheckAccess"/> are host-required (throw when
///   unbound); binding is a one-way door with an explicit test reset.
/// </summary>
[Collection("RealShell")]
public class MainDispatcherTests : IDisposable
{
  public MainDispatcherTests()
  {
    MainDispatcher.ResetForTesting();
  }

  public void Dispose()
  {
    MainDispatcher.ResetForTesting();
  }

  // ── The degradable delivery entry (TryPost) ──

  [Fact]
  public void TryPost_Unbound_RunsInline_AndReturnsFalse()
  {
    var ran = false;

    var posted = MainDispatcher.TryPost(() => ran = true);

    Assert.False(posted);
    Assert.True(ran);
  }

  [Fact]
  public void TryPost_Bound_OffMainThread_PostsAndReturnsTrue()
  {
    var dispatcher = new StubDispatcher(onMainThread: false);
    MainDispatcher.BindInstance(dispatcher);
    var ran = false;

    var posted = MainDispatcher.TryPost(() => ran = true);

    Assert.True(posted);
    Assert.False(ran); // deferred — the main thread runs it later
    Assert.Single(dispatcher.Posted);
  }

  [Fact]
  public void TryPost_Bound_OnMainThread_RunsInlineAndReturnsFalse()
  {
    var dispatcher = new StubDispatcher(onMainThread: true);
    MainDispatcher.BindInstance(dispatcher);
    var ran = false;

    var posted = MainDispatcher.TryPost(() => ran = true);

    Assert.False(posted);
    Assert.True(ran);
    Assert.Empty(dispatcher.Posted);
  }

  // ── Host-required entries throw when unbound ──

  [Fact]
  public void Post_Unbound_Throws()
  {
    Assert.Throws<InvalidOperationException>(() => MainDispatcher.Post(() => { }));
  }

  [Fact]
  public void InvokeAsync_Unbound_Throws()
  {
    // The throw is synchronous (before any task is created) — the Action
    // local keeps the delegate out of the Throws overload set.
    Action act = () => MainDispatcher.InvokeAsync(() => { });
    Assert.Throws<InvalidOperationException>(act);
  }

  [Fact]
  public void CheckAccess_Unbound_Throws()
  {
    Assert.Throws<InvalidOperationException>(() => MainDispatcher.CheckAccess());
  }

  // ── Binding: one-way door + the test reset ──

  [Fact]
  public void BindInstance_DifferentInstance_Throws()
  {
    MainDispatcher.BindInstance(new StubDispatcher(onMainThread: true));

    Assert.Throws<InvalidOperationException>(
      () => MainDispatcher.BindInstance(new StubDispatcher(onMainThread: true)));
  }

  [Fact]
  public void ResetForTesting_ClearsTheBinding()
  {
    MainDispatcher.BindInstance(new StubDispatcher(onMainThread: true));
    MainDispatcher.ResetForTesting();

    Assert.False(MainDispatcher.TryGet(out _));
    var ran = false;
    Assert.False(MainDispatcher.TryPost(() => ran = true));
    Assert.True(ran);
  }

  /// <summary>Deterministic dispatcher stub — records posted work instead of running it.</summary>
  private sealed class StubDispatcher(bool onMainThread) : IMainDispatcher
  {
    public List<Action> Posted { get; } = [];

    public bool CheckAccess() => onMainThread;

    public void Post(Action action, DispatchPriority priority = DispatchPriority.Normal)
      => Posted.Add(action);

    public Task InvokeAsync(Action action, DispatchPriority priority = DispatchPriority.Normal)
    {
      Posted.Add(action);
      return Task.CompletedTask;
    }

    public Task InvokeAsync(Func<Task> callback, DispatchPriority priority = DispatchPriority.Normal)
      => Task.CompletedTask;
  }
}
