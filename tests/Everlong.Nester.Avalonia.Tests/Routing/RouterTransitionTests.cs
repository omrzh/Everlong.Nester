using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Everlong.Nester.Presentation;
using Everlong.Nester.Shell;
using Everlong.Nester.Tests.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using Everlong.Nester.Intent;
using Everlong.Nester.Routing;
namespace Everlong.Nester.Tests.Routing;

/// <summary>
///   The platform reveal choreography: the scene director selection
///   (the changed chain's first <see cref="ISceneTransition" />), the
///   arriving-invisible prep phase, and the entry-release view removal
///   (forward trail cleared by a push).
/// </summary>
[Collection("RealShell")]
public class RouterTransitionTests
{
  private sealed class LayoutView : ContentControl, IBodyHolder
  {
    private readonly BodyPanel _body = new();

    internal LayoutView()
    {
      Content = _body;
      Width = 100;
      Height = 100;
    }

    public IBodyPanel GetBodyPanel() => _body;
  }

  private sealed class PageView : ContentControl { }

  /// <summary>A layout model that the test maps to a directing layout view.</summary>
  private sealed class DirectorLayout : TestContent { }

  /// <summary>A body-holding layout that is also an <see cref="ISceneTransition" /> — records the calls it receives.</summary>
  private sealed class DirectorLayoutView : ContentControl, IBodyHolder, ISceneTransition
  {
    private readonly BodyPanel _body = new();

    internal DirectorLayoutView()
    {
      Content = _body;
      Width = 100;
      Height = 100;
    }

    internal List<string> Calls { get; } = [];

    internal TransitionContext? LastContext { get; private set; }

    internal double? OpacityAtEnter { get; private set; }

    internal double? ArrivingChildOpacityAtEnter { get; private set; }

    public IBodyPanel GetBodyPanel() => _body;

    public Task AnimateEnterAsync(TransitionContext context, CancellationToken token)
    {
      Calls.Add("Enter");
      LastContext = context;
      OpacityAtEnter = Opacity;
      ArrivingChildOpacityAtEnter = GetBodyPanel().ActiveChild?.View?.Opacity;
      return Task.CompletedTask;
    }

    public Task AnimateExitAsync(TransitionContext context, CancellationToken token)
    {
      Calls.Add("Exit");
      return Task.CompletedTask;
    }
  }

  /// <summary>A page whose arrival suspends on a gate — simulates an in-flight data load.</summary>
  private abstract class GatedArrivalPage : IArrived
  {
    private readonly TaskCompletionSource _gate;

    internal GatedArrivalPage(TaskCompletionSource gate) => _gate = gate;

    /// <summary>Signals that the arrival hook has been entered (the reveal is done).</summary>
    internal TaskCompletionSource ArrivalStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task OnArrivedAsync(IRoutingContext context)
    {
      ArrivalStarted.TrySetResult();
      return _gate.Task;
    }
  }

  private sealed class LoadingPage : GatedArrivalPage
  {
    internal LoadingPage(TaskCompletionSource gate) : base(gate) { }
  }

  private sealed class RecordingDirectorPage : GatedArrivalPage
  {
    internal RecordingDirectorPage(TaskCompletionSource gate) : base(gate) { }
  }

  private sealed class ThrowingDirectorPage : GatedArrivalPage
  {
    internal ThrowingDirectorPage(TaskCompletionSource gate) : base(gate) { }
  }

  private sealed class LoadingPageView : ContentControl { }

  /// <summary>A director whose exit animation records calls and completes normally.</summary>
  private sealed class RecordingDirectorView : ContentControl, ISceneTransition
  {
    internal List<string> Calls { get; } = [];

    public Task AnimateEnterAsync(TransitionContext context, CancellationToken token)
    {
      Calls.Add("Enter");
      return Task.CompletedTask;
    }

    public Task AnimateExitAsync(TransitionContext context, CancellationToken token)
    {
      Calls.Add("Exit");
      return Task.CompletedTask;
    }
  }

  /// <summary>A director whose exit animation throws — the reveal must survive it.</summary>
  private sealed class ThrowingDirectorView : ContentControl, ISceneTransition
  {
    internal List<string> Calls { get; } = [];

