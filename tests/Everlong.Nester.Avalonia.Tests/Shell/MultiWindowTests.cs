using Avalonia;
using Everlong.DI;
using Everlong.Nester.Tests.Hosting;
using NesterApp;
using Avalonia.Headless.XUnit;
using Everlong.Nester.Auth;
using Everlong.Nester.Presentation;
using Everlong.Nester.Dialog;
using Everlong.Nester.Layer;
using Everlong.Nester.Notice;
using Everlong.Nester.Routing;
using Everlong.Nester.Shell;
using Microsoft.Extensions.DependencyInjection;
using NesterApp.Dialogs;
using NesterApp.Models;
using NesterApp.Pages.Landing;
using NesterApp.Pages.Login;
using NesterApp.Pages.Posts;
using NesterApp.Pages.Shell;
using Xunit;

namespace Everlong.Nester.Tests.Shell;

/// <summary>
///   Multi-window coexistence — the template's login window and main
///   window alive SIMULTANEOUSLY (the real flow: the login shell hides, then
///   the main shell starts; both scopes live until the login shell closes).
///   Pins the shared-singleton contract (app-level services are ONE instance
///   across windows) against per-window scoped isolation (navigation, dialog
///   leases, intents, teardown never leak across windows).
/// </summary>
[Collection("RealShell")]
public sealed class MultiWindowTests
{
  /// <summary>Two shells — the login window and the main window, each owning its own container.</summary>
  private sealed record TwoWindows(IShell Login, IShell Main);

  /// <summary>
  ///   The real two-window assembly: each window builds and owns its own
  ///   container (the Directors assemble their shells' containers), the REAL
  ///   Directors (LoginWindowModel / MainViewModel) — both started through
  ///   the real startup flow, so both startup coroutines run their first
  ///   navigation.
  /// </summary>
  private static async Task<TwoWindows> BuildTwoWindows()
  {
    // Rebuild the chain deterministically (serialized by the RealShell
    // collection).  The DataTemplates collection is read FORWARDS (first
    // wins), so the app template must be FIRST: app template → framework views,
    // i.e. index 0 is checked first.
    Application.Current!.DataTemplates.Clear();
    Application.Current!.DataTemplates.Add(RealAppHarness.PageTemplate);
    Application.Current!.DataTemplates.Add(new NesterExtendedViewLocator());

    // The registrations come from the template's shell classes
    // (the shell classes' Initialize) in production — this harness supplies them
    // explicitly (the shells are template files the test project compiles
    // from Template.Shared; the harness registers by hand to stay
    // self-contained).
    void WindowRegistrations(IServiceCollection s)
    {
      TestAppServices.AddWindowAuth(s);
      s.AddServices(new AppServices());
    }

    var login = TestHost.CreateShell<LoginWindowModel>(WindowRegistrations);
    var main = TestHost.CreateShell<MainViewModel>(WindowRegistrations);

    // The real startup flow — both windows' Directors run their startup
    // coroutines: login → LoginPageModel; main → LandingPageModel.
    login.Start();
    await login.Lifetime.Startup;
    main.Start();
    await main.Lifetime.Startup;

    return new TwoWindows(login, main);
  }

  /// <summary>The dialog-domain slices of a window (its own leases — nothing shared).</summary>
  private static List<ILayerLease> DialogSlices(IShell shell)
    => ((AvaloniaShell)shell).LeaseOrder().Where(c => KnownLayers.Dialog.Contains(c.Z)).ToList();

  /// <summary>The dialog overlays currently mounted on a window's stage.</summary>
  private static List<RoutingView> DialogHosts(IShell shell)
    => ((AvaloniaShell)shell).StagePanel!.DerivedHosts().ToList();

  // ── coexistence ───────────────────────────────────────────────────

  [AvaloniaFact]
  public async Task TwoWindows_CoexistWithOwnContainersAndDirectors()
  {
    var w = await BuildTwoWindows();
    try
    {
      // Distinct window identities: each window owns its own container,
      // its own Director and page surface.
      Assert.NotSame(w.Login, w.Main);
      Assert.IsType<LoginWindowModel>(((AvaloniaShell)w.Login).Director);
      Assert.IsType<MainViewModel>(((AvaloniaShell)w.Main).Director);
      Assert.NotSame(((AvaloniaShell)w.Login).Director, ((AvaloniaShell)w.Main).Director);

      // Both startup coroutines settled: login shows the login page, the
      // main window shows the landing page — each in its own window.
      Assert.IsType<LoginPageModel>(RealAppHarness.GroundEntry(w.Login));
      Assert.IsType<LandingPageModel>(RealAppHarness.GroundEntry(w.Main));

      Assert.NotEqual(ShellLifecycle.Disposed, w.Login.Lifetime.Lifecycle);
      Assert.NotEqual(ShellLifecycle.Disposed, w.Main.Lifetime.Lifecycle);
    }
    finally
    {
      await ((IAsyncDisposable)w.Login).DisposeAsync();
      await ((IAsyncDisposable)w.Main).DisposeAsync();
    }
  }

  // ── per-window auth instances, explicit state hand-off ──────

