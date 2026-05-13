using Everlong.Nester.Intent;
using Everlong.Nester.Routing;
using Xunit;

namespace Everlong.Nester.Tests.Routing;

/// <summary>
///   The page-inner-stack pattern (`docs/design/routing.md` §11): a
///   parameterized page keeps a private history of its own argument
///   instances and consumes Back/Forward through its
///   <see cref="IIntentHandler" /> — the outer stack stays untouched until
///   the inner history exhausts, and an outer re-adoption resets it.
/// </summary>
public class InnerStackTests
{
  /// <summary>A code-page-like participant — private inner history over its own args type.</summary>
  private sealed class InnerPage : IParameterized, IIntentHandler
  {
    private readonly List<TestArgs> _inner = [];
    private readonly List<TestArgs> _forward = [];

    internal TestArgs Current => _inner[^1];

    internal bool InnerCanGoBack => _inner.Count > 1;

    internal bool InnerCanGoForward => _forward.Count > 0;

    /// <summary>An inner navigation — the page's own choice, never told to the router.</summary>
    internal void Push(TestArgs position)
    {
      _forward.Clear();
      _inner.Add(position);
    }

    IArgs? IParameterized.EngagedArgs => _inner.Count > 0 ? _inner[^1] : null;

    void IParameterized.DeliverArgs(IArgs? args)
    {
      // The engagement boundary — inner history is scoped to one outer
      // adoption and resets here.
      _inner.Clear();
      _forward.Clear();
      _inner.Add((TestArgs)args!);
    }

    public ValueTask HandleAsync(IntentContext context, IntentDelegate next)
    {
      switch (context.Intent)
      {
        case BackIntent when _inner.Count > 1:
          // "I handled the Back inside my page — the router need not."
          _forward.Add(_inner[^1]);
          _inner.RemoveAt(_inner.Count - 1);
          context.Handle(this);
          return ValueTask.CompletedTask;
        case ForwardIntent when _forward.Count > 0:
          _inner.Add(_forward[^1]);
          _forward.RemoveAt(_forward.Count - 1);
          context.Handle(this);
          return ValueTask.CompletedTask;
        default:
          return next(context);   // inner history exhausted — the outer stack's turn
      }
    }
  }

  [Fact]
  public async Task InnerBack_IsConsumed_OuterStackUntouched()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var page = new InnerPage();
    await router.RouteAsync(new Request(typeof(InnerPage), new TestArgs("file"), [Target.Of(typeof(InnerPage), new TestArgs("file"), page)]));
    Assert.Equal(1, router.Stack.Count);
    page.Push(new TestArgs("p2"));
    page.Push(new TestArgs("p3"));
    Assert.True(page.InnerCanGoBack);

    Assert.Equal(IntentResult.Handled, await shell.DispatchIntent(null, new BackIntent()));
    Assert.Equal(new TestArgs("p2"), page.Current);   // inner pop
    Assert.Equal(1, router.Stack.Count);             // the outer stack never moved

    Assert.Equal(IntentResult.Handled, await shell.DispatchIntent(null, new BackIntent()));
    Assert.Equal(new TestArgs("file"), page.Current);  // back to the inner base
    Assert.Equal(1, router.Stack.Count);
    Assert.False(page.InnerCanGoBack);
  }

  [Fact]
  public async Task InnerExhausted_BackPassesToTheOuterStack()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var page = new InnerPage();
    await router.RouteAsync(new Request(typeof(PageAlpha), null, [Target.Of(typeof(PageAlpha))]));
    await router.RouteAsync(new Request(typeof(InnerPage), new TestArgs("file"), [Target.Of(typeof(InnerPage), new TestArgs("file"), page)]));
    page.Push(new TestArgs("p2"));

    // Inner first — consumed by the page.
    Assert.Equal(IntentResult.Handled, await shell.DispatchIntent(null, new BackIntent()));
    Assert.Equal(new TestArgs("file"), page.Current);
    Assert.Equal(2, router.Stack.Count);

    // Inner exhausted — the page passes; the router traverses back to the
    // alpha page (entries retained, the pointer moves).
    Assert.Equal(IntentResult.Handled, await shell.DispatchIntent(null, new BackIntent()));
    Assert.Equal(2, router.Stack.Count);
    Assert.IsType<PageAlpha>(router.Model.CurrentChain![0].Instance);
    Assert.False(router.Model.CanGoBack);
  }

  [Fact]
  public async Task InnerForward_RestoresThePoppedPosition()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var page = new InnerPage();
    await router.RouteAsync(new Request(typeof(InnerPage), new TestArgs("file"), [Target.Of(typeof(InnerPage), new TestArgs("file"), page)]));
    page.Push(new TestArgs("p2"));
    await shell.DispatchIntent(null, new BackIntent());   // inner back to base
    Assert.Equal(new TestArgs("file"), page.Current);

    Assert.Equal(IntentResult.Handled, await shell.DispatchIntent(null, new ForwardIntent()));
    Assert.Equal(new TestArgs("p2"), page.Current);       // inner forward restored
    Assert.Equal(1, router.Stack.Count);                 // outer untouched
  }

  [Fact]
  public async Task OuterReAdoption_ResetsTheInnerHistory()
  {
    (FakeShell shell, TestRouter router) = RouterTestHost.Create();
    var page = new InnerPage();
    await router.RouteAsync(new Request(typeof(InnerPage), new TestArgs("file"), [Target.Of(typeof(InnerPage), new TestArgs("file"), page)]));
    page.Push(new TestArgs("p2"));
    page.Push(new TestArgs("p3"));

    // A stranger re-routes the page — a non-adaptive page never absorbs, so
    // a fresh instance adopts the new file and its inner history starts at
    // the base.
    await router.RouteAsync(new Request(typeof(InnerPage), null, [Target.Of(typeof(InnerPage), new TestArgs("other"))]));
    var fresh = (InnerPage)router.Model!.CurrentChain![0].Instance;
    Assert.NotSame(page, fresh);
    Assert.Equal(new TestArgs("other"), fresh.Current);
    Assert.False(fresh.InnerCanGoBack);
  }
}
