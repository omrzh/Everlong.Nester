# Changelog

An entry is written per release, newest first, and says what a consumer has to act on — a required
migration, a renamed package, a raised framework floor — not what the commit log did. The version is
`AppVersion` in `NesterVersion.props`, and the seven packages and the template package carry it.

Below 1.0 nothing is promised stable: a minor version may rename a type or change a contract, and an
entry says so when it does. That is the one thing a consumer should assume rather than read here.

## 0.1.13 — 2026-10-03

**Behaviour.** The cross-layer origin is the interaction's, not the foreground's.  A surface took its
anchor from the element focus at the moment layer focus left it, which is not the interaction: the
command behind the trigger may await, and the arriving layer mounts before the transfer, so anything
that ran in between — a recycled container, a control that focuses as it mounts — emptied the anchor and
every cross-layer transition fell back to its no-origin path.  A surface records the interactions it sees
inside itself — focus arriving on a control, and a control's activation — and keeps the newest, falling
back to the element focus it held when it lost the foreground.  `IFocusAnchor.Anchor` reads the same
either way, and both are spent once the surface holds the foreground again.

A consumer that parked an origin of its own for a cross-layer flight — a window slot, an attached
property, a click handler that stored the control — can drop it and read the counterpart again
(`TransitionContext.Counterpart`, its content as `IFocusAnchor`).  An interaction the surface cannot see
still leaves only the fallback: a control inside a popup, and a gesture that neither takes focus nor
activates a control.

## 0.1.12 — 2026-10-03

Nothing between 0.1.10 and this one shipped — no tag exists since `v0.1.10` — so this entry carries the
whole span: the layer bands become planes with one focus grant, a derived router takes options, a
transition is directed by its first difference, the shell's close and its bounds are renamed, the host
window owns the state snapshot, and the notice chrome moves onto a panel.

**Migrate.** `KnownLayers`, `LayerBand` and `LayerPolicy` are gone. A lease is granted in a
`LayerPlane` — `Ground`, `Base`, `Dock`, `Overlay`, `Notice`, `Debug` or `Ghost` — and the plane
decides both the reserved z slice and the stacking behaviour. A stacking plane (`Dock`, `Overlay`,
`Debug`) grants one above its highest live lease; every other plane grants its floor. Callers name a
plane and nothing else.

**Migrate.** `ILayerBroker` has one grant: `Acquire(tenant, content, plane)`. The `(band, policy)`
overload and the exact-`z` overload are removed — the position policy is the plane's business, and an
exact slot is a platform seam, not a public entry.

**Migrate.** `Acquire` returns an `ILayerHandle`, not an `ILayerLease`. The handle carries the
read-only `Lease` and the slot's mutation — `SetVisible`, `SetIntentHandler`, `Release`; `ILayerLease`
is read-only, so `Focused` and `OnEvictedAsync` hand out a lease that cannot be escalated to a write.
The content is fixed at the grant: the handle has no `SetContent`, and a surface the tenant needs to
replace takes a fresh lease. `RequestFocus` takes the handle, and the platform factory is
`CreateHandle(ledger, content, plane, z)`.

**Migrate.** `ILayerLease.IsActive` is renamed `IsLive` (liveness), and the lease now reports its
`Plane`. "Active" was already spoken for by the app-activation domain and by the window's active
state, and the layer domain's new foreground concept is **layer focus**, not activation.

**New.** Layer focus is the single foreground grant: `ILayerBroker.Focused` reports the holder and
`ILayerBroker.RequestFocus` lets a live layer claim it. A layer opts in by having its content implement
`IFocusableContent` — `TryFocus` accepts or declines per transfer, and `OnFocusing` / `OnFocused` /
`OnUnfocusing` / `OnUnfocused` are the pre/post transfer hooks, each handed a `LayerFocusContext` that
names what moved the focus. A layer whose content does not implement the capability never takes focus.
The platform routing surface is where the hooks land, so it is also where the element focus is saved
before a transfer and restored after one.

