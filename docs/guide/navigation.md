# Navigation — locators, layouts, arguments, lifecycle

> How an application navigates with Nester. The engine underneath is specified in
> `docs/design/routing.md`; the intent protocol a gesture travels through is `docs/design/intent.md`.
> The template's `Pages/Posts/` is the worked example for everything on this page.

## 1. A destination is a ViewModel type

There are no route records. `[Routable]` (or `[Routable<TArgs>]` when the page carries arguments) and
`[Layout<T>]` are the whole declaration, and the generator emits a sealed locator beside the type.

```csharp
[Routable]
[Layout<MainLayoutModel>]
[Transient]
public partial class PostsPageModel : RoutableModel;

// the generator's half, available as `new PostsLocator()`
```

Layouts nest transitively — `[Layout<SettingsLayoutModel>]` where that layout is itself
`[Layout<MainLayoutModel>]` presents `MainLayout → SettingsLayout → page`. The chain is a compile-time
fact: the runtime never reflects to discover it.

```csharp
[Routable<PostDetailArgs>]
[Layout<MainLayoutModel>]
public partial class PostDetailPageModel : ParameterizedModel<PostDetailArgs>;
```

A locator is immutable and never mutated by navigation, so the same descriptor can be routed twice and
produce two independent presentations.

### The mount point

A view that can be a non-terminal chain node declares where the node below it mounts: it implements
`ILayoutControl` and returns the `LayoutBody` from its XAML.

```csharp
public partial class MainLayout : UserControl, ILayoutControl
{
  public ILayoutBody GetLayoutBody() => Body;   // the x:Name="Body" element
}
```

```xml
<n:LayoutBody x:Name="Body" />
```

That contract is the only source for a mount point — the framework never searches the view's tree, and
the element's name means nothing to it (it is only how the code-behind reaches the element). A view
whose chain position is never a parent needs no declaration; one that is a parent and declares none
leaves its child unmounted.

`LayoutBody` is a panel: every visited page stays in it as a child, and only the active one is visible.
Keep it attached to the view's own visual tree — the framework mounts into it directly, and a body
outside the tree renders nothing.

## 2. Navigating

`Router` comes from the model base (`RoutableModel.Router`) or from `[Inject]`.

| Call | Meaning |
|---|---|
| `Router.RouteAsync(locator)` | Route by description. Fire-and-forget: it commits the route, no result is bridged back. A live engagement with equal arguments is reused; a dead one is re-materialized. |
| `Router.JumpAsync(site)` | Record a fresh visit to an **existing** engagement (an `ILocation` the stack handed you). No resolution, no re-materialization. Returns `false` when no live entry presents the site — then rebuild it: `Router.RouteAsync(site.ToLocator())`. A visit that faults reaches the caller as a fault: `false` is never a swallowed failure. |
| `Router.Derive(isEphemeral: false)` | Open a result-capable derived router. Capsules, dialogs and floating pages are built on it (`docs/guide/interaction.md`). |
| `Router.Stack` | The navigation-state surface: `Location`, `CanGoBack`, `CanGoForward`, `Count`, `MaxDepth`, `TrimBackward()`, `TrimForward()`, `PeekPrevious/Next`, `BackStack`, `ForwardStack`, `Snapshot`. Its state members raise `PropertyChanged`, so they bind directly to a button's `IsEnabled`. |

Back, forward and refresh are **intents**, not router calls — a layout control never navigates
directly, because user input must stay attributable and refusable:

```csharp
[RelayCommand] private Task GoBack()    => Shell.DispatchIntent(this, new BackIntent()).AsTask();
[RelayCommand] private Task GoForward() => Shell.DispatchIntent(this, new ForwardIntent()).AsTask();
[RelayCommand] private Task Refresh()   => Shell.DispatchIntent(this, new RefreshIntent()).AsTask();
```

The distinction matters: `RouteAsync(locator)` is an addressed directive that consults nobody, while
`RouteIntent(locator)` is the consulted form a page may veto.

## 3. Arguments

An argument carrier is a record deriving from `Args`; value equality comes with it.

```csharp
public sealed record PostDetailArgs(Post Post) : Args;
```

`ParameterizedModel<TArgs>` delivers the arguments into the abstract `OnArgsDelivered()` and exposes
them as `EngagedArgs`:

```csharp
[Routable<PostDetailArgs>]
public partial class PostDetailPageModel : ParameterizedModel<PostDetailArgs>
{
  [ObservableProperty] public partial Post Post { get; private set; } = Post.Empty;

  protected override void OnArgsDelivered()
  {
    if (EngagedArgs is { } args) Post = args.Post;
  }
}
```

**The engagement is the node's identity.** A route matches a held instance by the value it serves (or by
reference when you pass an explicit `instance:`): a request equal to the served value reuses that
instance with no re-arrival. `EngagedArgs` is what the instance actually serves — it may differ from
the request when `OnArgsDelivered` substitutes a default for an unspecified or invalid request — and
the tree keys on that served value, so the same value reuses the same node.

Value-different arguments land a fresh instance — unless the participant implements
`IAdaptiveParameterized` and answers `IsAdaptable(requested)`. Then the revision lands in place: the
same instance is delivered the new arguments through the same `OnArgsDelivered` hook (before the
membership notifications) and its arrival replays. `AdaptiveParameterizedModel<TArgs>` accepts any
request of the model's own argument type; implement `IAdaptiveParameterized` directly to refuse
selectively.

## 4. Lifecycle: commit, then converge

