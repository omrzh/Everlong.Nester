using Avalonia;
using Avalonia.Headless.XUnit;
using Everlong.DI;
using Everlong.Nester.Presentation;
using Everlong.Nester.Dialog;
using Everlong.Nester.Messaging;
using Everlong.Nester.Routing;
using Everlong.Nester.Shell;
using Microsoft.Extensions.DependencyInjection;
using NesterApp;
using NesterApp.Dialogs;
using NesterApp.Pages.Landing;
using NesterApp.Pages.Posts;
using NesterApp.Pages.Settings;
using NesterApp.Pages.Shell;
using NesterApp.Properties;
using NesterApp.Services;
using Xunit;

using Everlong.Nester.Tests.Hosting;

namespace Everlong.Nester.Tests.Shell;

/// <summary>
///   The REAL-application test surface: Template.Shared compiled into the
///   test project — the actual Director (MainViewModel with [Inject] Dialogs/Busy),
///   the [ServiceRegistrar] AppServices registration surface, real page
///   models, real dialog sessions, real settings and the real i18n Lang.
///   Only the visual stack is fake (one DataTemplate → ContentControl with a
///   type tag); everything else runs the true Build flow.
/// </summary>
[Collection("RealShell")]
public sealed class RealAppTests
{
  /// <summary>Fake visual stack: every NesterApp view model maps to a tagged ContentControl.</summary>
  private static IShell BuildMainShell(Action<IServiceCollection>? extra = null)
  {
    // Rebuild the chain deterministically (serialized by the RealShell
    // collection).  CompositeViewLocator scans FORWARDS (first wins), so
    // the app template must be FIRST: app template → framework views,
    // i.e. index 0 is checked first.
    Application.Current!.DataTemplates.Clear();
    Application.Current!.DataTemplates.Add(RealAppHarness.PageTemplate);
    Application.Current!.DataTemplates.Add(new NesterExtendedViewLocator());

    return TestHost.CreateShell<MainViewModel>(s =>
    {
      TestAppServices.AddWindowAuth(s);          // real app-level registrations (auth, http)
      s.AddServices(new AppServices());   // real [ServiceRegistrar] surface
      extra?.Invoke(s);                   // the test's own seam over the app's (last registration wins)
    });
  }

  [AvaloniaFact]
  public void Build_MainViewModel_RealFlow_ResolvesRealServices()
  {
    var shell = BuildMainShell();

    // The real Director is resolved and member-injected by BuildStage —
    // these services are what MainViewModel's [Inject] members resolve to.
    Assert.NotNull(shell.Services!.GetService<IRouter>());
  }

  [AvaloniaFact]
  public async Task Navigate_RealDirector_StartupCoroutineRuns_AndBusyResolves()
  {
    var shell = BuildMainShell();

    // The real Director is resolved and member-injected by BuildStage — its
    // startup coroutine runs the first navigation through the shell's router
    // (this is the regression guard for the "shadow Director" bug: a
    // re-constructed, never-injected MainViewModel NREs here).
    shell.Start();
    await shell.Lifetime.Startup;

    // Ground slice exists and carries the first navigation.
    Assert.IsType<LandingPageModel>(RealAppHarness.GroundEntry(shell));
    Assert.NotEmpty(((AvaloniaShell)shell).LeaseOrder());
  }

  [AvaloniaFact]
  public async Task Navigate_RealPages_LandingShown()
  {
    var shell = BuildMainShell();
    shell.Start();
    await shell.Lifetime.Startup;

    Assert.IsType<LandingPageModel>(RealAppHarness.GroundEntry(shell));

    await RealAppHarness.RouteAsync(shell, typeof(PostsPageModel));
    Assert.IsType<PostsPageModel>(RealAppHarness.GroundEntry(shell));
  }

