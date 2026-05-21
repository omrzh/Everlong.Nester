using System.Collections;
using Everlong.Nester.Intent;
using Everlong.Nester.Layer;
using Everlong.Nester.Notice;
using Everlong.Nester.Shell;
using Everlong.Nester.Threading;
using Everlong.Nester.Tests.Layer;
using Xunit;

namespace Everlong.Nester.Tests.Notice;

/// <summary>
///   <see cref="NoticeServiceBase"/> delivery contract: the show
///   action runs exactly once — inline when unbound or already on the main
///   thread, deferred via <see cref="MainDispatcher.TryPost"/> otherwise —
///   and the notice host is always ensured before the show.
/// </summary>
[Collection("RealShell")]
public class NoticeServiceBaseTests : IDisposable
{
  public NoticeServiceBaseTests()
  {
    MainDispatcher.ResetForTesting();
  }

  public void Dispose()
  {
    MainDispatcher.ResetForTesting();
  }

  [Fact]
  public void Toast_Unbound_RunsShowExactlyOnce_AfterEnsuringHosts()
  {
    var trace = new List<string>();
    var service = NewService(trace);

    service.Toast("hello");

    // Inline path: host first, then the show — exactly once each.
    Assert.Equal(new[] { "acquire", "show" }, trace);
  }

  [Fact]
  public void Toast_CrossThread_PostsOneDeferredShow_ThatEnsuresHosts()
  {
    var dispatcher = new StubDispatcher();
    MainDispatcher.BindInstance(dispatcher);
    var trace = new List<string>();
    var service = NewService(trace);

    service.Toast("hello");

    Assert.Empty(trace); // deferred — nothing ran on this thread
    Assert.Single(dispatcher.Posted);

    dispatcher.RunPosted();

    Assert.Equal(new[] { "acquire", "show" }, trace); // host ensured before the show
  }

  private static FakeNoticeService NewService(List<string> trace)
  {
    var shell = new FakeShell(trace);
    var engine = new TracingEngine(trace);
    return new FakeNoticeService(shell, engine, new NoticeServiceOptions());
  }

  /// <summary>The notice tenant under test — the real base, the real delivery path.</summary>
  private sealed class FakeNoticeService(ILayerBroker broker, INoticeEngine engine, NoticeServiceOptions options)
    : NoticeServiceBase(broker, engine, options)
  {
    protected override object CreateHost() => new object();
  }

  /// <summary>Records show calls in the shared trace.</summary>
  private sealed class TracingEngine(List<string> trace) : INoticeEngine
  {
    public IEnumerable ToastEntries => Array.Empty<object>();
    public IEnumerable SnackbarEntries => Array.Empty<object>();
    public IEnumerable BannerEntries => Array.Empty<object>();

    public void ShowToast(ToastEntry entry, NoticeServiceOptions options) => trace.Add("show");

    public void ShowSnackbar(SnackbarEntry entry, NoticeServiceOptions options) => trace.Add("show");

    public void ShowNotification(NotificationEntry entry, NoticeServiceOptions options) => trace.Add("show");
  }

  /// <summary>Minimal shell: the broker is real (TestBrokerCore); the rest never runs here.</summary>
  private sealed class FakeShell(List<string> trace) : IShell, ILayerBroker
  {
    private readonly TestBrokerCore _broker = new();

    public IServiceProvider Services => throw new NotSupportedException();

    public IShellLifetime Lifetime { get; } = new FakeLifetime();

    private sealed class FakeLifetime : IShellLifetime
    {
      public ShellLifecycle Lifecycle => ShellLifecycle.Started;
      public Task Startup => Task.CompletedTask;
      public CancellationToken Stopping => CancellationToken.None;
      public CancellationToken Stopped => CancellationToken.None;
    }

    public object? Stage => null;

    public nint HostHandle => 0;

    public T? GetPlatformService<T>() where T : class => null;

    public void Start() => throw new NotSupportedException();

    public ValueTask<IntentResult> DispatchIntent(object? sender, IIntent intent) => throw new NotSupportedException();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public ILayerLease Acquire(ILayerTenant tenant, object content, LayerBand band, LayerPolicy policy)
    {
      trace.Add("acquire");
      return _broker.Acquire(tenant, content, band, policy);
    }

    public ILayerLease Acquire(ILayerTenant tenant, object content, int z)
    {
      trace.Add("acquire");
      return _broker.Acquire(tenant, content, z);
    }

    public void ReportError(Exception exception)
    {

    }
  }

  /// <summary>Off-main-thread dispatcher stub: records posted work, runs it on demand.</summary>
  private sealed class StubDispatcher : IMainDispatcher
  {
    private readonly List<Action> _posted = [];

    public bool CheckAccess() => false;

    public void Post(Action action, DispatchPriority priority = DispatchPriority.Normal)
      => _posted.Add(action);

    public Task InvokeAsync(Action action, DispatchPriority priority = DispatchPriority.Normal)
    {
      _posted.Add(action);
      return Task.CompletedTask;
    }

    public Task InvokeAsync(Func<Task> callback, DispatchPriority priority = DispatchPriority.Normal)
    {
      _posted.Add(() => callback());
      return Task.CompletedTask;
    }

    public IReadOnlyList<Action> Posted => _posted;

    public void RunPosted()
    {
      foreach (var action in _posted)
      {
        action();
      }
    }
  }
}