    public Task AnimateEnterAsync(TransitionContext context, CancellationToken token)
    {
      Calls.Add("Enter");
      return Task.CompletedTask;
    }

    public Task AnimateExitAsync(TransitionContext context, CancellationToken token)
    {
      Calls.Add("Exit");
      throw new InvalidOperationException("director exit failed");
    }
  }

  /// <summary>A page view that directs its own scene changes — records the calls.</summary>
  private sealed class DirectorPageView : ContentControl, ISceneTransition
  {
    internal List<string> Calls { get; } = [];

    internal double? OpacityAtEnter { get; private set; }

    internal double? OpacityAtExit { get; private set; }

    internal double? ArrivingOpacityAtExit { get; private set; }

    internal TransitionContext? LastContext { get; private set; }

    public Task AnimateEnterAsync(TransitionContext context, CancellationToken token)
    {
      Calls.Add("Enter");
      LastContext = context;
      OpacityAtEnter = Opacity;
      return Task.CompletedTask;
    }

    public Task AnimateExitAsync(TransitionContext context, CancellationToken token)
    {
      Calls.Add("Exit");
      LastContext = context;
      OpacityAtExit = Opacity;
      ArrivingOpacityAtExit = context.Arriving?.Opacity;
      return Task.CompletedTask;
    }
  }

  /// <summary>A distinct VM type whose view is a director.</summary>
  private sealed class DirectorPage : TestContent { }

  /// <summary>A parameterized page that serves revised arguments in place — its view is a director.</summary>
  private sealed class AdaptiveDirectorPage : IAdaptiveParameterized
  {
    public IArgs? EngagedArgs { get; private set; }

    bool IAdaptiveParameterized.IsAdaptable(IArgs? requested) => true;

    void IParameterized.DeliverArgs(IArgs? args) => EngagedArgs = args;
  }

  /// <summary>A VM whose view's enter animation suspends on a gate — the reveal stays in flight.</summary>
  private sealed class EnterGatedDirectorPage : IArrived
  {
    internal TaskCompletionSource EnterGate { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task OnArrivedAsync(IRoutingContext context) => Task.CompletedTask;
  }

  /// <summary>A director whose enter animation awaits the VM's gate (a long choreography).</summary>
  private sealed class EnterGatedDirectorView : ContentControl, ISceneTransition
  {
    internal List<string> Calls { get; } = [];

    public async Task AnimateEnterAsync(TransitionContext context, CancellationToken token)
    {
      Calls.Add("Enter");
      if (DataContext is EnterGatedDirectorPage page)
        await page.EnterGate.Task;
    }

    public Task AnimateExitAsync(TransitionContext context, CancellationToken token)
    {
      Calls.Add("Exit");
      return Task.CompletedTask;
    }
  }

  private sealed class ViewTemplate : IDataTemplate
  {
    private readonly Type _type;
    private readonly Func<Control> _factory;

    internal ViewTemplate(Type type, Func<Control> factory)
    {
      _type = type;
      _factory = factory;
    }

    public bool Match(object? data) => data?.GetType() == _type;

    public Control? Build(object? param) => _factory();
  }

