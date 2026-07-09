using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Everlong.DI;
using Everlong.Nester.Hosting;
using Everlong.Nester.Intent;
using Everlong.Nester.Shell;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Everlong.Nester.Routing;
using Everlong.Nester.Tests.Hosting;
using Everlong.Nester.Controls;
using PlatformControl = Avalonia.Controls.Control;

namespace Everlong.Nester.Tests.Shell;

/// <summary>
///   Verifies the shell creation contract (the user-owned shell shape —
///   <see cref="TestHost"/> / <see cref="Everlong.Nester.Tests.Hosting.TestShell{TDirector}"/>):
///   one shell = one own container, assembled synchronously — presenting the shell is the caller's.
/// </summary>
public class ShellCreationTests
{
  // ── Scene types ──────────────────────────────────────────

  public interface ITestMarker;

  public sealed class TestMarker : ITestMarker;

  /// <summary>The shell ViewModel (Director). Host-created (new() — deliberately has no parameterless ctor problems: it IS parameterless) — member-injected after the window container exists.</summary>
  public sealed class TestContext : IShellDirector, IInjectable
  {
    public ITestMarker Id { get; private set; } = null!;

    public bool OnShellReadyCalled { get; private set; }
    public List<IIntent> Intents { get; } = [];

    public void Inject(IServiceProvider services)
    {
      Id = services.GetRequiredService<ITestMarker>();
    }


    public ValueTask HandleAsync(IntentContext context, IntentDelegate next)
    {
      Intents.Add(context.Intent);
      OnShellReadyCalled = true;   // the startup dispatch reached the Director
      context.Handle();
      return ValueTask.CompletedTask;
    }

    public void OnAssembled(IShell shell) { }
    public bool HandleError(Exception exception) => true;
  }

  private sealed record TestIntent : IIntent;

  /// <summary>
  ///   Director with member injection (the template shape: ViewModelBase carries
  ///   <c>[Inject] IRouter</c>): its IRouter members
  ///   resolve after the window container exists — the framework injects the
  ///   Director during assembly, before the startup flow.
  /// </summary>
  public sealed class InjectableContext : IShellDirector, IInjectable
  {
    public IRouter? Router { get; private set; }
    public IRouter? Dialogs { get; private set; }
    public bool OnShellReadyCalled { get; private set; }

    public void Inject(IServiceProvider services)
    {
      Router = services.GetService<IRouter>();
      Dialogs = services.GetService<IRouter>();
    }


    public ValueTask HandleAsync(IntentContext context, IntentDelegate next)
    {
      OnShellReadyCalled = true;   // the startup dispatch reached the Director
      context.Handle();
      return ValueTask.CompletedTask;
    }

    public void OnAssembled(IShell shell) { }
    public bool HandleError(Exception exception) => true;
  }

  /// <summary>Director that handles nothing — lets the intent chain reach the host (③).</summary>
  public sealed class PassthroughContext : IShellDirector
  {
    public ValueTask HandleAsync(IntentContext context, IntentDelegate next) => next(context);
    public void OnAssembled(IShell shell) { }
    public bool HandleError(Exception exception) => true;
  }

  // ── Harness ──────────────────────────────────────────────

  private static void AddTestRegistrations(IServiceCollection services)
  {
    services.AddSingleton<ITestMarker, TestMarker>();
  }

  // ── the shell creation surface ────────────────────────

  [AvaloniaFact]
  public async Task TwoShells_ProduceIndependentContainers()
  {
    // Each shell builds and owns its own window container — independent
    // providers, scopes and Directors.
    IShell shellA = TestHost.CreateShell<TestContext>(AddTestRegistrations);
    IShell shellB = TestHost.CreateShell<TestContext>(AddTestRegistrations);

    Assert.NotSame(shellA, shellB);
    Assert.NotSame(shellA.Services, shellB.Services);

    // The Director is NOT resolved before start (the declarative resolution
    // happens in Start via DirectorType) — each shell resolves its own
    // instance once started.
    Assert.Null(((AvaloniaShell)shellA).Director);
    Assert.Null(((AvaloniaShell)shellB).Director);

    shellA.Start();
    shellB.Start();
    await shellA.Lifetime.Startup;
    await shellB.Lifetime.Startup;

    Assert.NotNull(((AvaloniaShell)shellA).Director);
    Assert.NotSame(((AvaloniaShell)shellA).Director, ((AvaloniaShell)shellB).Director);
  }