  [AvaloniaFact]
  public async Task Navigate_SettingsPage_WithoutAThemeService_Materializes()
  {
    // The harness registers no theme service — the same shape as the
    // TerminalGui head, and the shape AppSettings.Inject already tolerates
    // ("a host without a theme service registers none").  The settings page
    // must materialize there too: its [Inject] member is optional, so the
    // resolution yields null instead of throwing out of Inject.
    var shell = BuildMainShell();
    shell.Start();
    await shell.Lifetime.Startup;

    await RealAppHarness.RouteAsync(shell, typeof(SettingsPageModel));

    Assert.IsType<SettingsPageModel>(RealAppHarness.GroundEntry(shell));
  }

  [AvaloniaFact]
  public async Task Dialog_RealSession_DismissCompletesAndReturnsLease()
  {
    var shell = BuildMainShell();
    shell.Start();
    await shell.Lifetime.Startup;

    var dialogs = shell.Services!.GetRequiredService<IRouter>();
    var session = new MyConfirmDialogSession { Message = "Real session" };
    var showing = dialogs.ShowAsync<bool>(session);

    // The dialog presents as a overlay on the window's stage (the router's
    // close-stack ledger) — its own layer, its own dimmer + chrome chain.
    var overlay = Assert.Single(((AvaloniaShell)shell).StagePanel!.DerivedHosts());
    Assert.Single(((AvaloniaShell)shell).StagePanel!.DerivedHosts());
    Assert.Same(session, overlay.Location!.Instance);

    // The session's own close ends the overlay (its result settles the
    // show task with the given value).
    session.Close(false);

    // Session completed; the overlay is gone.
    _ = await showing.WaitAsync(TimeSpan.FromSeconds(5));
    Assert.Empty(((AvaloniaShell)shell).StagePanel!.DerivedHosts());
    Assert.Empty(((AvaloniaShell)shell).StagePanel!.DerivedHosts());
  }

  [AvaloniaFact]
  public void Lang_RealLocales_ResolveStrings()
  {
    Lang.Initialize("en");

    // Real generated Lang surface (dotnet elg gen over Template.Shared locales).
    Assert.False(string.IsNullOrEmpty(Lang.Pages.DashboardTitle));

    // The shell section the Director reads: the hand-off question's file count
    // is an ICU plural, so the count picks the branch (not string interpolation).
    Assert.False(string.IsNullOrEmpty(Lang.Shell.CloseConfirmMessage));
    Assert.Equal("2 files", Lang.Shell.FormatActivationFileCount(2));
    Assert.Equal("no files", Lang.Shell.FormatActivationFileCount(0));

    // The backdrop dev panel's own section.
    Assert.False(string.IsNullOrEmpty(Lang.Backdrop.Title));
  }

  /// <summary>The opener seam, recorded instead of launching an application.</summary>
  private sealed class RecordingOpener : IWorkspaceFileOpener
  {
    /// <summary>Holds the shell hand-off open, so a test can see what settled first.</summary>
    public TaskCompletionSource<bool> Gate { get; } = new();

    public string? LastPath { get; private set; }

    public async Task<bool> OpenAsync(string path)
    {
      LastPath = path;
      return await Gate.Task;
    }
  }

  [AvaloniaFact]
  public async Task Workspace_RealDirector_StartsTheWatcherOnTheResolvedRoot()
  {
    var shell = BuildMainShell();
    shell.Start();
    await shell.Lifetime.Startup;

    // The Director's OnAssembled owns the background work: the watcher takes
    // the OS handle over a root nobody configured — the nearest project marker
    // above the process, or the Downloads fallback.
    var monitor = shell.Services!.GetRequiredService<WorkspaceFileMonitor>();

    Assert.True(monitor.IsWatching);
    Assert.NotEmpty(monitor.Root);
  }

  [AvaloniaFact]
  public async Task Workspace_Watcher_StopsOnTheHostsStoppingSignal()
  {
    var shell = BuildMainShell();
    shell.Start();
    await shell.Lifetime.Startup;

    var monitor = shell.Services!.GetRequiredService<WorkspaceFileMonitor>();
    Assert.True(monitor.IsWatching);

    // The domain's only shutdown path is IHostLifetime — the shell's own
    // teardown cancels it, so the watcher is down before the container is.
    await ((AvaloniaShell)shell).DisposeAsync();

    Assert.False(monitor.IsWatching);
  }