  private static (AvaloniaShell shell, Router router) Create(bool windowed = false)
  {
    Application.Current!.DataTemplates.Add(new ViewTemplate(typeof(LayoutAlpha), () => new LayoutView()));
    Application.Current!.DataTemplates.Add(new ViewTemplate(typeof(PageAlpha), () => new PageView()));
    Application.Current!.DataTemplates.Add(new ViewTemplate(typeof(PageBeta), () => new PageView()));
    Application.Current!.DataTemplates.Add(new ViewTemplate(typeof(DirectorLayout), () => new DirectorLayoutView()));
    Application.Current!.DataTemplates.Add(new ViewTemplate(typeof(DirectorPage), () => new DirectorPageView()));
    Application.Current!.DataTemplates.Add(new ViewTemplate(typeof(AdaptiveDirectorPage), () => new DirectorPageView()));
    Application.Current!.DataTemplates.Add(new ViewTemplate(typeof(LoadingPage), () => new LoadingPageView()));
    Application.Current!.DataTemplates.Add(new ViewTemplate(typeof(RecordingDirectorPage), () => new RecordingDirectorView()));
    Application.Current!.DataTemplates.Add(new ViewTemplate(typeof(ThrowingDirectorPage), () => new ThrowingDirectorView()));
    Application.Current!.DataTemplates.Add(new ViewTemplate(typeof(EnterGatedDirectorPage), () => new EnterGatedDirectorView()));

    var shell = TestHost.CreateShell<NoopDirector>(s =>
    {
      s.AddSingleton<LayoutAlpha>(_ => new LayoutAlpha());
      s.AddSingleton<PageAlpha>(_ => new PageAlpha());
      s.AddSingleton<PageBeta>(_ => new PageBeta());
      s.AddSingleton<DirectorLayout>(_ => new DirectorLayout());
      s.AddSingleton<DirectorPage>(_ => new DirectorPage());
    }, rootView: windowed ? new HostWindow { Width = 900, Height = 600 } : null);
    shell.Start();
    return (shell, new Router(shell.Services));
  }

  private static Request Chain(Type layout, Type page, object? pageInstance = null)
    => new(page, null, [Target.Of(layout), Target.Of(page, instance: pageInstance)]);

  private static Request Chain3(Type outer, Type middle, Type page)
    => new(page, null, [Target.Of(outer), Target.Of(middle), Target.Of(page)]);

  /// <summary>A mount child — a platform location carrying the given view.</summary>
  private static IViewLocation<Control> Node(Control view)
    => new PlatformLocation(view.GetType(), Args.Empty, view) { View = view };

  /// <summary>A real host window — the stage joins a visual root, so the reveal's layout wait can run.</summary>
  private sealed class HostWindow : Window, IAvaloniaShellHost
  {
    public void HostShell(IAvaloniaShell shell, Control stage) => Content = stage;
  }

  // ── a convergence no view directs: the entering side is shown in the landing turn ──

  [AvaloniaFact]
  public async Task Route_WithNoDirector_ShowsTheArrivingPageInTheLandingTurn()
  {
    (AvaloniaShell shell, Router router) = Create(windowed: true);

    await router.RouteAsync(Chain(typeof(LayoutAlpha), typeof(PageAlpha)));

    // No moving-side view directs this convergence, so nothing owes the
    // entering side a laid-out-but-invisible state: the reveal runs to its end
    // in the same turn the transaction lands — no dispatcher pass to wait on,
    // and no dip to opacity 0 left behind.
    Assert.True(router.WaitIdleAsync().IsCompleted, "the reveal must not outlive the landing turn");

    var hostBody = (IBodyPanel<Control>)router.View;
    var bodyPanel = (IBodyPanel<Control>)((LayoutView)hostBody.Children[0].View!).GetBodyPanel();
    var page = Assert.IsType<PageView>(bodyPanel.ActiveChild!.View);
    Assert.True(page.IsVisible);
    Assert.Equal(1, page.Opacity);
    Assert.True(page.IsHitTestVisible);
  }

  // ── the mount point is resolved when it is first needed ──

  [AvaloniaFact]
  public async Task Route_TerminalBecomesContainer_MountsItsChildWhenItDoes()
  {
    (AvaloniaShell shell, Router router) = Create();

    // The layout arrives alone: it is the chain's terminal, so nothing mounts
    // into it and its mount point is never asked for.
    await router.RouteAsync(new Request(typeof(LayoutAlpha), null));

    var hostBody = (IBodyPanel<Control>)router.View;
    var layoutView = Assert.IsType<LayoutView>(hostBody.Children[0].View);
    var bodyPanel = (IBodyPanel<Control>)layoutView.GetBodyPanel();
    Assert.Empty(bodyPanel.Children);

    // The same layout now hosts a page — the mount point resolves on first use.
    await router.RouteAsync(Chain(typeof(LayoutAlpha), typeof(PageAlpha)));

    Assert.Single(bodyPanel.Children);
    Assert.IsType<PageView>(bodyPanel.ActiveChild!.View);
  }