  [AvaloniaFact]
  public async Task DispatchIntent_BeforeBuild_IsInert()
  {
    var shell = new BareShell();

    Assert.Equal(IntentResult.Pass, await shell.DispatchIntent(null, new TestIntent()));
  }

  // ── full assembly ────────────────────────────────────

  [AvaloniaFact]
  public async Task Build_AssemblesRunningShell()
  {

    IShell shell = TestHost.CreateShell<TestContext>(AddTestRegistrations);
    shell.Start();
    await shell.Lifetime.Startup;   // the startup flow (Director coroutine + host presentation) — no host here
    var shellImpl = Assert.IsType<TestShell<TestContext>>(shell);

    // Director host-created, member-injected (the marker resolves from the
    // window container), ready callback fired.
    var context = Assert.IsType<TestContext>(shellImpl.Director);
    Assert.True(context.OnShellReadyCalled);
    Assert.IsType<TestMarker>(context.Id);

    // The default host (TestHostView) built and bound to the Director.
    var holderView = Assert.IsType<TestHostView>(shellImpl.HostView);
    Assert.Same(context, holderView.DataContext);

    // Window-level identity: the shell self-registers into its own container.
    Assert.NotNull(shell.Services);
    Assert.Same(shell, shell.Services!.GetRequiredService<IShell>());
    Assert.Same(shell, shell.Services.GetRequiredService<TestShell<TestContext>>());
    Assert.IsType<TestMarker>(shell.Services.GetRequiredService<ITestMarker>());

    // The shell's lifetime projection is DI-injectable and live.
    var lifetime = shell.Services.GetRequiredService<IShellLifetime>();
    Assert.Same(shellImpl.Lifetime, lifetime);
    Assert.Equal(ShellLifecycle.Started, lifetime.Lifecycle);
    Assert.False(lifetime.Stopping.IsCancellationRequested);
    Assert.False(lifetime.Stopped.IsCancellationRequested);

    // The host-lifetime face is the same instance — a service observes
    // teardown through IHostLifetime without depending on the shell.
    Assert.Same(lifetime, shell.Services.GetRequiredService<IHostLifetime>());

    // Runtime surface is live: intents reach the Director after the stage.
    Assert.Equal(IntentResult.Handled, await shell.DispatchIntent(null, new TestIntent()));
    Assert.Contains(context.Intents, i => i is TestIntent);
  }

  [AvaloniaFact]
  public async Task Build_DirectorWithLayerInjection_ResolvesAfterContainerReady()
  {

    IShell shell = TestHost.CreateShell<InjectableContext>(AddTestRegistrations);
    shell.Start();
    await shell.Lifetime.Startup;   // the startup flow runs the Director coroutine
    var shellImpl = Assert.IsType<TestShell<InjectableContext>>(shell);
    var ceo = Assert.IsType<InjectableContext>(shellImpl.Director);

    // The Director's member injection ran during assembly (after the window
    // container exists): IRouter resolves
    // from the window container.
    Assert.True(ceo.OnShellReadyCalled);
    Assert.NotNull(ceo.Router);
    Assert.NotNull(ceo.Dialogs);
  }

  [AvaloniaFact]
  public async Task DisposeAsync_WithoutBuild_IsNoop()
  {
    // Failed boot / teardown of an unassembled shell must not dispose a
    // default AsyncServiceScope (.NET 10 throws on an empty one).
    var shell = new BareShell();

    await shell.DisposeAsync();
  }

  [AvaloniaFact]
  public async Task Start_WithoutAssembly_Throws()
  {
    // The startup contract: Start requires the assembly (typed members) first
    // — a clear guard, not an NRE on null providers.
    var shell = new BareShell();

    Assert.Throws<InvalidOperationException>(() => shell.Start());
  }

  [AvaloniaFact]
  public void Services_BeforeAssembly_Throws()
  {
    // The resolution face is live only after assembly — reading Services on
    // an unassembled shell fails fast; it never assembles implicitly.
    var shell = new BareShell();

    Assert.Throws<InvalidOperationException>(() => shell.Services);
  }

