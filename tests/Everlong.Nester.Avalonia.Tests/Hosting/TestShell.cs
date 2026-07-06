using Avalonia.Controls;
using Everlong.Nester.DI;
using Everlong.Nester.Dialog;
using Everlong.Nester.Shell;
using Microsoft.Extensions.DependencyInjection;
using Everlong.Nester.Tests.Shell;
using Everlong.Nester.Activation;

namespace Everlong.Nester.Tests.Hosting;

/// <summary>
///   The test harness's own shell — the USER-owned shell shape: a concrete
///   shell class per shell kind, the assembly job in
///   <see cref="InitializeServices" /> (assigns the shell's container — the
///   harness's own build).  The Director is DECLARED via
///   <see cref="DirectorType" /> and resolved through the injector at start
///   (member injection included).  Tests that never call Start still get a
///   live container: the constructor assembles the shell
///   (<see cref="ShellBase.EnsureAssembled" /> — idempotent, Start's
///   assembly step is then a no-op).
/// </summary>
internal class TestShell<TDirector> : AvaloniaShell where TDirector : class, IShellDirector, new()
{
  private readonly Action<IServiceCollection>? _extraRegistrations;
  private ContentControl? _rootView;
  private readonly bool _directMount;
  private readonly bool _manualDirector;
  private readonly bool _rootViewFromLocator;
  private bool _noHost;

  public TestShell(Action<IServiceCollection>? extraRegistrations = null, ContentControl? rootView = null, bool directMount = false, bool manualDirector = false, bool rootViewFromLocator = false, bool noHost = false, IActivationIntent? startupIntent = null)
    : base(startupIntent)
  {
    _extraRegistrations = extraRegistrations;
    _rootView = rootView;
    _directMount = directMount;
    _manualDirector = manualDirector;
    _rootViewFromLocator = rootViewFromLocator;
    _noHost = noHost;
    if (!manualDirector)
      DirectorType = typeof(TDirector);   // declarative: resolved via the injector at start (member injection included)
    EnsureAssembled();   // tests need the container after construction (Start may never run) — idempotent
  }

  protected override IServiceProvider InitializeServices()
  {
    var services = new ServiceCollection();
    if (!_manualDirector)
      services.AddScoped<TDirector>();   // the Director — resolved via DirectorType (member injection included)
    services.AddNesterShell(this);   // the shell's identity: IShell / ILayerBroker / the concrete type
    services.AddNesterCore();        // core infrastructure (logging / injector / options / flying layer, the scoped routing domain)
    services.AddNesterDialog();      // the dialog domain
    services.AddNesterNotice();
    _extraRegistrations?.Invoke(services);

    var root = services.BuildServiceProvider(new ServiceProviderOptions
    {
      ValidateScopes = true,
      ValidateOnBuild = true
    });

    // A USER-provided Director (assigned by hand)
    // is never touched by the framework — the IInjectable protocol is the
    // user's business there; the framework only injects what it resolves
    // itself via DirectorType.
    if (_manualDirector)
      Director = new TDirector();

    return root;
  }

  /// <summary>Host production: direct mount / from-locator → the locator default; noHost → none (single-view direct mount); else the passed view or the harness's <see cref="TestHostView"/>.</summary>
  protected override void PrepareHost()
  {
    if (_directMount || _rootViewFromLocator)
    {
      base.PrepareHost();
      return;
    }

    if (_noHost)
    {
      ShellHost = null;
      EnsureDesktopHost(); // desktop without a host fails fast (single-view direct-mounts)
      return;
    }

    ShellHost = _rootView ?? new TestHostView();
  }

  /// <summary>The host produced by <see cref="PrepareHost"/> (test/observer channel).</summary>
  internal object? HostView => ShellHost;

  /// <summary>Swaps the host after construction (failed-mount retry tests).</summary>
  internal void ReplaceHost(ContentControl view)
  {
    _rootView = view;
    _noHost = false;
  }

  /// <summary>The framework never shows the window — the harness presents window hosts at the synchronous anchor (mirrors the template).</summary>
  protected override void OnAssembled()
  {
    if (ShellHost is Window window)
      window.Show();
  }
}