  [AvaloniaFact]
  public async Task RouteAsync_EnterAnimation_RunsTheFirstDifferenceDirector_WithTheArrivingHeadInvisible()
  {
    (AvaloniaShell shell, Router router) = Create();
    await router.RouteAsync(Chain(typeof(LayoutAlpha), typeof(PageAlpha)));

    // The page is the change's first difference — the shared layout stays, so
    // the search starts at the page and only it directs.  The arriving head is
    // laid out but invisible (the prepare phase); the departing head stays.
    await router.RouteAsync(Chain(typeof(LayoutAlpha), typeof(DirectorPage)));

    var hostBody = (IBodyPanel<Control>)router.View;
    var bodyPanel = (IBodyPanel<Control>)((LayoutView)hostBody.Children[0].View!).GetBodyPanel();
    var director = Assert.IsType<DirectorPageView>(bodyPanel.ActiveChild!.View);

    Assert.Equal(["Enter"], director.Calls);
    Assert.NotNull(director.LastContext);
    Assert.Equal(TransitionKind.Enter, director.LastContext!.Kind);
    Assert.Same(director, director.LastContext.Arriving);
    Assert.NotNull(director.LastContext.Departing);
    Assert.Equal(0, director.OpacityAtEnter);
  }

  // ── a body-holding first difference is the only node the framework dips ──

  [AvaloniaFact]
  public async Task RouteAsync_BodyHoldingFirstDifferenceDirector_IsDipped_WithItsArrivingChildVisible()
  {
    (AvaloniaShell shell, Router router) = Create();

    await router.RouteAsync(Chain3(typeof(DirectorLayout), typeof(LayoutAlpha), typeof(PageAlpha)));

    var hostBody = (IBodyPanel<Control>)router.View;
    var director = Assert.IsType<DirectorLayoutView>(hostBody.Children[0].View);

    // The framework dips one node per side — the arriving head.  The child it
    // hosts is not touched; a delegating director moves the lever itself.
    Assert.Equal(["Enter"], director.Calls);
    Assert.Equal(0, director.OpacityAtEnter);
    Assert.Equal(1, director.ArrivingChildOpacityAtEnter);
    Assert.Same(director, director.LastContext!.Arriving);
    Assert.Null(director.LastContext.Departing);
  }

  // ── the director is the changed chain's first ISceneTransition: the search starts at the first difference ──

  [AvaloniaFact]
  public async Task Director_SearchStartsAtTheFirstDifference_NotTheSharedPrefix()
  {
    (AvaloniaShell shell, Router router) = Create();

    // old [] → new [DirectorLayout, LayoutAlpha, PageAlpha]: nothing is shared,
    // so the outermost node is the first difference — the layout directs.
    await router.RouteAsync(Chain3(typeof(DirectorLayout), typeof(LayoutAlpha), typeof(PageAlpha)));

    var hostBody = (IBodyPanel<Control>)router.View;
    var outerDirector = Assert.IsType<DirectorLayoutView>(hostBody.Children[0].View);
    var middle = Assert.IsType<LayoutView>(outerDirector.GetBodyPanel().Children[0].View);
    Assert.Equal(["Enter"], outerDirector.Calls);

    // old [DirectorLayout, LayoutAlpha, PageAlpha] →
    // new [DirectorLayout, LayoutAlpha, DirectorPage]: two shared prefix nodes,
    // the outermost of them a director — but re-engaged, so the search starts
    // at the first difference and the new page directs.
    await router.RouteAsync(Chain3(typeof(DirectorLayout), typeof(LayoutAlpha), typeof(DirectorPage)));

    Assert.Equal(["Enter"], outerDirector.Calls);   // the shared director is not asked again

    var pageDirector = Assert.IsType<DirectorPageView>(
      middle.GetBodyPanel().Children.Select(n => n.View).OfType<DirectorPageView>().Single());
    Assert.Same(pageDirector, pageDirector.LastContext!.Arriving);
    Assert.Equal(["Enter"], pageDirector.Calls);
  }