**Migrate.** `IRouter.Derive(bool isEphemeral)` is gone. A derived router is created from
`DeriveOptions`: the `Plane` its lease is granted in (default `Overlay`) and the `Parents` every route
is completed with. The parents are the overlay's default layouts — prepended to every route it computes
and reused across its entries, so a dialog's dimmer outlives the stages it wraps. The one-shot surface
is retired: a derived router stacks, traverses and closes like any other, a caller that wants the
ground addresses it directly, and `IRouterSeed.IsEphemeral` goes with the flag. `PresentOnDerivedAsync`
takes the options first.

**Migrate.** `IRouterSeed` follows: `Band` and `Policy` collapse to `Plane`, `IsEphemeral` is gone, and
`Borrowed` goes too — the site the overlay was said to borrow had no reader. What the seed carries
instead is the lease of the layer the router was derived from, `IRouterSeed.Counterpart`, and
`IRouterSeed.Initialize` takes the plane, the scope, the parents and the counterpart.

**Migrate.** `RouteIntent` is removed. A route is an addressed directive — `RouteAsync` — and is never
dispatched as an intent; the consulted form survives for traversal alone. `IRouteIntent` is renamed
`ITraversalIntent` — it marks `BackIntent`, `ForwardIntent` and `RefreshIntent`.

**Migrate.** The transition anchor is the surface's own, not a value a view parks in a window slot:
`FlyingCanvas.Anchor` and the `n:Transition.Anchor` attached property (`Transition` in both extensions
packages) are gone. A layer's content that implements `IFocusAnchor` exposes the element it held when it
lost layer focus, and a transition that crosses layers is handed the source layer's lease through
`TransitionContext.Counterpart`. `TransitionContext.IsCrossLayer` says whether the transition crosses
layers, so a director can key its choreography on it.

**Migrate.** A director is the moving side's first-difference node, not the outermost view that
implements `ISceneTransition`, and `TransitionContext` carries the two moving **heads** instead of the
chains: `Arriving` and `Departing` replace `ArrivingChain`, `DepartingChain`, `ArrivingHead` and
`DepartingHead`; `ShowArriving` / `HideDeparting` touch that one node; `HideArriving`, `RevealBefore`,
`NextDirectorAfter` and `ScopedFrom` are gone. A change whose first difference does not implement
`ISceneTransition` runs no Transition phase and settles to its final visibility in the landing turn, so a
fresh shell that only hosts a page animates the page only when the shell itself directs. A director that
needs the whole side hidden moves the lever down itself as it delegates; the shipped `PassThroughAsync` /
`PassExitAsync` walk the body tree for that.

**New.** A shell that directs only by passing the change through declares `IPassThroughTransition`
(a `PControl` `IBodyHolder` in the extensions packages) instead of forwarding the two `ISceneTransition`
members by hand: they are defaulted to the delegation above.

**Migrate.** `TryCloseIntent` is gone — every intent is refusable, so `CloseIntent` alone stands for a
user close affordance, and the imperative close the pair was standing in for is the new
`IShell.CloseAsync`: teardown followed by the platform's presentation end, consulting no handler. It is
to `CloseIntent` what `RouteAsync` is to a route, so a logout, a settings rebuild or a successful login
calls it directly, while a window close, a title-bar button or the terminal quit key dispatches the
intent. The window-close translators are renamed with it — `WindowClosingToCloseIntent` and
`ClosingToCloseIntent`.

**Migrate.** `IMessageBox` is renamed `IModalPrompt`, and `RequestSessionEnding` takes a
`promptFactory`; the default implementations follow as `AvaloniaModalPrompt` and `WpfModalPrompt`. The
surface is synchronous by contract — a prompt blocks the calling thread until the user answers — and
`Alert(title, message)` is added for the acknowledgement, which has no answer to report.

**Migrate.** `ShellBounds` is renamed `Bounds` — a position and a size for any surface, which never
needed a platform's name; `ShellBounds.Empty` is `Bounds.Empty`. Neither framework declares a type called
`Bounds` (both name the rectangle `Rect`), so the name resolves on either platform side without an
alias.

**Migrate.** The host-state snapshot belongs to the window, and everyone else only observes it:
`HostPropertyBase`'s setters are `protected`, `HostStatus` is renamed `HostProperty` so a window scope
resolves one type on both platforms, and `IAvaloniaShell.FeedHostPropertyChanged` /
`IWpfShell.FeedHostPropertyChanged` are gone. The host window resolves the snapshot once at
`HostShell`, seeds it with `Prime(window)` and feeds every later change through `Feed(...)` — a window
that forwarded its properties to the shell now writes the snapshot itself, and a model that wrote
`Title`, `TopMost` or the rest directly can no longer do so. `RestoreShellStateIntent` is answered by
the host window, the only holder of `LastHostState`.