  [AvaloniaFact]
  public async Task DisposeAsync_Twice_IsIdempotent()
  {

    // The window-close path and the AppLifetime teardown chain both call
    // DisposeAsync — the second pass must be a no-op.
    IShell shell = TestHost.CreateShell<TestContext>(AddTestRegistrations);

    await ((AvaloniaShell)shell).DisposeAsync();
    await ((AvaloniaShell)shell).DisposeAsync();
  }

  [AvaloniaFact]
  public void PlatformFace_ShellDeclaresItsPlatformIdentity()
  {
    // The platform face inherits IShell: a View-side type that knows its
    // platform casts the IShell reference to the platform face and calls
    // platform members directly — IShell members stay available (inherited).
    IShell shell = TestHost.CreateShell<TestContext>(AddTestRegistrations);

    Assert.IsAssignableFrom<IAvaloniaShell>(shell);
    var face = Assert.IsAssignableFrom<IAvaloniaShell>(shell);
    Assert.Same(shell, face);            // same instance — the face is the shell itself
    Assert.NotNull(face.Lifetime);  // IShell member reachable through the face
  }

  // ── Host branch (ConnectHost) ────────────────────────

  /// <summary>Host view: provides a logical content layer (the framework mounts the stage into it); records intent fallback calls.</summary>
  private sealed class HostView : ContentControl, IAvaloniaShellHost, IIntentHandler
  {
    private readonly ContentLayer _contentLayer = new();

    public void HostShell(IAvaloniaShell shell, PlatformControl stage) => _contentLayer.Content = stage;

    /// <summary>The hosted surface (test/observer channel).</summary>
    internal ContentLayer ContentLayer => _contentLayer;

    public int HandleCount { get; private set; }

    public ValueTask HandleAsync(IntentContext context, IntentDelegate next)
    {
      HandleCount++;
      context.Handle();
      return ValueTask.CompletedTask;
    }
  }

  /// <summary>Host view that never wires its layer — legal now: the framework mounts logically and never depends on the host's visual readiness.</summary>
  private sealed class IgnoringHostView : ContentControl, IAvaloniaShellHost
  {
    private readonly ContentLayer _contentLayer = new();

    public void HostShell(IAvaloniaShell shell, PlatformControl stage) => _contentLayer.Content = stage;

    /// <summary>The hosted surface (test/observer channel).</summary>
    internal ContentLayer ContentLayer => _contentLayer;
  }

  /// <summary>Test shell recording the presentation hooks' invocation order.</summary>
  private sealed class RecordingShell : TestShell<TestContext>
  {
    public RecordingShell(ContentControl? rootView) : base(AddTestRegistrations, rootView) { }

    public bool OnAssembledCalled { get; private set; }
    public bool OnStartedCalled { get; private set; }
    private int _order;
    private int _initializedOrder;
    private int _startedOrder;

    protected override void OnAssembled()
    {
      OnAssembledCalled = true;
      _initializedOrder = ++_order;
    }

    protected override Task OnStarted(Task directorStart)
    {
      OnStartedCalled = true;
      _startedOrder = ++_order;
      return base.OnStarted(directorStart);
    }

    /// <summary>The synchronous presentation hook ran before the async chain started.</summary>
    public bool InitializedBeforeStarted => OnAssembledCalled && OnStartedCalled && _initializedOrder < _startedOrder;
  }

  [AvaloniaFact]
  public async Task Build_DesktopWithoutHost_FailsFast()
  {
    // Desktop (the headless harness has no single-view lifetime) requires
    // a host — the framework never falls back for uncooperative views:
    // fail fast at MountStage (now part of the Start shell-mount, not of
    // construction).
    IShell shell = TestHost.CreateShell<TestContext>(AddTestRegistrations, noHost: true);
    Assert.Throws<InvalidOperationException>(() => shell.Start());
  }

  [AvaloniaFact]
  public async Task Start_DesktopWithoutHost_FailedMountIsRetryable()
  {
    // The fail-fast must not brick the shell: after the user fixes the
    // host (per the error message), the same shell instance can start —
    // the mount flags are set only after the mount succeeded.
    var shell = TestHost.CreateShell<TestContext>(AddTestRegistrations, noHost: true);
    Assert.Throws<InvalidOperationException>(() => shell.Start());

    var impl = Assert.IsType<TestShell<TestContext>>(shell);
    impl.ReplaceHost(new TestHostView());

    shell.Start();
    await shell.Lifetime.Startup;
  }