  [AvaloniaFact]
  public async Task Absorb_Revision_ReengagesInPlace_WithRefreshKind()
  {
    (AvaloniaShell shell, Router router) = Create();
    var page = new AdaptiveDirectorPage();

    // The layout is presented first so it is the shared prefix on the next
    // route — the adaptive page is then the change's first difference.
    await router.RouteAsync(Chain(typeof(LayoutAlpha), typeof(PageAlpha)));

    await router.RouteAsync(new Request(typeof(AdaptiveDirectorPage), new TestArgs("a1"),
      [Target.Of(typeof(LayoutAlpha)), Target.Of(typeof(AdaptiveDirectorPage), new TestArgs("a1"), page)]));

    var hostBody = (IBodyPanel<Control>)router.View;
    var bodyPanel = (IBodyPanel<Control>)((LayoutView)hostBody.Children[0].View!).GetBodyPanel();
    var director = Assert.IsType<DirectorPageView>(bodyPanel.ActiveChild!.View);
    Assert.Equal(["Enter"], director.Calls);
    Assert.Equal(0, director.OpacityAtEnter);          // a fresh arrival is prepared invisible

    await router.RouteAsync(new Request(typeof(AdaptiveDirectorPage), new TestArgs("a2"),
      [Target.Of(typeof(LayoutAlpha)), Target.Of(typeof(AdaptiveDirectorPage), new TestArgs("a2"))]));

    // The revision re-engages the same view in place: the framework does not
    // hide it, nothing departs, and the kind the director reads is Refresh.
    Assert.Equal(["Enter", "Enter"], director.Calls);
    Assert.Equal(TransitionKind.Refresh, director.LastContext!.Kind);
    Assert.Same(director, director.LastContext.Arriving);
    Assert.Null(director.LastContext.Departing);
    Assert.Equal(1, director.OpacityAtEnter);
  }

  [AvaloniaFact]
  public async Task RouteToAnAncestorSite_DepartingOnly_StillDirectsTheExit()
  {
    (AvaloniaShell shell, Router router) = Create();
    await router.RouteAsync(Chain(typeof(LayoutAlpha), typeof(PageAlpha)));
    await router.RouteAsync(Chain(typeof(LayoutAlpha), typeof(DirectorPage)));

    var hostBody = (IBodyPanel<Control>)router.View;
    var bodyPanel = (IBodyPanel<Control>)((LayoutView)hostBody.Children[0].View!).GetBodyPanel();
    var director = Assert.IsType<DirectorPageView>(bodyPanel.ActiveChild!.View);
    Assert.Equal(["Enter"], director.Calls);

    // A route whose resolved chain is a reference prefix of the presented one:
    // the page leaves and nothing arrives.
    await router.RouteAsync(new Request(typeof(LayoutAlpha), null));

    Assert.Equal(["Enter", "Exit"], director.Calls);
  }

  [AvaloniaFact]
  public async Task Back_ExitAnimation_RunsTheDepartingDirector()
  {
    (AvaloniaShell shell, Router router) = Create();

    await router.RouteAsync(Chain(typeof(LayoutAlpha), typeof(PageAlpha)));
    await router.RouteAsync(Chain(typeof(LayoutAlpha), typeof(DirectorPage)));

    await shell.DispatchIntent(null, new BackIntent());

    // Back (Exit) — the director walks the departing chain: the page that
    // is leaving directs its own exit.
    var hostBody = (IBodyPanel<Control>)router.View;
    var bodyPanel = (IBodyPanel<Control>)((LayoutView)hostBody.Children[0].View!).GetBodyPanel();
    var director = Assert.IsType<DirectorPageView>(
      bodyPanel.Children.Select(n => n.View).OfType<DirectorPageView>().Single());

    Assert.Equal(["Enter", "Exit"], director.Calls);   // enter on arrival, exit on back
    Assert.NotNull(director.LastContext);
    Assert.Equal(TransitionKind.Exit, director.LastContext!.Kind);
    Assert.Same(director, director.LastContext.Departing);
    Assert.Equal(1, director.OpacityAtExit);            // the departing head stays visible
    Assert.Equal(0, director.ArrivingOpacityAtExit);    // the arriving head is dipped
  }

