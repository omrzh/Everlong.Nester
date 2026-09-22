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
      s.AddSingleton<DirectorPage>(_ => new DirectorPage());
    }, rootView: windowed ? new HostWindow { Width = 900, Height = 600 } : null);
    shell.Start();
    return (shell, new Router(shell.Services));
  }

  private static Request Chain(Type layout, Type page, object? pageInstance = null)
    => new(page, null, [Target.Of(layout), Target.Of(page, instance: pageInstance)]);

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
  public async Task RouteAsync_EnterAnimation_RunsTheChainDirector_WithArrivingInvisible()
  {
    (AvaloniaShell shell, Router router) = Create();

    await router.RouteAsync(Chain(typeof(LayoutAlpha), typeof(DirectorPage)));

    // The page is the changed chain's first ISceneTransition (outermost
    // first) — the reveal selects it and runs its enter animation with the
    // arriving chain laid out but invisible (the prepare phase).
    var hostBody = (IBodyPanel<Control>)router.View;
    var bodyPanel = (IBodyPanel<Control>)((LayoutView)hostBody.Children[0].View!).GetBodyPanel();
    var director = Assert.IsType<DirectorPageView>(bodyPanel.Children[0].View);

    Assert.Equal(["Enter"], director.Calls);
    Assert.NotNull(director.LastContext);
    Assert.Equal(TransitionKind.Enter, director.LastContext!.Kind);
    Assert.Contains(director, director.LastContext!.ArrivingChain);
    Assert.Empty(director.LastContext.DepartingChain);
    Assert.Equal(0, director.OpacityAtEnter);
  }

  [AvaloniaFact]
  public async Task Absorb_Revision_ReengagesInPlace_WithRefreshKind()
  {
    (AvaloniaShell shell, Router router) = Create();
    var page = new AdaptiveDirectorPage();

    await router.RouteAsync(new Request(typeof(AdaptiveDirectorPage), new TestArgs("a1"),
      [Target.Of(typeof(LayoutAlpha)), Target.Of(typeof(AdaptiveDirectorPage), new TestArgs("a1"), page)]));

    var hostBody = (IBodyPanel<Control>)router.View;
    var bodyPanel = (IBodyPanel<Control>)((LayoutView)hostBody.Children[0].View!).GetBodyPanel();
    var director = Assert.IsType<DirectorPageView>(bodyPanel.Children[0].View);
    Assert.Equal(["Enter"], director.Calls);
    Assert.Equal(0, director.OpacityAtEnter);          // a fresh arrival is prepared invisible

    await router.RouteAsync(new Request(typeof(AdaptiveDirectorPage), new TestArgs("a2"),
      [Target.Of(typeof(LayoutAlpha)), Target.Of(typeof(AdaptiveDirectorPage), new TestArgs("a2"))]));

    // The revision re-engages the same view in place: the framework does not
    // hide it, nothing departs, and the kind the director reads is Refresh.
    Assert.Equal(["Enter", "Enter"], director.Calls);
    Assert.Equal(TransitionKind.Refresh, director.LastContext!.Kind);
    Assert.Contains(director, director.LastContext.ArrivingChain);
    Assert.Empty(director.LastContext.DepartingChain);
    Assert.Equal(1, director.OpacityAtEnter);
  }

  [AvaloniaFact]
  public async Task RouteToAnAncestorSite_DepartingOnly_StillDirectsTheExit()
  {
    (AvaloniaShell shell, Router router) = Create();
    await router.RouteAsync(Chain(typeof(LayoutAlpha), typeof(DirectorPage)));

    var hostBody = (IBodyPanel<Control>)router.View;
    var bodyPanel = (IBodyPanel<Control>)((LayoutView)hostBody.Children[0].View!).GetBodyPanel();
    var director = Assert.IsType<DirectorPageView>(bodyPanel.Children[0].View);
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
    Assert.Contains(director, director.LastContext!.DepartingChain);
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
