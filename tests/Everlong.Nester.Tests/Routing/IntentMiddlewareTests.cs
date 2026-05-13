using Everlong.Nester.Intent;
using Everlong.Nester.Shell;
using Everlong.Nester.Routing;
using Xunit;

namespace Everlong.Nester.Tests.Routing;

/// <summary>
///   A node view participating in the intent chain — veto in the tunnel
///   phase, observation of the settled outcome in the bubble phase.
/// </summary>
internal sealed class IntentChainView : IIntentHandler
{
  private readonly bool _vetoesBack;

  internal IntentChainView(bool vetoesBack = false) => _vetoesBack = vetoesBack;

  internal List<string> Events { get; } = [];

  /// <inheritdoc />
  public async ValueTask HandleAsync(IntentContext context, IntentDelegate next)
  {
    Events.Add($"tunnel:{context.Intent.GetType().Name}");
    if (context.Intent is BackIntent && _vetoesBack)
    {
      context.Veto();
      return;
    }
    await next(context);
    Events.Add($"observe:{context.Result}:{(context.HandledBy?.GetType().Name ?? "none")}");
  }
}

/// <summary>A router whose view fills each node's view slot (the platform's assemble step).</summary>
internal sealed class ViewAssemblingRouter : RouterBase
{
  internal ViewAssemblingRouter(IShell shell, Func<Type, object> viewFor)
    : this(shell, shell.Services, viewFor)
  {
  }

  /// <summary>The derived variant — a derived router carrying the same assembly view.</summary>
  internal ViewAssemblingRouter(IShell shell, IServiceProvider services, Func<Type, object> viewFor)
    : base(services, new TestStackModel(new TestNavigationView(assemble: truth =>
    {
      foreach (Location node in truth)
        node.Presenter ??= viewFor(node.Type);
    })))
  {
  }
}

/// <summary>
///   The middleware intent chain: the tunnel phase reaches views before the
///   instances, a terminating handler short-circuits the derived router, and the
///   bubble phase observes the settled outcome.
/// </summary>
public class IntentMiddlewareTests
{
  private static Request ChainOf(Type page)
    => new(page, null, [Target.Of(page)]);

  /// <summary>A derived router's view vetoes the external back — the derived router stays open.</summary>
  [Fact]
  public async Task DerivedView_VetoesBack_DerivedStaysOpen()
  {
    var view = new IntentChainView(vetoesBack: true);
    var shell = new FakeShell();
    var router = new ViewAssemblingRouter(shell, _ => view);
    shell.RouterFactory = sp => new ViewAssemblingRouter(shell, sp, _ => view);
    var content = new TestContent();
    var request = new Request(typeof(TestContent), null,
      [Target.Of(typeof(TestContent), instance: content)]);

    IRouter result = router.Derive();
    await result.RouteAsync(request);

    IntentResult handled = await shell.DispatchIntent(null, new BackIntent(null));

    Assert.Equal(IntentResult.Vetoed, handled);             // vetoed by the vetoing view
    Assert.False(result.Completion!.Result.IsCompleted); // the derived router stays open
    Assert.Equal(["tunnel:BackIntent"], view.Events);
  }

  /// <summary>A non-vetoing view observes the node pipeline's settled veto in the bubble phase.</summary>
  [Fact]
  public async Task GroundView_ObservesTheSettledVeto()
  {
    var view = new IntentChainView();
    var shell = new FakeShell();
    var router = new ViewAssemblingRouter(shell, _ => view);
    shell.RouterFactory = sp => new ViewAssemblingRouter(shell, sp, _ => view);
    var guard = new PageAlpha { Mandatory = true };
    await router.RouteAsync(new Request(typeof(PageAlpha), null,
      [Target.Of(typeof(PageAlpha), instance: guard)]));

    IntentResult handled = await shell.DispatchIntent(null, new BackIntent());

    Assert.Equal(IntentResult.Vetoed, handled);
    Assert.Same(guard, router.Model!.Current);   // the traversal did not run
    Assert.Contains(view.Events, e => e == "tunnel:BackIntent");
    Assert.Contains(view.Events, e => e.StartsWith("observe:Vetoed:"));
  }

  /// <summary>The derived layer is consulted before the ground — a consuming derived router short-circuits the rest.</summary>
  [Fact]
  public async Task DerivedLayer_IsAskedBeforeTheGround_AndShortCircuits()
  {
    var capsuleView = new IntentChainView();
    var groundView = new IntentChainView();
    var shell = new FakeShell();
    var router = new ViewAssemblingRouter(shell,
      t => t == typeof(PageAlpha) ? groundView : capsuleView);
    shell.RouterFactory = sp => new ViewAssemblingRouter(shell, sp,
      t => t == typeof(PageAlpha) ? groundView : capsuleView);

    await router.RouteAsync(ChainOf(typeof(PageAlpha)));
    var content = new TestContent();
    IRouter result = router.Derive();
    await result.RouteAsync(new Request(typeof(TestContent), null,
      [Target.Of(typeof(TestContent), instance: content)]));

    IntentResult handled = await shell.DispatchIntent(null, new BackIntent("ok"));

    Assert.Equal(IntentResult.Handled, handled);
    Assert.Equal("ok", await result.Completion!.Result);  // the derived router closed
    Assert.Contains(capsuleView.Events, e => e == "tunnel:BackIntent");
    Assert.Empty(groundView.Events);          // the ground was never consulted
  }
}