  [AvaloniaFact]
  public async Task Push_ForwardTrailCleared_RemovesDroppedPageView_AndReleasesIt()
  {
    (AvaloniaShell shell, Router router) = Create();

    var pageC = new DirectorPage();
    var pageD = new PageAlpha();

    await router.RouteAsync(Chain(typeof(LayoutAlpha), typeof(PageAlpha)));
    await router.RouteAsync(Chain(typeof(LayoutAlpha), typeof(PageBeta)));
    await router.RouteAsync(Chain(typeof(LayoutAlpha), typeof(DirectorPage), pageC));   // C

    // Back to B — C stays in the forward trail, its view stays mounted.
    Assert.Equal(IntentResult.Handled, await shell.DispatchIntent(null, new BackIntent()));
    Assert.False(pageC.Released);

    var hostBody = (IBodyPanel<Control>)router.View;
    var bodyPanel = (IBodyPanel<Control>)((LayoutView)hostBody.Children[0].View!).GetBodyPanel();
    var pageCView = Assert.IsType<DirectorPageView>(bodyPanel.Children[^1].View);

    // Navigate to a fresh page — the push clears the forward trail (C):
    // the dropped chain's page is released and its view is physically
    // removed from the shared body panel.
    await router.RouteAsync(Chain(typeof(LayoutAlpha), typeof(PageAlpha), pageD));

    Assert.True(pageC.Released, "the forward-cleared page must be released");
    Assert.DoesNotContain(bodyPanel.Children, n => ReferenceEquals(n.View, pageCView));
    // The shared layout node and the live pages stay mounted (A's page, the
    // restored B page, and D's fresh page — C is gone).
    Assert.Equal(3, bodyPanel.Children.Count);
    Assert.Same(pageD, router.Model!.Current);
  }

  [AvaloniaFact]
  public async Task Back_WhilePageArrivalInFlight_SwitchesBackToPreviousPage()
  {
    (AvaloniaShell shell, Router router) = Create();

    await router.RouteAsync(Chain(typeof(LayoutAlpha), typeof(PageAlpha)));

    // Route to a page whose arrival suspends on a gate (a data load in
    // flight) — the reveal is done, the pipeline is suspended in AfterStep.
    var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var loading = new LoadingPage(gate);
    Task route = router.RouteAsync(Chain(typeof(LayoutAlpha), typeof(LoadingPage), loading));
    await loading.ArrivalStarted.Task;

    var hostBody = (IBodyPanel<Control>)router.View;
    var bodyPanel = (IBodyPanel<Control>)((LayoutView)hostBody.Children[0].View!).GetBodyPanel();
    Assert.IsType<LoadingPageView>(bodyPanel.ActiveChild!.View);

    // Back while the arrival is still in flight — the traversal must
    // switch the presented page back immediately.
    Task back = shell.DispatchIntent(null, new BackIntent()).AsTask();

    gate.SetResult();
    await Task.WhenAll(route, back);

    // The screen is back on the previous page; the loading page is hidden.
    Assert.IsType<PageView>(bodyPanel.ActiveChild!.View);
    Assert.False(bodyPanel.Children.First(n => n.View is LoadingPageView).View!.IsVisible);
  }

  [AvaloniaFact]
  public async Task Back_WhileArrivalInFlight_WithRecordingDirector_SwitchesBack()
  {
    (AvaloniaShell shell, Router router) = Create();

    await router.RouteAsync(Chain(typeof(LayoutAlpha), typeof(PageAlpha)));

    var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var loading = new RecordingDirectorPage(gate);
    Task route = router.RouteAsync(Chain(typeof(LayoutAlpha), typeof(RecordingDirectorPage), loading));
    await loading.ArrivalStarted.Task;

    var hostBody = (IBodyPanel<Control>)router.View;
    var bodyPanel = (IBodyPanel<Control>)((LayoutView)hostBody.Children[0].View!).GetBodyPanel();
    var director = Assert.IsType<RecordingDirectorView>(bodyPanel.ActiveChild!.View);
    Assert.Equal(["Enter"], director.Calls);

    Task back = shell.DispatchIntent(null, new BackIntent()).AsTask();

    gate.SetResult();
    await Task.WhenAll(route, back);

    // A non-throwing director must not block the switch.
    Assert.Equal(["Enter", "Exit"], director.Calls);
    Assert.IsType<PageView>(bodyPanel.ActiveChild!.View);
    Assert.False(bodyPanel.Children.First(n => n.View is RecordingDirectorView).View!.IsVisible);
  }