**Migrate.** The notice chrome is a panel: `INoticePanel` is its platform surface, resolved from a
`NoticePanelModel` through the application's template table, never injected, and one panel owns the
three regions, each entry's view, its placement and its animation. `INoticeEngine` loses the entry-view
collections it exposed and gains `AttachPanel`; `NoticeServiceOptions` loses the positions and the
margins to `NoticePanelModel`; `INoticeDirector`, `DefaultNoticeDirector` and `BottomSlideItemViewBase`
are gone, so a custom item view derives from `FeedbackItemViewBase`. An entry's own `ISceneTransition`
still wins, and a view that implements none gets its channel's default, derived from the region's
anchor — which fixes the toast, whose default slid up from the bottom and now drops from the top.

**New.** `ViewResolution` is public in both platform packages and `ThemeDictionarySwap` in
`Everlong.Nester.Wpf` — the view-resolution and theming seams an extension package builds on.

**Migrate.** `IFocusPolicySurface` and its `FocusPolicy` enum are gone from the core package. A layer's
content no longer declares its own Tab participation: the platform surface derives it from the router it
carries — a derived router's presentation traps Tab and excludes every layer beneath it, and any other
routing surface cycles. A consumer that implemented the interface to declare a Tab intent can no longer
do so.

**Migrate.** A custom exit hook calls `AppLifetimeBase.RunExitTeardown` instead of awaiting the teardown
cascade: the platform tears its dispatcher down the moment the exit handler returns and aborts every
continuation still queued, so the cascade is pumped until it settles and only then does the hook return.
A failing cascade is reported through the app's error channel, never thrown into the exit path.

## 0.1.10 — 2026-09-22

Nothing between 0.1.7 and this one shipped — the only tag is `v0.1.7` — so this entry carries the whole
span: the shell's intent family is split in two, one intent is gone, the flying plane gains an anchor, a
view declares its mount point one way, the family that names it is renamed, and a session ending is
arbitrated through the message hub.

**Migrate.** `IShellIntent` carried two subjects under one name — the window's chrome and the shell's own
end. Six records move to the new `IWindowIntent`: `ShowIntent`, `HideIntent`, `TopmostIntent`,
`MutateShellStateIntent`, `RestoreShellStateIntent` and `CenterOnScreenIntent`. What `IShellIntent` keeps
is the shell's end alone — `TryCloseIntent` and `CloseIntent` are unchanged.

A platform shell or an application shell that answers either set has to widen its guard: a fallback branch
that tested `is not IShellIntent` now answers nothing in the window family, and a switch that tested a
narrowed `IShellIntent` stops compiling (`CS8121`).

`CenterOnOwnerIntent` is removed. No platform implemented it, so a dispatch of it settled as `Pass`
everywhere — `Everlong.Nester.TerminalGui` "consumed" it as a no-op and no window platform had a case
for it at all.

**Behaviour.** `Everlong.Nester.TerminalGui` no longer consumes the window intents. It has no window, so
a window intent settles as `Pass` instead of `Handled`; read `IntentResult` where the answer matters.

**Behaviour.** The reveal waits for the arriving view to be laid out before the scene director runs: one
dispatcher pass, and the view's `Loaded` event when one pass was not enough. `ISceneTransition`'s "laid
out" promise now holds for a dialog's arriving view too, so a geometry-based director — a shared-element
flight — no longer needs a wait of its own, and a director that read its own `Bounds` through a
hand-rolled dispatcher loop can drop it.

**Migrate.** `TransitionContext.FlyingCanvas` is `FlyingCanvas?` instead of `PCanvas`, and it is
genuinely `null` outside page composition. A notice director was handed a null plane with a
non-nullable annotation before, so a director that reads geometry off it needs a null check.

**Behaviour.** The flying plane carries an untyped anchor: `FlyingCanvas.Anchor` is one value the
plane never reads or validates, and it is the window-scoped place for what the view layer knows at a
tap and the arriving director needs afterwards — a shared-element origin, for instance. It is cleared
with the surface, by `FlyingCanvas.Clear()` and when the plane's lease is reclaimed, and reading it does
not take it.  Child visuals go on `Children` as before.