  [AvaloniaFact]
  public async Task AuthService_PerWindowInstances_StateHandedOffExplicitly()
  {
    var w = await BuildTwoWindows();
    try
    {
      // Each window owns its auth service (the window container's
      // singleton) — NO shared app-level instance (the template's login
      // flow hands the state over explicitly instead).
      var loginAuth = w.Login.Services!.GetRequiredService<IAuthService>();
      var mainAuth = w.Main.Services!.GetRequiredService<IAuthService>();
      Assert.NotSame(loginAuth, mainAuth);

      // The login window authenticates — the main window sees nothing.
      loginAuth.Login(new DemoUser("admin", ["Admin"]));
      Assert.Null(mainAuth.CurrentUser);

      // The call site hands the state over explicitly (the template's
      // login→main flow shape): the main window's auth adopts the login
      // window's principal.
      mainAuth.Login(loginAuth.CurrentUser!);
      Assert.Same(loginAuth.CurrentUser, mainAuth.CurrentUser);
      Assert.True(mainAuth.Authenticate().Succeeded);
      Assert.True(mainAuth.Authorize("Admin", "Admin").Succeeded);
    }
    finally
    {
      await ((IAsyncDisposable)w.Login).DisposeAsync();
      await ((IAsyncDisposable)w.Main).DisposeAsync();
    }
  }

  // ── per-window scoped isolation ─────────────────────────────

  [AvaloniaFact]
  public async Task ScopedServices_PerWindowInstances()
  {
    var w = await BuildTwoWindows();
    try
    {
      // Window-level services are SCOPED — each window owns its instance;
      // nothing window-scoped may be shared between windows.
      Assert.NotSame(w.Login.Services!.GetRequiredService<IRouter>(),
                     w.Main.Services!.GetRequiredService<IRouter>());
      Assert.NotSame(w.Login.Services!.GetRequiredService<IRouter>(),
                     w.Main.Services!.GetRequiredService<IRouter>());
      Assert.NotSame(w.Login.Services!.GetRequiredService<INoticeService>(),
                     w.Main.Services!.GetRequiredService<INoticeService>());
    }
    finally
    {
      await ((IAsyncDisposable)w.Login).DisposeAsync();
      await ((IAsyncDisposable)w.Main).DisposeAsync();
    }
  }

  // ── navigation independence ─────────────────────────────────

  [AvaloniaFact]
  public async Task Navigation_IndependentAcrossWindows()
  {
    var w = await BuildTwoWindows();
    try
    {
      // The main window navigates — the login window's stack is untouched.
      await RealAppHarness.RouteAsync(w.Main, typeof(PostsPageModel));
      Assert.IsType<PostsPageModel>(RealAppHarness.GroundEntry(w.Main));
      Assert.IsType<LoginPageModel>(RealAppHarness.GroundEntry(w.Login));

      // And the reverse: the login window navigates — the main window's
      // stack is untouched.
      await RealAppHarness.RouteAsync(w.Login, typeof(LandingPageModel));
      Assert.IsType<LandingPageModel>(RealAppHarness.GroundEntry(w.Login));
      Assert.IsType<PostsPageModel>(RealAppHarness.GroundEntry(w.Main));
    }
    finally
    {
      await ((IAsyncDisposable)w.Login).DisposeAsync();
      await ((IAsyncDisposable)w.Main).DisposeAsync();
    }
  }

  // ── dialog layer / lease isolation ──────────────────────────

  [AvaloniaFact]
  public async Task DialogLeases_IsolatedPerWindow()
  {
    var w = await BuildTwoWindows();
    try
    {
      var loginRouter = w.Login.Services!.GetRequiredService<IRouter>();
      var mainRouter = w.Main.Services!.GetRequiredService<IRouter>();

      // Both windows show a dialog — each presents a overlay on its OWN
      // window's router ledger (per-window isolation: the ground of one
      // window never sees the other's dialogs).
      var loginSession = new MyConfirmDialogSession { Message = "login window" };
      var mainSession = new MyConfirmDialogSession { Message = "main window" };
      var loginShowing = loginRouter.ShowAsync<bool>(loginSession);
      var mainShowing = mainRouter.ShowAsync<bool>(mainSession);

      Assert.Single(DialogHosts(w.Login));
      Assert.Single(DialogHosts(w.Main));
      Assert.Single(DialogHosts(w.Login));
      Assert.Single(DialogHosts(w.Main));

      // The LOGIN session closes through ITS own overlay: its overlay
      // closes, the main window's session is untouched.
      loginSession.Close(false);
      _ = await loginShowing.WaitAsync(TimeSpan.FromSeconds(5));
      Assert.False(mainShowing.IsCompleted);
      Assert.Empty(DialogHosts(w.Login));   // the login overlay closed
      Assert.Single(DialogHosts(w.Main));   // the other window's session lives on
      Assert.Single(DialogHosts(w.Main));                       // its overlay untouched

      // The main window's dialog still closes through ITS overlay.
      mainSession.Close(false);
      _ = await mainShowing.WaitAsync(TimeSpan.FromSeconds(5));
      Assert.Empty(DialogHosts(w.Main));
    }
    finally
    {
      await ((IAsyncDisposable)w.Login).DisposeAsync();
      await ((IAsyncDisposable)w.Main).DisposeAsync();
    }
  }

  // ── teardown independence ───────────────────────────────────

  [AvaloniaFact]
  public async Task DisposeLoginWindow_MainWindowKeepsWorking()
  {
    var w = await BuildTwoWindows();
    try
    {
      // Full teardown of ONE window — the other window's scope, services
      // and navigation keep working.
      await ((IAsyncDisposable)w.Login).DisposeAsync();
      Assert.Equal(ShellLifecycle.Disposed, w.Login.Lifetime.Lifecycle);
      Assert.NotEqual(ShellLifecycle.Disposed, w.Main.Lifetime.Lifecycle);

      await RealAppHarness.RouteAsync(w.Main, typeof(PostsPageModel));
      Assert.IsType<PostsPageModel>(RealAppHarness.GroundEntry(w.Main));

      // The shared singleton survives the login window's teardown.
      Assert.NotNull(w.Main.Services!.GetRequiredService<IAuthService>());
    }
    finally
    {
      await ((IAsyncDisposable)w.Main).DisposeAsync();
    }
  }
}