  [AvaloniaFact]
  public async Task Back_WhileArrivalInFlight_WithThrowingDirector_StillSwitchesBack()
  {
    (AvaloniaShell shell, Router router) = Create();

    await router.RouteAsync(Chain(typeof(LayoutAlpha), typeof(PageAlpha)));

    var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var loading = new ThrowingDirectorPage(gate);
    Task route = router.RouteAsync(Chain(typeof(LayoutAlpha), typeof(ThrowingDirectorPage), loading));
    await loading.ArrivalStarted.Task;

    var hostBody = (IBodyPanel<Control>)router.View;
    var bodyPanel = (IBodyPanel<Control>)((LayoutView)hostBody.Children[0].View!).GetBodyPanel();
    var director = Assert.IsType<ThrowingDirectorView>(bodyPanel.ActiveChild!.View);
    Assert.Equal(["Enter"], director.Calls);

    Task back = shell.DispatchIntent(null, new BackIntent()).AsTask();

    gate.SetResult();
    await Task.WhenAll(route, back);

    // The director's exit failure must be contained: the reveal still
    // finishes its final visibility, the previous page shows.
    Assert.Equal(["Enter", "Exit"], director.Calls);
    Assert.IsType<PageView>(bodyPanel.ActiveChild!.View);
    Assert.False(bodyPanel.Children.First(n => n.View is ThrowingDirectorView).View!.IsVisible);
  }

  [AvaloniaFact]
  public void SettleActive_HidesEveryChildExceptTheActiveOne()
  {
    var body = new BodyPanel();
    IBodyPanel<Control> host = body;
    var staleA = Node(new PageView());
    var active = Node(new PageView());
    var staleC = Node(new PageView());

    host.Add(staleA);
    host.Add(active);
    host.Add(staleC);
    host.SetActiveChild(active);

    // Force the stale state a broken choreography would leave: non-active
    // children still visible.  The settle converges to the active child.
    staleA.View!.IsVisible = true;
    staleC.View!.IsVisible = true;

    host.SettleActive();

    Assert.False(staleA.View!.IsVisible);
    Assert.True(active.View!.IsVisible);
    Assert.False(staleC.View!.IsVisible);
  }

  [AvaloniaFact]
  public async Task Back_WhileEnterAnimationInFlight_ConvergesToBackTarget()
  {
    (AvaloniaShell shell, Router router) = Create();

    await router.RouteAsync(Chain(typeof(LayoutAlpha), typeof(PageAlpha)));

    // Route to a director page whose enter animation suspends on a gate —
    // the forward reveal is in flight, its final visibility still pending.
    var page = new EnterGatedDirectorPage();
    Task route = router.RouteAsync(Chain(typeof(LayoutAlpha), typeof(EnterGatedDirectorPage), page));

    var hostBody = (IBodyPanel<Control>)router.View;
    var bodyPanel = (IBodyPanel<Control>)((LayoutView)hostBody.Children[0].View!).GetBodyPanel();
    var director = Assert.IsType<EnterGatedDirectorView>(bodyPanel.ActiveChild!.View);
    Assert.Equal(["Enter"], director.Calls);   // the enter animation is in flight

    // Back while the forward reveal is still animating — the traversal
    // switches back, and the late forward reveal must not re-show the
    // departing page.
    Task back = shell.DispatchIntent(null, new BackIntent()).AsTask();

    page.EnterGate.SetResult();
    await Task.WhenAll(route, back);

    Assert.IsType<PageView>(bodyPanel.ActiveChild!.View);
    Assert.False(bodyPanel.Children.First(n => n.View is EnterGatedDirectorView).View!.IsVisible);
  }
}