The route commits first — the state change is irreversible. The UI then converges onto it: views
assemble, transitions play, hooks run. Three consequences follow directly:

- Nothing a hook does can roll the commit back. A throwing hook is quarantined and reported through
  the router's error channel; the convergence continues for the other participants.
- A newer navigation supersedes an in-flight convergence — `context.Lifetime` fires and the newer
  commit's convergence takes over. A navigation that aborts before its commit fires it too, and so does
  the end of the convergence it belongs to: a token handed to slow work never outlives the presentation
  that work serves.
- A terminal close is not a transition: it runs a release-only convergence (no departure hooks) and
  the chain dies as a whole.

| Contract | When it runs |
|---|---|
| `IArriving.OnArrivingAsync(context)` | Pre-arrival, awaited before the reveal. Fires on every arrival, including re-visits — a reset point that may borrow time. |
| `IArrived.OnArrivedAsync(context)` | The UI-thread arrival point: load data, apply state. The guaranteed path. |
| `IBodyChanged.OnBodyChanged(bodyChain)` | Synchronously at the commit, before any arrival/departure hook, when the **membership** of the chain *below* this participant changed. The body chain is outermost-first, so a layout can rebuild breadcrumbs or a title from it. The body participants have not arrived yet. A page revised in place keeps its place in the chain, so this stays silent for it — read the value it now serves from the instance (`EngagedArgs`) or from `Router.Stack`, or bind to the body's own state. |
| `IDeparting.OnDeparting(context)` | Synchronous pre-departure. |
| `IDeparted.OnDeparted(context)` | Synchronous cleanup when navigating away (the instance stays retained). |
| `IReleasable.Release()` | The visit is evicted from the stack — permanent removal. |

`IRoutingContext` carries `Direction` (`Route` / `Back` / `Forward` / `Refresh` / `Close` / `Jump`),
`Arrival` / `Departure` (`ILocation`), `Arrivings` / `Departings` — the chains this transfer moves,
outermost first — `IsElevated`, and `Lifetime`, the token that bounds this navigation and cancels slow
work. It fires when a newer navigation lands, when this one abandons before its commit, when its
convergence ends, and when the router tears down.

### The model base

`RoutableModel` (in `Everlong.Nester.Extensions`) implements the plumbing and turns the interfaces into
overridable hooks. It adds `[Inject] Shell`, `[Inject] Router`, the routing state
(`HasBeenRouted`, `HasBeenArrived`, `IsActiveLocation`) and:

```csharp
protected virtual void OnRoutedTo(IRoutingContext context, bool isFirstRouted) { }
protected virtual void OnRoutedFrom(IRoutingContext context) { }
protected virtual Task OnArrivedAsync(IRoutingContext context, bool isFirstArrived)
  => Task.CompletedTask;
protected virtual void OnReleased() { }
```

`OnRoutedTo` runs at the truth — before the arrival ceremonies — which makes it the earliest hook and
the right place to start a background load. The template's `PostDetailPageModel` is the pattern
written out: fire the load in `OnRoutedTo`, wait a bounded budget for it in `OnArrivingAsync`, apply
the settled result in `OnArrivedAsync`.

`ParameterizedModel<TArgs>` adds `IParameterized` + `EngagedArgs` + `OnArgsDelivered()`;
`DialogSessionBase[<TResult>]` adds the result write-back channel for dialogs.

### Threading

Lifecycle callbacks run on the UI thread. Never block with `.Result`, `.Wait()` or `Thread.Sleep()` —
that starves the router's pump. A navigation started *inside* a lifecycle callback is queued rather
than re-entered: fire-and-forget never deadlocks, but it supersedes the current run's convergence, so
use `MainDispatcher.TryPost(...)` (or `Dispatcher.UIThread.Post`) when the current run must settle
first.

## 5. Guards

Implement `IIntentHandler` and return `true` (or call `context.Veto(this)`) to consume an intent:

```csharp
public partial class ProfilePageModel : RoutableModel, IIntentHandler
{
  public async ValueTask HandleAsync(IntentContext context, IntentDelegate next)
  {
    if (context.Intent is not BackIntent) { await next(context); return; }

    if (!await Router.ConfirmAsync(Lang.Pages.LeaveWithoutSaving))
    {
      context.Veto(this);          // the back does not run
      return;
    }

    await next(context);
  }
}
```

The router asks the presented chain first — view, then instance, outermost first — so a page vetoes a
shell close probe (`TryCloseIntent`) or an intent-dispatched route command (`RouteIntent`) the same
way. A direct `Router.RouteAsync(locator)` is an addressed directive that consults no one; a layer
that must block navigation behind it does so at the input level (a modal focus trap).

## 6. Retention

The router's tree — the current chain plus the retained forward/back trail — is the only retention.
There is no instance cache to manage, and an instance is released only when it is evicted.

There is no route-level retention declaration. A page owns its policy and drives it on the stack:

```csharp
public void OnDeparted(IRoutingContext context)
{
  if (context.Direction == RoutingDirection.Back)
    Router.Stack.TrimForward();     // backing out drops the forward trail
}
```

A one-shot *surface* is the other lever, and it is derived explicitly:
`Router.Derive(isEphemeral: true)` takes one route request and hands later ones off instead of
stacking them. `PresentOnDerivedAsync` does exactly that for dialogs.

## 7. Next

- Dialogs, floating pages, notices, messaging, auth, settings and i18n: `docs/guide/interaction.md`.
- Why the engine is shaped this way (descriptors, plans, sites, the two pipes): `docs/design/routing.md`.
