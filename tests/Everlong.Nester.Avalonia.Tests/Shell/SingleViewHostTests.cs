using System.Reflection;
using Avalonia;
using Everlong.DI;
using NesterApp;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Everlong.Nester.Presentation;
using Everlong.Nester.Hosting;
using Everlong.Nester.Shell;
using Everlong.Nester.Threading;
using Microsoft.Extensions.DependencyInjection;
using NesterApp.Pages.Login;
using NesterApp.Pages.Settings;
using NesterApp.Pages.Shell;
using NesterApp.Services;
using Xunit;

using Everlong.Nester.Tests.Hosting;

using Everlong.Nester.Intent;
namespace Everlong.Nester.Tests.Shell;

/// <summary>
///   The single-view host path — the single-view shell has no
///   host and no Director view: DIRECT MOUNT — the stage itself is the top
///   level (the browser host puts it into its AvaloniaView.Content).  Pins
///   the MainView contract: start connects the stage as the MainView,
///   rebuild swaps MainView to the new shell, close detaches.
/// </summary>
[Collection("RealShell")]
public sealed class SingleViewHostTests
{
  /// <summary>
  ///   Avalonia 12 marks the lifetime interfaces <c>[NotClientImplementable]</c>:
  ///   the C# compiler rejects user implementations (CS0535 with a synthesized
  ///   member name), and the <c>ApplicationLifetime</c> setter throws once the
  ///   app is set up (Application.cs — "not possible to change after
  ///   Application was initialized").  The fake is therefore a runtime
  ///   <see cref="DispatchProxy"/> (the compiler never sees a user class
  ///   implementing the interface) installed on the backing field directly —
  ///   per test, restored afterwards.  The platform reads the property at call
  ///   time; the guard is setter-only.
  /// </summary>
  private static readonly FieldInfo LifetimeField = typeof(Application).GetField(
    "_applicationLifetime", BindingFlags.NonPublic | BindingFlags.Instance)
    ?? throw new InvalidOperationException("Avalonia.Application._applicationLifetime not found (Avalonia 12.1.1).");

