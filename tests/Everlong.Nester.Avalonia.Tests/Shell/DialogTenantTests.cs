using Everlong.Nester.ComponentModel;
using Everlong.Nester.Presentation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Everlong.DI;
using Everlong.Nester.Controls;
using Everlong.Nester.Dialog;
using Everlong.Nester.Routing;
using Everlong.Nester.Shell;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using Everlong.Nester.Intent;
namespace Everlong.Nester.Tests.Shell;

using static Everlong.Nester.Tests.AsyncTestHelpers;

/// <summary>
///   The dialog tenant (P5, unified): each session presents as
///   a close-stack overlay on the router's ledger — the dimmer chain, the
///   session view and the enter transition all run through the routing
///   pipeline.  Concurrent dialogs get independent overlays (later on top),
///   intents dismiss the topmost dialog first,
///   sessions are injected into the shell scope, and the modal-back /
///   mandatory / dismiss semantics stay intact.
/// </summary>
[Collection("RealShell")]
public class DialogTenantTests
{
  // ── Scene types ──────────────────────────────────────────

  /// <summary>Scoped service whose disposal proves the dialog layer's scope release.</summary>
  public sealed class DisposableProbe : IDisposable
  {
    public bool Disposed { get; private set; }
    public void Dispose() => Disposed = true;
  }

  public sealed class ProbeDialog : DialogSessionBase<object?>, IInjectable
  {
    public DisposableProbe? ScopedProbe { get; private set; }

    public void Inject(IServiceProvider services)
    {
      ScopedProbe = services.GetService<DisposableProbe>();
    }
  }

  public sealed class TransitionDialog : DialogSessionBase<object?>
  {
  }

  /// <summary>Dialog view with transition counter.</summary>
  public sealed class TransitionDialogView : ContentControl, ISceneTransition
  {
    public int EnterCount { get; private set; }

    public Task AnimateEnterAsync(TransitionContext ctx, CancellationToken token)
    {
      EnterCount++;
      return Task.CompletedTask;
    }

    public Task AnimateExitAsync(TransitionContext ctx, CancellationToken token)
      => Task.CompletedTask;
  }

  private sealed class ViewLocator : IDataTemplate
  {
    public bool Match(object? data) => data is ProbeDialog or TransitionDialog;

    public Control? Build(object? data) => data switch
    {
      TransitionDialog => new TransitionDialogView(),
      _ => new ContentControl()
    };
  }

  // ── Harness ──────────────────────────────────────────────

  private static (RealShell Stage, StagePanel Panel, AvaloniaShell Shell, IServiceProvider Sp) CreateShell()
  {
    var shell = RealShell.Create<RealShell.RealTestContext>(
      register: s =>
      {
        s.AddScoped<DisposableProbe>();
      },
      template: new ViewLocator());
    return (shell, shell.Panel, shell.Shell, shell.Services);
  }

  private static NavigationHost SingleCapsule(StagePanel panel)
    => Assert.Single(panel.DerivedHosts());

  /// <summary>The dimmer view inside the overlay (the default dimmer chrome).</summary>
  private static DimmerLayout SingleDimmer(NavigationHost host)
    => Assert.IsType<DimmerLayout>(Assert.Single(host.Children));

  /// <summary>The dialog domain's active sessions (open order).</summary>


  // ── an exclusive overlay + dimmer shell + the reclaim loop ──

  [AvaloniaFact]
  public async Task ShowAsync_PresentsDimmerShell_ThenReclaimsLayer()
  {
    var (stage, panel, _, sp) = CreateShell();
    var dialogs = sp.GetRequiredService<IRouter>();
    var session = new ProbeDialog();

    Task showTask = dialogs.ShowAsync(session);
    await WaitUntilAsync(() => panel.DerivedHosts().Count() == 1);

    // One dialog overlay on the router's ledger; the session rides its chain.
    Assert.Single(panel.DerivedHosts());
    Assert.Same(session, SingleCapsule(panel).Location!.Instance);

    // The overlay hosts the framework dimmer (layer default shell); the
    // session view hangs inside the dimmer's body.
    var host = SingleCapsule(panel);
    var dimmer = SingleDimmer(host);
    var dialogView = Assert.Single(((LayoutBody)dimmer.GetLayoutBody()).Children);
    Assert.Same(session, dialogView.DataContext);

    // Close: the session completes, the overlay is reclaimed.
    session.Close("ok");
    await showTask;
    Assert.Empty(panel.DerivedHosts());
    Assert.Empty(panel.DerivedHosts());
  }

  /// <summary>
  ///   The dialog overlay owns its view chain: the overlay host carries the
  ///   presented chain (dimmer + session) as its status truth, and the
  ///   session is the leaf.
  /// </summary>
  [AvaloniaFact]
  public async Task DialogCapsule_HostsItsOwnViewChain()
  {
    var (_, panel, _, sp) = CreateShell();
    var dialogs = sp.GetRequiredService<IRouter>();
    var session = new ProbeDialog();

    Task showTask = dialogs.ShowAsync(session);
    await WaitUntilAsync(() => panel.DerivedHosts().Count() == 1);

    var host = SingleCapsule(panel);
    Assert.Equal(2, host.Location!.Trail.Count);   // [dimmer, session]
    Assert.Same(session, host.Location!.Instance);
    Assert.Single(panel.DerivedHosts());

    session.Close("ok");
    await showTask;
    Assert.Empty(panel.DerivedHosts());
  }

  // ── concurrent dialogs = independent overlays, mutually untouched ──

