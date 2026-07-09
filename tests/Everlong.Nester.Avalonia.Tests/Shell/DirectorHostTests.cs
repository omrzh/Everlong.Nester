using Avalonia.Headless.XUnit;
using Everlong.DI;
using Everlong.Nester.Intent;
using Everlong.Nester.Routing;
using Everlong.Nester.Shell;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Everlong.Nester.Tests.Hosting;

namespace Everlong.Nester.Tests.Shell;

/// <summary>
///   The user-shell shape: the user shell host-creates the
///   Director, assembles the registrations (framework defaults +
///   the user's registrations via <see cref="TestShell{TDirector}"/>),
///   builds the window's own provider, member-injects the Director (after the
///   container exists, before startup), and hands (provider, scope, director)
///   to the sealed shell.  Every shell owns its provider — no parent-child
///   relation to the host.
/// </summary>
public class DirectorHostTests
{
  // ── Director shapes ─────────────────────────────────────────

  /// <summary>Director that needs nothing beyond the framework defaults.</summary>
  public sealed class DefaultDirector : IShellDirector
  {
    public ValueTask HandleAsync(IntentContext context, IntentDelegate next) => next(context);
    public void OnAssembled(IShell shell) { }
    public bool HandleError(Exception exception) => true;
  }

  /// <summary>Injection-wired Director: member injection must run AFTER the container exists and BEFORE the startup dispatch reaches the Director.</summary>
  public sealed class InjectedDirector : IShellDirector, IInjectable
  {
    public IRouter? Router { get; private set; }

    public bool SawRouterOnFirstIntent { get; private set; }

    public void Inject(IServiceProvider services)
    {
      Router = services.GetRequiredService<IRouter>();
    }

    public ValueTask HandleAsync(IntentContext context, IntentDelegate next)
    {
      SawRouterOnFirstIntent |= Router is not null;   // injection ran before the startup dispatch (empty ShellActivationIntent)
      return next(context);
    }

    public void OnAssembled(IShell shell) { }
    public bool HandleError(Exception exception) => true;
  }

  /// <summary>User shell shape: registrations per Director kind (every shell is its own SPA).</summary>
  private sealed class MarkedShellFactory : TestShell<DefaultDirector>
  {
    public MarkedShellFactory()
      : base(services => services.AddScoped<MarkedService>())
    {
    }
  }

  /// <summary>The shell service registered per shell kind by <see cref="MarkedShellFactory"/>.</summary>
  public sealed class MarkedService;

  // ── Harness ──────────────────────────────────────────────

  // ── Factory: framework defaults ───────────────────────────

  [AvaloniaFact]
  public async Task DefaultFactory_BuildsWindowWithFrameworkDefaults()
  {

    var shell = TestHost.CreateShell<DefaultDirector>();

    // Framework domains resolve from the window container; the shell
    // self-registered into its own container.
    Assert.NotNull(((IShell)shell).Services!.GetRequiredService<IRouter>());
    Assert.Same(shell, ((IShell)shell).Services!.GetRequiredService<IShell>());
    Assert.Same(shell, ((IShell)shell).Services!.GetRequiredService<TestShell<DefaultDirector>>());
  }

  // ── Factory: registrations (per shell kind) ───────

  [AvaloniaFact]
  public async Task FactoryWindowRegistrations_LandInWindowContainer()
  {

    // The user shell's registrations (the Initialize registrations) resolve from the
    // window container — the Director's kind selected the registrations.
    var shell = TestHost.CreateShell<DefaultDirector>(register: null, out _);
    var services = ((IShell)shell).Services!;

    // (framework default factory: no user registrations → nothing extra)
    Assert.Null(services.GetService<MarkedService>());

    // The user shell shape:
    var custom = new MarkedShellFactory();
    try
    {
      Assert.NotNull(custom.Services!.GetRequiredService<MarkedService>());
    }
    finally
    {
      await custom.DisposeAsync();
    }
  }

  // ── Injection timing ──────────────────────────────────────

  [AvaloniaFact]
  public async Task MemberInjection_RunsBeforeStartupDispatch()
  {

    var shell = TestHost.CreateShell<InjectedDirector>();
    shell.Start();
    await shell.Lifetime.Startup;

    var director = Assert.IsType<InjectedDirector>(shell.Director);
    Assert.NotNull(director.Router);
    Assert.True(director.SawRouterOnFirstIntent);   // the startup dispatch (empty ShellActivationIntent) saw the injected Router
  }

  // ── A user-provided Director = the user's protocol ──

  [AvaloniaFact]
  public async Task ManualDirector_InjectionIsTheUsersBusiness()
  {
    // The cut: a USER-provided Director (assigned by hand) is never touched
    // by the framework — the IInjectable protocol is the user's job there;
    // the framework only injects what it resolves itself via DirectorType
    // (see MemberInjection_RunsBeforeStartupDispatch for the other side).
    var shell = TestHost.CreateShell<InjectedDirector>(manualDirector: true);
    shell.Start();
    await shell.Lifetime.Startup;

    var director = Assert.IsType<InjectedDirector>(shell.Director);
    Assert.Null(director.Router);   // never injected — the user's protocol, not the framework's
    Assert.False(director.SawRouterOnFirstIntent);
  }

  // ── Every shell owns its provider ─────────────────────────

  [AvaloniaFact]
  public async Task EachShell_OwnsItsProvider()
  {

    var a = TestHost.CreateShell<DefaultDirector>();
    var b = TestHost.CreateShell<DefaultDirector>();

    Assert.NotSame(a, b);
    Assert.NotSame(((IShell)a).Services, ((IShell)b).Services);
    Assert.NotSame(a.RootProvider, b.RootProvider);

    // The shell owns its container: releasing shell A does not touch B's.
    await a.DisposeAsync();
    Assert.NotNull(((IShell)b).Services.GetRequiredService<IRouter>());
    await b.DisposeAsync();

    // A disposed shell's container is gone — the resolution face is gone
    // too: accessing Services after disposal throws (never null).
    Assert.Throws<InvalidOperationException>(() => ((IShell)b).Services);
  }
}