  private class SingleViewLifetimeProxy : DispatchProxy
  {
    private Control? _mainView;

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
      return targetMethod?.Name switch
      {
        "get_MainView" => _mainView,
        "set_MainView" => _mainView = args?[0] as Control,
        _ => null
      };
    }
  }

  private static (ISingleViewApplicationLifetime Fake, IApplicationLifetime? Original, AppLifetimeImpl Lifetime) InstallSingleViewLifetime()
  {
    var fake = DispatchProxy.Create<ISingleViewApplicationLifetime, SingleViewLifetimeProxy>();
    var original = (IApplicationLifetime?)LifetimeField.GetValue(Application.Current);
    LifetimeField.SetValue(Application.Current, fake);

    // The real Director (MainViewModel) reads AppLifetime.IsSingleView to
    // pick its startup page — bind a single-view lifetime for the shell tests
    // (cleared per-test, mirroring NegotiationHandshakeTests).
    var impl = new AppLifetimeImpl(new AppLifetimeOptions(), isSingleView: true);
    impl.BindStaticFacades();
    return (fake, original, impl);
  }

  private static async Task<IShell> BuildSingleViewShell()
  {
    // Rebuild the chain deterministically (serialized by the RealShell
    // collection): the page template → the framework views.  No host
    // template: a single-view shell declares no host, so the stage is the
    // MainView.
    Application.Current!.DataTemplates.Clear();
    Application.Current!.DataTemplates.Add(RealAppHarness.PageTemplate);
    Application.Current!.DataTemplates.Add(new NesterExtendedViewLocator());

    // The registrations come from the template's shell classes
    // (the shell classes' Initialize) in production — this harness supplies them
    // explicitly.
    var shell = TestHost.CreateShell<MainViewModel>(s =>
    {
      TestAppServices.AddWindowAuth(s);
      s.AddServices(new AppServices());
    }, directMount: true);   // single-view: no host declared — the stage itself is the MainView
    shell.Start();
    await shell.Lifetime.Startup;
    return shell;
  }

  /// <summary>The single-view MainView — the shell's own stage (direct mount: no host is declared).</summary>
  private static Control MainViewOf(ISingleViewApplicationLifetime sv)
    => Assert.IsAssignableFrom<Control>(sv.MainView);

  /// <summary>
  ///   Page-mapping template that EXCLUDES the Director: pages resolve, the
  ///   root view does not — the unambiguous direct-mount environment.
  /// </summary>
  private sealed class NoRootTemplate : IDataTemplate
  {
    public Control? Build(object? param)
      => param is null ? null : new ContentControl { Tag = param.GetType() };

    public bool Match(object? data)
      => data is not null
         && data.GetType().Namespace?.StartsWith("NesterApp") == true
         && data.GetType() != typeof(MainViewModel);
  }

  // ── no host declared: the stage IS the MainView ──

  [AvaloniaFact]
  public async Task SingleViewHost_Start_NoHostDeclared_DirectMountsTheStage()
  {
    var (sv, original, impl) = InstallSingleViewLifetime();
    try
    {
      var shell = await BuildSingleViewShell();

      // The host is the concrete shell's to declare.  Declaring none in a
      // single view means direct mount: the stage itself is the top level
      // (the browser host puts it into its AvaloniaView.Content), and the
      // stage carries the Director the shell presents.
      Assert.Same(((AvaloniaShell)shell).StagePanel, MainViewOf(sv));

      // The browser Director's startup coroutine ran: first navigation = login.
      Assert.IsType<LoginPageModel>(RealAppHarness.GroundEntry(shell));
    }
    finally
    {
      LifetimeField.SetValue(Application.Current, original);
      await impl.DisposeAsync();
      AppLifetime.ResetForTesting();
      MainDispatcher.ResetForTesting();
    }
  }

  // ── the desktop-only domain stays idle in a single view ──────

  [AvaloniaFact]
  public async Task SingleView_Director_LeavesTheWorkspaceWatcherUnstarted()
  {
    var (_, original, impl) = InstallSingleViewLifetime();
    try
    {
      var shell = await BuildSingleViewShell();

      // The palette is a desktop gesture: a single view (mobile / browser /
      // TUI) has no window that could translate Ctrl+Shift+P, so the Director
      // never starts the domain — it is registered and idle, not watching.
      var monitor = shell.Services!.GetRequiredService<WorkspaceFileMonitor>();

      Assert.False(monitor.IsWatching);
      Assert.Empty(monitor.Root);
    }
    finally
    {
      LifetimeField.SetValue(Application.Current, original);
      await impl.DisposeAsync();
      AppLifetime.ResetForTesting();
      MainDispatcher.ResetForTesting();
    }
  }

  // ── no mapping = the UNAMBIGUOUS fallback — direct mount ──────

  [AvaloniaFact]
  public async Task SingleViewHost_Start_DirectMountsStage_WhenNoMapping()
  {
    var (sv, original, impl) = InstallSingleViewLifetime();
    try
    {
      // No template matches MainViewModel: the locator misses, the
      // root view stays null — the unambiguous single-view fallback:
      // the stage itself becomes the MainView.  Pages still resolve
      // (NoRootTemplate excludes only the Director).
      Application.Current!.DataTemplates.Clear();
      Application.Current!.DataTemplates.Add(new NoRootTemplate());
      Application.Current!.DataTemplates.Add(new NesterExtendedViewLocator());

      var shell = TestHost.CreateShell<MainViewModel>(s =>
      {
        TestAppServices.AddWindowAuth(s);
        s.AddServices(new AppServices());
      }, directMount: true);
      shell.Start();
      await shell.Lifetime.Startup;

      Assert.Same(shell.StagePanel, sv.MainView);   // direct mount

      // The browser Director's startup coroutine ran: first navigation = login.
      Assert.IsType<LoginPageModel>(RealAppHarness.GroundEntry(shell));
    }
    finally
    {
      LifetimeField.SetValue(Application.Current, original);
      await impl.DisposeAsync();
      AppLifetime.ResetForTesting();
      MainDispatcher.ResetForTesting();
    }
  }

  // ── the rebuild (settings language switch) ──────────────────

  [AvaloniaFact]
  public async Task SingleViewHost_Rebuild_SwapsMainView_ToTheNewShell()
  {
    var (sv, original, impl) = InstallSingleViewLifetime();
    try
    {
      // Real visual stack (the harness template set — see BuildSingleViewShell).
      Application.Current!.DataTemplates.Clear();
      Application.Current!.DataTemplates.Add(RealAppHarness.PageTemplate);
      Application.Current!.DataTemplates.Add(new NesterExtendedViewLocator());

      // One window container per shell — the settings language switch
      // flow: new shell → Start → navigate to settings.
      void WindowRegistrations(IServiceCollection s)
      {
        TestAppServices.AddWindowAuth(s);
        s.AddServices(new AppServices());
      }

      var a = TestHost.CreateShell<MainViewModel>(WindowRegistrations, directMount: true);
      a.Start();
      await a.Lifetime.Startup;
      var aView = MainViewOf(sv);

      // The rebuild: a second shell (its own container) starts and
      // replaces the MainView.
      var b = TestHost.CreateShell<MainViewModel>(WindowRegistrations, directMount: true);
      b.Start();
      await b.Lifetime.Startup;

      var bView = MainViewOf(sv);
      Assert.NotSame(aView, bView);   // each shell's own stage
      Assert.Same(b.StagePanel, bView);   // the new shell's stage is the MainView
      Assert.Null(aView.Parent);   // the old shell's stage left the live tree

      // The new shell is live — the rebuild's next step is the settings page.
      await RealAppHarness.RouteAsync(b, typeof(SettingsPageModel));
      Assert.IsType<SettingsPageModel>(RealAppHarness.GroundEntry(b));

      // Retire the old shell (the real flow disposes the previous
      // single-view shell) — the new shell keeps working.
      await a.DisposeAsync();
      await RealAppHarness.RouteAsync(b, typeof(LoginPageModel));
      Assert.IsType<LoginPageModel>(RealAppHarness.GroundEntry(b));
      Assert.Same(bView, sv.MainView);
    }
    finally
    {
      LifetimeField.SetValue(Application.Current, original);
      await impl.DisposeAsync();
      AppLifetime.ResetForTesting();
      MainDispatcher.ResetForTesting();
    }
  }

  // ── close detaches ──────────────────────────────────────────

  [AvaloniaFact]
  public async Task SingleViewHost_CloseIntent_DetachesMainViewAndDisposes()
  {
    var (sv, original, impl) = InstallSingleViewLifetime();
    try
    {
      var shell = await BuildSingleViewShell();
      Assert.NotNull(sv.MainView);

      // CloseIntent is handled by the single-view fallback path: the shell
      // is destroyed and the MainView detached — the fallback awaits
      // DisposeAsync, so the dispatch returns deterministically done.
      Assert.Equal(IntentResult.Handled, await shell.DispatchIntent(null, new CloseIntent()));

      Assert.Null(sv.MainView);
      Assert.Equal(ShellLifecycle.Disposed, shell.Lifetime.Lifecycle);
    }
    finally
    {
      LifetimeField.SetValue(Application.Current, original);
      await impl.DisposeAsync();
      AppLifetime.ResetForTesting();
      MainDispatcher.ResetForTesting();
    }
  }
}