  [AvaloniaFact]
  public async Task Start_WithHostView_PresentsSynchronouslyThenRunsPresentationChain()
  {
    var view = new HostView();
    var shell = new RecordingShell(view);
    shell.Start();   // the startup flow runs synchronously up to the presentation anchor

    // OnAssembled (the synchronous presentation anchor) runs inside
    // Start — before the async presentation chain (OnStarted).
    Assert.True(shell.OnAssembledCalled);
    Assert.True(shell.InitializedBeforeStarted);

    await shell.Lifetime.Startup;   // the presentation chain settles

    Assert.True(shell.OnStartedCalled);
    // The framework mounted the stage into the host's CONTENT LAYER (a
    // logical mount — the host never touches the stage itself).
    Assert.Same(shell.StagePanel, view.ContentLayer.Content);
  }

  [AvaloniaFact]
  public async Task Build_WithHostThatNeverWiresLayer_StillSucceeds()
  {
    var view = new IgnoringHostView();
    IShell shell = TestHost.CreateShell<TestContext>(AddTestRegistrations, rootView: view);
    // The host never wires its layer into a visual tree — legal: the
    // framework mounts logically (ContentLayer) and never depends on the
    // host's visual readiness (the "no dependence on the root" contract).
    shell.Start();
    await shell.Lifetime.Startup;

    Assert.Same(((AvaloniaShell)shell).StagePanel, view.ContentLayer.Content);
  }

  // ── The root-view contract: not assigned = the view locator rules ──

  /// <summary>Registers view templates for the Director on the headless app (restored on dispose).</summary>
  private sealed class TemplateScope : IDisposable
  {
    private readonly DataTemplates _templates;
    private readonly IDataTemplate[] _added;

    public TemplateScope(params IDataTemplate[] added)
    {
      _templates = Application.Current!.DataTemplates;
      _added = added;
      foreach (var template in added)
        _templates.Add(template);
    }

    public void Dispose()
    {
      foreach (var template in _added)
        _templates.Remove(template);
    }
  }

  [AvaloniaFact]
  public async Task Host_NotOverridden_ResolvesViaViewLocator()
  {
    using var templates = new TemplateScope(new FuncDataTemplate<TestContext>((_, _) => new TestHostView()));

    // No CreateHost override = the locator default: the view locator
    // resolves the host from the Director ([ViewFor<T>] — the same
    // end-to-end contract as pages), the framework wires DataContext at
    // mount.
    IShell shell = TestHost.CreateShell<TestContext>(AddTestRegistrations, rootViewFromLocator: true);
    shell.Start();
    await shell.Lifetime.Startup;

    var shellImpl = Assert.IsType<TestShell<TestContext>>(shell);
    var holderView = Assert.IsType<TestHostView>(shellImpl.HostView);
    Assert.Same(shellImpl.Director, holderView.DataContext);
  }

  [AvaloniaFact]
  public async Task Host_NotOverridden_NoMapping_DesktopFailsFast()
  {
    // No template matches the Director: the locator misses, the host
    // stays null — desktop has NO unambiguous fallback (a shell without a
    // host cannot present): fail fast with a message pointing at both
    // routes (override CreateHost / provide a [ViewFor<T>] mapping).
    var templates = Application.Current!.DataTemplates;
    var saved = templates.ToList();
    templates.Clear();
    try
    {
      IShell shell = TestHost.CreateShell<TestContext>(AddTestRegistrations, rootViewFromLocator: true);
      Assert.Throws<InvalidOperationException>(() => shell.Start());
    }
    finally
    {
      templates.Clear();
      foreach (var template in saved)
        templates.Add(template);
    }
  }

  [AvaloniaFact]
  public async Task DispatchIntent_WithHost_ReachesHostAsLastResort()
  {
    var view = new HostView();
    IShell shell = TestHost.CreateShell<PassthroughContext>(AddTestRegistrations, rootView: view);
    shell.Start();
    await shell.Lifetime.Startup;   // the shell-mount (stage/host) is part of Start now

    // ① layers (none) → ② Director (passthrough) → ③ host — handled.
    IntentResult handled = await shell.DispatchIntent(null, new TestIntent());
    Assert.Equal(IntentResult.Handled, handled);
    // The startup dispatch's empty ShellActivationIntent already passed
    // through to the host once (count 1), this dispatch is the second.
    Assert.Equal(2, view.HandleCount);
  }
}