  [AvaloniaFact]
  public async Task ConcurrentDialogs_GetIndependentCapsules()
  {
    var (_, panel, _, sp) = CreateShell();
    var dialogs = sp.GetRequiredService<IRouter>();
    var first = new ProbeDialog();
    var second = new ProbeDialog();

    Task showFirst = dialogs.ShowAsync(first);
    Task showSecond = dialogs.ShowAsync(second);
    await WaitUntilAsync(() => panel.DerivedHosts().Count() == 2);

    // Two independent overlays — each owns its own mount point and dimmer,
    // ordered by the router's ledger (later cut on top).
    Assert.Equal(2, panel.DerivedHosts().Count());
    Assert.Equal(2, panel.DerivedHosts().Count());
    Assert.All(panel.DerivedHosts(), h => Assert.IsType<DimmerLayout>(Assert.Single(h.Children)));

    // Closing the first leaves the second fully alive; closing the second
    // reclaims the last overlay.
    first.Close();
    await showFirst;
    Assert.Single(panel.DerivedHosts());
    Assert.False(showSecond.IsCompleted);

    second.Close();
    await showSecond;
    Assert.Empty(panel.DerivedHosts());
  }

  // ── session injection through the shell scope; closing releases it ──

  [AvaloniaFact]
  public async Task DialogSession_InjectedIntoShellScope_AndCapsuleReclaimedOnClose()
  {
    var (_, panel, _, sp) = CreateShell();
    var containerProbe = sp.GetRequiredService<DisposableProbe>();
    var dialogs = sp.GetRequiredService<IRouter>();
    var session = new ProbeDialog();

    // Session injection runs synchronously inside ShowAsync (before the
    // first await) — the injected context is already captured.
    Task showTask = dialogs.ShowAsync(session);
    Assert.NotNull(session.ScopedProbe);

    // The session resolves from its derived router's own scope — a fresh Scoped
    // instance per derived router.
    Assert.NotSame(containerProbe, session.ScopedProbe);

    session.Close("ok");
    await showTask;

    // The dialog overlay is reclaimed on close.
    Assert.Empty(panel.DerivedHosts());
  }

  // ── presentation runs the shared pipeline — dimmer + transition ──

  [AvaloniaFact]
  public async Task DialogView_TransitionAndSceneLifecycle_Run()
  {
    var (_, panel, _, sp) = CreateShell();
    var dialogs = sp.GetRequiredService<IRouter>();
    var session = new TransitionDialog();

    Task showTask = dialogs.ShowAsync(session);
    await WaitUntilAsync(() => panel.DerivedHosts().Count() == 1);

    // Presented through the routing pipeline: dimmer shell + enter transition.
    var dimmer = SingleDimmer(SingleCapsule(panel));
    var dialogView = Assert.IsType<TransitionDialogView>(((LayoutBody)dimmer.GetLayoutBody()).Children.Single());
    Assert.Equal(1, dialogView.EnterCount);

    session.Close();
    await showTask;
    Assert.Empty(panel.DerivedHosts());
  }

  // ── the framework does not guess — default light-dismiss (back closes); mandatory is expressed by the dimmer ──

  [AvaloniaFact]
  public async Task BackIntent_Default_LightDismissesDialog()
  {
    var (_, panel, shell, sp) = CreateShell();
    var dialogs = sp.GetRequiredService<IRouter>();
    var session = new ProbeDialog();

    Task showTask = dialogs.ShowAsync(session);
    await WaitUntilAsync(() => panel.DerivedHosts().Count() == 1);

    // Default presentation is light-dismiss: an external back closes the
    // dialog; ShowAsync settles with null.
    Assert.Equal(IntentResult.Handled, await shell.DispatchIntent(null, new BackIntent()));
    await showTask.WaitAsync(TimeSpan.FromSeconds(10));
    Assert.Empty(panel.DerivedHosts());
  }

  [AvaloniaFact]
  public async Task BackIntent_MandatoryDimmer_NeverLightDismisses()
  {
    var (_, panel, shell, sp) = CreateShell();
    var dialogs = sp.GetRequiredService<IRouter>();
    var session = new ProbeDialog();

    Task showTask = dialogs.ShowAsync(session, new DefaultDimmerModel { LightDismiss = false });
    await WaitUntilAsync(() => panel.DerivedHosts().Count() == 1);

    // The mandatory dimmer wins: the back is vetoed — the dialog stays;
    // ShowAsync stays pending.
    Assert.Equal(IntentResult.Vetoed, await shell.DispatchIntent(null, new BackIntent()));
    Assert.False(showTask.IsCompleted);
    Assert.Single(panel.DerivedHosts());

    // The session's own close still ends it (ShowAsync completes).
    session.Close();
    await showTask.WaitAsync(TimeSpan.FromSeconds(10));
    Assert.True(showTask.IsCompleted);
    Assert.Empty(panel.DerivedHosts());
  }

  // ── a session closing itself must not hang ShowAsync ──

  [AvaloniaFact]
  public async Task DialogSessionOwnClose_ShowAsyncCompletes()
  {
    var (_, panel, _, sp) = CreateShell();
    var dialogs = sp.GetRequiredService<IRouter>();
    var session = new ProbeDialog();

    var showTask = dialogs.ShowAsync(session);
    await WaitUntilAsync(() => panel.DerivedHosts().Count() == 1);

    // The session's own close resolves the show task with the given
    // result so ShowAsync does not hang.
    session.Close();
    await showTask.WaitAsync(TimeSpan.FromSeconds(10));

    Assert.True(showTask.IsCompleted);
    Assert.Null(await showTask);
    Assert.Empty(panel.DerivedHosts());
  }
}