**Feature.** The extensions mark a control as the anchor of a transition: `n:Transition.Anchor` parks
the annotation's value — the card to fly, handed over by the view — in its window's plane, and the park
lands before the button's own command runs.  `FlyingCanvas.Anchor` is a plain slot: a read hands the
value back and does not take it, so emptying the slot is the reader's own.  Both templates do exactly
that on the post list (`PostsPage` → `PostDetailPage`).

**Migrate.** A view that can sit above another chain node must now declare its mount point: it
implements `IBodyHolder` and returns the panel from `GetBodyPanel()`. The framework no longer searches
the view's logical tree for a `LayoutBody` — the one named `Body`, or the only one. That search had no
scoping rule, so a panel belonging to a layout the host composes declaratively was as much a candidate
as the view's own, and it was invisible inside a `ControlTemplate`; a view that relied on the
convention adds the contract and returns the panel from the code-behind.

**Migrate.** The family is renamed around what the thing is — the body below the node — rather than
the kind of host that carries it:

| before | after |
|---|---|
| `ILayoutControl` | `IBodyHolder` |
| `GetLayoutBody()` | `GetBodyPanel()` |
| `ILayoutBody<TView>`, `ILayoutBody` | `IBodyPanel<TView>`, `IBodyPanel` |
| `LayoutBody`, `<n:LayoutBody/>` | `BodyPanel`, `<n:BodyPanel/>` |
| `TuiLayoutBody` | `TuiBodyPanel` |
| `LayoutBodyController`, `LayoutBodyChangeSet` | `BodyPanelController`, `BodyPanelChangeSet` |

The XAML element is the one name a consumer types, so every `<n:LayoutBody/>` becomes `<n:BodyPanel/>`.
`Panel` is Nester's word here, not the platform's: Avalonia's `Panel` is a single-cell container,
WPF's is not, and Terminal.Gui has no panel at all.

**Migrate.** `IBodyPanel<TView>.IsAttachedToVisualTree` is removed. The reveal read it once, to decide
whether to wait for an arriving view to load; the wait moved onto the arriving view itself and nothing has
read the flag since. An implementation of `IBodyPanel<TView>` drops the member — the platform panels no
longer track it.

**Behaviour.** A convergence no moving-side view directs now completes with the commit that landed it
instead of a dispatcher pass later — the entry was made visible-but-transparent and laid out for a
director that never ran. `ISceneTransition` still owes a laid-out view; the wait now sits in the branch
that consumes it.

**Behaviour.** A view that hosts a chain node but declares no mount point is reported through the error
channel — the shell's `ReportError`, so the app's error handler sees it — instead of leaving a blank page
in silence. The child stays unmounted and the rest of the convergence proceeds.

**Feature.** A session ending is arbitrated through the message hub. The app calls `RequestSessionEnding`
on the hub from `OnSessionEnding` / `ShutdownRequested`; the framework broadcasts `SessionEndingMessage`,
a participant registers a prompt and the action it confirms with `Guard`, and the framework asks through
`IMessageBox` — a default per platform — and runs the confirmed actions once every guard passes. The
intent chain stays shell-internal.

**Behaviour.** A close the OS or the lifetime initiates no longer enters the user close chain: it would
run the close guard, and on Avalonia holding it aborts the shutdown outright. Avalonia reads the reason
off the close event; WPF has none, so the shell marks the session ending instead.

## 0.1.7 — 2026-09-20

The first release, seven packages at one version plus the template package:

| packages | target |
|---|---|
| `Everlong.Nester`, `.Abstractions`, `.Extensions`, `.Avalonia`, `.Extensions.Avalonia` | net8.0 |
| `Everlong.Nester.Wpf`, `.Extensions.Wpf` | net8.0-windows |
| `Everlong.Nester.Templates` | the `nester-avalonia` and `nester-wpf` templates |

Nothing to act on: nothing precedes this version, so nothing is retired, renamed or raised.
`Everlong.Nester.TerminalGui` is not published — it is the experimental surface — and `Generators`
and `CodeFixers` ship inside `Everlong.Nester` as analyzers rather than as packages of their own.
