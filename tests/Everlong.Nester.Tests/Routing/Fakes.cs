using Everlong.Nester.Diagnostics;
using Everlong.Nester.Intent;
using Everlong.Nester.Layer;
using Everlong.Nester.Routing;
using Everlong.Nester.Shell;
using Everlong.Nester.Tests.Layer;
using Microsoft.Extensions.DependencyInjection;

namespace Everlong.Nester.Tests.Routing;

/// <summary>
///   A "give whatever's asked" provider: registered types resolve to their
///   registered instance or factory, anything else is created fresh.
/// </summary>
internal sealed class FakeServiceProvider : IServiceProvider
{
  private readonly FakeServiceProvider? _parent;
  private readonly Dictionary<Type, object> _explicit = [];
  private readonly Dictionary<Type, Func<IServiceProvider, object>> _factories = [];
  private readonly Dictionary<Type, object> _resolved = [];

  internal FakeServiceProvider(FakeServiceProvider? parent = null) => _parent = parent;

  internal void Register<T>(T instance) where T : class => _explicit[typeof(T)] = instance;

  internal void Register<T>(Func<IServiceProvider, object> factory) where T : class
    => _factories[typeof(T)] = factory;

  public object? GetService(Type serviceType) => Resolve(serviceType, this);

  /// <summary>Walks the provider chain — a factory is invoked with the requesting provider and its result cached on the requester, so a scope's seed and router are the same instances across resolutions.</summary>
  private object? Resolve(Type serviceType, FakeServiceProvider requester)
  {
    if (_explicit.TryGetValue(serviceType, out object? instance))
      return instance;
    if (_factories.TryGetValue(serviceType, out var factory))
    {
      if (requester._resolved.TryGetValue(serviceType, out object? cached))
        return cached;
      object? value = factory(requester);
      requester._resolved[serviceType] = value;
      return value;
    }
    if (_parent is not null)
      return _parent.Resolve(serviceType, requester);
    return serviceType.IsInterface || serviceType.IsAbstract
      ? null
      : Activator.CreateInstance(serviceType, nonPublic: true);
  }
}

/// <summary>The test scope — a child provider over the fake root.</summary>
internal sealed class FakeScope : IServiceScope
{
  private readonly IServiceProvider _provider;

  internal FakeScope(IServiceProvider provider) => _provider = provider;

  public IServiceProvider ServiceProvider => _provider;

  public void Dispose()
  {
  }
}

/// <summary>The test scope factory — each scope is a child provider.</summary>
internal sealed class FakeScopeFactory(FakeServiceProvider root) : IServiceScopeFactory
{
  public IServiceScope CreateScope() => new FakeScope(new FakeServiceProvider(root));
}

/// <summary>
///   The minimal shell the Routing domain tests run against, wired on the
///   test ledger (<see cref="TestBrokerCore" />, the same contract the
///   platform shells use): a tenant acquires a lease and the shell's
///   dispatch reaches it z-descending.  Backed by a give-whatever's-asked
///   provider.
/// </summary>
internal sealed class FakeShell : IShell, ILayerBroker
{
  private readonly TestBrokerCore _brokerCore = new();

  internal FakeServiceProvider Provider { get; } = new();

  /// <summary>The derived-router factory — defaults to a bare <see cref="TestRouter" />; tests override it to carry the same stages as their root router.</summary>
  internal Func<IServiceProvider, RouterBase>? RouterFactory { get; set; }

  internal FakeShell()
  {
    // The routing domain's DI surface — the seed is per-provider (a child
    // scope seeds its own), the scope factory is shared.
    Provider.Register<IShell>(this);
    Provider.Register<ILayerBroker>(this);
    Provider.Register<IErrorReporter>(this);
    Provider.Register<IIntentDispatcher>(this);
    Provider.Register<IRouterSeed>(_ => new RouterSeed());
    Provider.Register<IServiceScopeFactory>(new FakeScopeFactory(Provider));
    Provider.Register<IRouter>(sp => RouterFactory is { } factory
      ? factory(sp)
      : new TestRouter(sp.GetRequiredService<IShell>(), sp));
  }

  /// <summary>The routing tests' reporter channel — the router reports orchestration-boundary failures here.</summary>
  internal Action<Exception>? ErrorReporter { get; set; }

  public IServiceProvider Services => Provider;

  /// <summary>The fake shell is always started, never stops.</summary>
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

  public void Start()
  {
  }

  public void ReportError(Exception exception) => ErrorReporter?.Invoke(exception);

  public ValueTask<IntentResult> DispatchIntent(object? sender, IIntent intent)
    => _brokerCore.TryDispatch(new IntentContext(intent, sender));

  public ILayerLease Acquire(ILayerTenant tenant, object content, LayerBand band, LayerPolicy policy)
    => _brokerCore.Acquire(tenant, content, band, policy);

  public ILayerLease Acquire(ILayerTenant tenant, object content, int z)
    => _brokerCore.Acquire(tenant, content, z);

  /// <summary>The live leases in intent-dispatch order (test/observer channel).</summary>
  internal IEnumerable<ILayerLease> Leases => _brokerCore.BottomUp();

  public ValueTask DisposeAsync() => default;
}