  [AvaloniaFact]
  public async Task Workspace_Palette_AppliesABroadcastChangeWhileItIsOpen()
  {
    var shell = BuildMainShell();
    shell.Start();
    await shell.Lifetime.Startup;

    var hub = shell.Services!.GetRequiredService<IMessageHub>();

    var dispatching = shell.DispatchIntent(null, new ShowWorkspacePaletteIntent());
    var stage = ((AvaloniaShell)shell).StagePanel!;
    var palette = Assert.IsType<WorkspacePaletteSession>(Assert.Single(stage.DerivedHosts()).Location!.Instance);

    const string added = "docs/brand-new-file.txt";
    Assert.DoesNotContain(added, palette.Matches);

    // The watcher publishes on its own thread and the palette is the recipient
    // that hops; this body already runs on the UI thread, so the hop lands
    // inline — the list moves without a re-read of the tree.
    hub.Publish(new WorkspaceChangedMessage([new WorkspaceChange(added, WorkspaceChangeKind.Created)]));

    Assert.Equal(added, palette.Matches[0]);   // the newest change leads the list

    // A deletion takes it back out.
    hub.Publish(new WorkspaceChangedMessage([new WorkspaceChange(added, WorkspaceChangeKind.Deleted)]));

    Assert.DoesNotContain(added, palette.Matches);

    palette.Close();
    await dispatching;
  }

  [AvaloniaFact]
  public async Task Workspace_RealDirector_PresentsThePaletteAndSettlesItsResult()
  {
    var opener = new RecordingOpener();
    var shell = BuildMainShell(s => s.AddSingleton<IWorkspaceFileOpener>(opener));
    shell.Start();
    await shell.Lifetime.Startup;

    var monitor = shell.Services!.GetRequiredService<WorkspaceFileMonitor>();

    // Ctrl+Shift+P reaches the Director as this intent; the Director presents
    // the palette on a derived router — an ordinary derived surface, with the
    // dimmer and the result channel every other presentation has.
    var dispatching = shell.DispatchIntent(null, new ShowWorkspacePaletteIntent());
    var stage = ((AvaloniaShell)shell).StagePanel!;
    var palette = Assert.IsType<WorkspacePaletteSession>(Assert.Single(stage.DerivedHosts()).Location!.Instance);

    // It opened over the workspace the watcher resolved, listing its files.
    Assert.Equal(monitor.Root, palette.Root);
    Assert.NotEmpty(palette.Matches);
    Assert.False(palette.IsEmpty);
    Assert.Equal(0, palette.SelectedIndex);

    // The filter is the model's: typing narrows the rows the palette owns.
    string row = palette.Matches[0];
    palette.Filter = row;
    Assert.All(palette.Matches, match => Assert.Contains(row, match, StringComparison.OrdinalIgnoreCase));

    // The keyboard only asks — the model moves the row.
    palette.Filter = string.Empty;
    palette.NextCommand.Execute(null);
    Assert.Equal(1, palette.SelectedIndex);

    // Enter hands the selected path to the opener seam and settles the
    // presentation with it — a palette is a destination, not a navigation.
    string expected = Path.Combine(monitor.Root, palette.Matches[palette.SelectedIndex]);
    Task opening = palette.OpenCommand.ExecuteAsync(null);

    // Enter settles the palette BEFORE the editor exists — the hand-off is still
    // pending while the presentation is already down.  A slow shell association
    // (the OS picking an application, then talking to it) is the editor's wait,
    // never the overlay's.
    await dispatching;

    Assert.Equal(expected, opener.LastPath);
    Assert.False(opening.IsCompleted);
    Assert.Empty(stage.DerivedHosts());

    // The launch finishes on its own, long after the palette is gone.
    opener.Gate.SetResult(true);
    await opening;
  }
}
