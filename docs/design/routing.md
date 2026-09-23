# Routing — The Descriptor Is Not the Site

> Status: living spec. Code is the source of truth; this document fixes the vocabulary and the
> authoring rules of the Routing domain.

## 1. Why three nouns

Routing answers two questions that must not be answered by the same object: *where do you want to go?*
and *where are you now?*

- A **descriptor** (`ILocator`) is what an author writes — reusable, immutable, and allowed to be
  partial, because the layout chain may be filled in later.
- A **plan position** (`ITarget`) is one ordered step: the participant type, the arguments it consumes,
  and an optional pre-built instance.
- A **site** (`ILocation`) is a realized node: type, arguments, instance, and its place in the
  presented chain.

Conflating a request with its result would make the request mutable by the act of navigating, and a
request that navigation rewrites is a request that cannot be sent twice. The split is what lets one
descriptor be navigated twice and produce two independent presentations: the engine never introspects
a descriptor's complexity, a descriptor expands to a complete plan, and everything after that reads
plans and sites.

## 2. The vocabulary

The contracts live in `Everlong.Nester.Abstractions`, namespace `Everlong.Nester.Routing`; the engine
lives in the core runtime.

`ILocator` is the descriptor: `Path` is the ordered target chain, outermost first, the content target
last, and the value is fixed for the descriptor's lifetime. `ITarget` is one plan position — `Type`,
`Args`, and `Instance`, the last `null` when the participant is resolved at interpretation. `IArgs`
marks an argument carrier, and `Args` is the default carrier: record value equality, with an empty
value. `ILocation` is a realized node — `Type`, `Args`, `Instance`, `Parent`, and `Trail`, the ordered
path from the outermost node down to this one.

`IRouter` is the navigation entry point: `Role`, `RouteAsync`, `JumpAsync`, `Derive`, `Completion` and
`Stack`. `RouterRole` splits the two shapes a router comes in — `Base`, the application's main router,
and `Derived`, a result-bearing presentation. `IRouterStack` is the navigation-state surface: its
property members raise `PropertyChanged` on every landing and trim, while its query members compute on
demand and carry no notification promise. `IRouterCompletion` is the result channel, awaitable through
its awaiter.

`RoutingDirection` is the cause axis of a change — `Route`, `Back`, `Forward`, `Refresh`, `Close`,
`Jump`. `IRoutingTransfer` is one transfer: its `Direction`, the `Lifetime` token that bounds it, and
its two moving sides, `Arrivings` and `Departings`. `IRoutingContext` is that transfer as the
lifecycle callbacks observe it: the landing site, the site departed from, whether the transfer runs on
a derived router, and a request-scoped `IFeatureCollection` keyed by interface type.

The lifecycle contracts are the two halves of a navigation. Before the commit: `IRoutable`,
`IParameterized`, `IAdaptiveParameterized`, `IBodyChanged`. After the commit: `IArriving`, `IArrived`,
`IDeparting`, `IDeparted`, and `IReleasable` for the end of the chain's hold. `IRouteIntent` marks a
routing intent, and `RouteIntent` — carrying a descriptor — is the consulted form of a navigation.

## 3. From descriptor to site

A descriptor may be partial. The layout chain a page sits in is usually a property of the page type
rather than of the request, so a request may name only the content target and leave the layouts to be
filled in. Two things can complete a plan: a compile-time expansion that turns an attribute on a
participant type into a sealed descriptor whose construction already carries the full chain, or the
runtime route hook at interpretation. Either way the layout chain is a fact rather than a discovery —
the runtime never reflects over a type to find out where it nests.

A site is one realized node of the presented chain, and every resolved participant gets the same
weight: an intermediate layout and the content target are both sites. `Parent` is `null` only at the
outermost node, and a depth-1 presentation, with no layout at all, is normal rather than degenerate.
Because each site knows its parent, the presented chain is the parent lineage of the current site, and
ancestry needs no separate list.

## 4. Two pipes

A navigation is computed by two folded pipes over one transaction:

- the **transaction pipe** is synchronous, runs before the commit, and is rewritable and abortable — it
  decides *what* the presentation becomes;
- the **convergence pipe** is asynchronous, runs after the commit, and is detached from the caller — it
  *runs* the presentation.

The commit is the boundary between them: the transaction settles the caller's `RouteAsync` await, and
the convergence continues on its own, observed through its completion channel. A transaction that
settles without landing — a consumed route, an unwalkable traversal — commits nothing and starts no
convergence.

The split is what keeps a request reversible: everything an author can refuse or rewrite happens
before any view moves, and everything that touches a view happens after the decision is final. A
failure on either side therefore means something different. A throw before the commit faults the
caller and leaves the presentation untouched; a throw after it is quarantined and the run continues.

## 5. The transaction pipe

The pipe is folded as **one sub-pipe per direction** — a shared prefix, the direction's own decision,
and a shared suffix — and a transaction lands on its own sub-pipe at intake. A direction walks nothing
it does not need, so a traversal never meets the route's plan expansion.

**Only a route is asked for a request.** It is the one direction that materializes a chain from a
descriptor, so it is the only one with a request to rewrite or consume. The traversal, refresh, jump
and close directions replay or depart an engagement that already exists, and a hook with nothing to
rewrite or refuse would only blur the contract.

Two windows bound what may change while a transaction computes. The **request window** is open while
the descriptor is still a request — the route hook may rewrite the descriptor there, and expanding it
into a plan closes the window for good. The **sync window** is held open over the transaction's own
computation, and a trim is declined while it is open, so a transaction that would otherwise race the
stack it is reading computes on stable data.

**Anchor discipline.** A descriptor must be fully expanded into its plan *before* the tree walk
begins. Reuse identity is positional — a node matches within its parent's children by
`(type, args, instance)` — so completing a plan mid-walk would turn reuse into build-then-reconcile
and destroy the reuse semantics. A full descriptor and a partial one differ only in who completes the
plan, never in when the walk starts.

The route decision reads the plan in two steps. It first scans for absorption: a presented position
matches when the arguments it serves equal the requested value, or when an adaptive participant
answers that it will serve the revision in place. It then walks the container tree — a lookup hit
reuses the held node, so the node's state and view continue; a miss materializes a node and attaches
it. What the walk settles is the fresh parameterized nodes, each to be delivered its arguments, and
the absorbed revisions. Delivery reads each participant's engagement back into the node's identity, so
what the node serves afterwards is what the participant said it serves, not what was requested.

## 6. The convergence

A convergence is one fold: the run's supervision, the subclass observation hook, the run's scope, the
phases that prepare, run and complete the chain, and the release-drain endpoint. The observation hook
wraps the whole run, which is what lets a subclass see a convergence as one event rather than as a
sequence of phases.

The phases are ordered by what they depend on. The resolved chain's view slots are filled first, and
filling is idempotent — a node shared with the previous presentation keeps the view it already has.
The departing pairs then leave, innermost first, before the arriving side runs its arrival gate,
outermost first: a surface is on its way out before whatever replaces it is asked to arrive. The chain
is then published to the model and the visual switch runs. The departure notices and the arrival
completions follow, and a run that is cut short leaves its unreached nodes unarrived, so their next
entry simply runs the convergence again. The release drain is the endpoint every path that ends a
presentation settles at.

Two observation faces share the transfer. The view's face (`IConvergenceScene`) sees the resolved
chain as concrete nodes — the nodes it has to assemble and reveal. The participants' face
(`IRoutingContext`) sees only `ILocation`. Keeping the two apart is why the view's surface can name
concrete node types while a participant's callback cannot, and the narrower face is the one handed to
participant code.

## 7. The transfer's two sides

**`Arrivings` and `Departings` are the transfer's moving sides.** Both are chains, outermost first,
the content target last, and both start at the same position: the first one where the new chain's
served value differs from the old chain's, compared as `(type, instance, args)`. Everything above that
position is unchanged data — it is in neither list, and it is never re-entered, re-delivered or
re-left. The comparison includes the arguments, so a node re-engaged in place is itself the first
difference and lands in both lists; comparing structure alone would miss it. Where a structural walk
is involved the two readings agree, because a reused node's data is equal by construction — the
data-aware comparison is what makes an in-place revision a difference at all.

The sides are read as **position pairs**, one depth at a time. An element's depth is its position in
the chain, so a pair is any two elements at the same depth, one from each side.

| Pair | Meaning |
|---|---|
| the same node on both sides | **re-engaged** — neither departs nor mounts; it replays its arrival. |
| different nodes | **replaced** — the old member departs, the new one arrives and mounts. |
| an arriving element alone | **entering** — it arrives and mounts. |
| a departing element alone | **leaving** — it departs, its view unmounts. |

The key is the node, not the instance: an instance that arrives under a different parent is a fresh
node at another depth, so it leaves and enters rather than re-engaging. A re-parent is a move, and a
move is a departure.

Two directions carry their sides directly, because nothing about their chains is a *positional*
difference:

- **refresh** carries the whole chain on both sides — the same chain in both lists — so every
  position pairs on its own node, nothing departs and nothing mounts, and the whole chain replays its
  arrival; naming only the arriving side would read every member as entering.
- **close** carries an empty arriving side and the presented chain on the departing side, so every
  position is leaving.

A traversal between two history entries whose chains carry equal data lands with both sides empty: the
presentation did not move, the history cursor did.

From those pairs the run derives the order every phase observes: departures run innermost first and
arrivals outermost first, a re-engaged element keeps its view and its visibility while an entering or
replaced one mounts, and only a position whose requested value differs from the served value is
delivered.

**The lifetime token's span.** `IRoutingTransfer.Lifetime` belongs to the transfer: one cancellation
source per transfer, created the first time a callback reads the token and never reused. Every token
ends exactly once. A landed transfer's token ends when its convergence ends, and a convergence
superseded before it starts ends through the same exit. A transaction that leaves the pipe without
committing cancels its own — a faulted or vetoed navigation has no convergence to end it. A newer
transfer landing cancels the previous transfer's token as it takes the active slot, and a router
tearing down cancels the active transfer's token.

Work handed the token therefore stops at the latest when its own presentation is over. Work that must
outlive the presentation carries its own token and reacts to the departure callbacks; a cancellation
callback is user code, so a throw reports through the error channel and decides no navigation.

## 8. Lifecycle callbacks and their exception discipline

**Before the commit**, callbacks run while the transaction is still reversible, and a throw aborts it:
nothing commits, no convergence launches, and the exception faults the caller's await as it stands.

- `IRoutable.OnRoutedTo` / `OnRoutedFrom` — the membership edges: edge-triggered, never on a data
  refresh or a re-arrival, synchronous side effects only.
- `IParameterized.DeliverArgs(args)` — the instance adopts the arguments it should serve. It runs once
  per fresh parameterized node and once per absorbed revision, before the membership notifications, and
  it is the only callback allowed to change the engagement.
- `IParameterized.EngagedArgs` — read back after delivery; `null` means no correction, so the requested
  arguments stand.
- `IAdaptiveParameterized.IsAdaptable(requested)` — asked while absorption is still being decided, so
  it must have no side effects. A refusal lands a fresh instance.
- `IBodyChanged.OnBodyChanged(body)` — fires before the commit and before the body's participants have
  arrived; silent while the membership holds, so a re-engagement in place does not fire it.

**After the commit**, callbacks run during the convergence, and a throw — or a faulted task — is
quarantined: it rides the router's error channel, the convergence continues for the remaining
participants, and nothing rolls back.

- `IArriving.OnArrivingAsync` — before the participant arrives; the returned task is awaited before the
  arrival completes.
- `IArrived.OnArrivedAsync` — the arrival point.
- `IDeparting.OnDeparting` / `IDeparted.OnDeparted` — before the participant departs while it is still
  presented, and after it has.
- `IReleasable.Release()` — the participant is no longer held by the chain, whether by eviction, trim,
  teardown, or a transaction that materialized it without committing.

The two disciplines are the transaction's reversibility read out loud: before the commit a callback
can still change the outcome, after it the outcome is a fact and a callback can only fail its own
participant.

**A close is not a transition.** It fires no membership edge and runs no departure pair: the chain
dies as a whole, and the release drain runs `IReleasable` over every held member. Every routing
callback therefore observes a real transition — none is ever called without a context.

**A navigation issued from a callback is queued**, never re-entered: it rides the router's pump, and
the new transaction supersedes the current one's convergence. A fire-and-forget navigation cannot
deadlock for that reason. A blocking wait on the returned task is the one shape that does — it starves
the pump when the callback runs in the transaction phase. An awaited navigation lands at the next
stage boundary and cuts the current run there.

## 9. Identity and retention

A node's identity is `(type, args, instance)`, matched positionally within its parent's children. The
arguments are *what the node serves*, read back from the participant after delivery, so a request equal
to the served value reuses the node and a request that does not negotiates or materializes a fresh
one.

Argument equality therefore decides reuse. A request equal to the served value reuses the engagement
with no re-arrival. A different request lands a fresh node, unless the participant is adaptive and
answers that it will serve the revision — then the revision lands in place: no push, no membership
edges, the node's identity rewritten to what it now serves, its engagement delivered before the
notifications, and its arrival replayed.

Retention is the stack itself. The entries hold the nodes — there is no separate instance cache and no
weak reference — so an instance is pinned while any live entry holds it. A dropped entry's nodes go to
the release ledger rather than dying with the entry, and only when an instance has no holdings left
does the drain release it. That is what lets a traversal back and forward return to an engagement with
its state intact.

## 10. Derived routers

`Derive` creates a router that presents an overlay with its own stack, its own scope, its own lease and
its own result channel. The overlay borrows the presenting site at derivation, so it knows what it
covers, and its completion channel is always present where a base router's is `null`.

A back at the overlay's foot completes the overlay with the back's return value, and a request that
never commits while the overlay is still empty completes it with `null`. The close sequence runs the
close transaction, its convergence and the release drain before the lease returns, so a creator
awaiting the result observes a free layer.

An **ephemeral** overlay is one-shot: it accepts a single route request, and every later request is
re-dispatched as the consulted form of a navigation for a navigable surface to take. It answers no
route consultation at all — the skip happens before the chain is asked, which is what keeps stacked
one-shot surfaces from handing the same request to each other forever. A back is still answered,
because a back concerns the surface's own content.

The round trip out of a presentation is explicit: a jump records a fresh visit to the live entry that
presents a site, and a site converts back into a descriptor from its trail, with arguments kept and
instances dropped.

## 11. Direction semantics

| Direction | Meaning |
|---|---|
| `Route` | Present a plan: push a new entry, or absorb a revised one in place. |
| `Back` / `Forward` | Traverse retained entries — they never re-run the request gate. |
| `Refresh` | Replay the current chain in place — no membership edges, arrival only. |
| `Jump` | Record a fresh visit to an existing engagement. |
| `Close` | End the presentation: the chain dies as a whole. |

A back at the base router's foot is consumed, and a back at an overlay's foot closes the overlay. The
base router answers a refresh; a derived router forwards the command.

A navigation to the current chain is not a refresh: it pushes a new entry and republishes silently.

A jump answers `false` only when no live entry presents the site. A visit that faults before it lands
faults the caller's await, so a `false` is never a swallowed failure.

## 12. Constraints

- **A directive is addressed; a consultation is not.** `RouteAsync` consults no one — a caller that
  wants the page's veto dispatches the intent form instead. Modality is enforced at the input level,
  not the command level.
- **One router owns one lease and one view.** The router presents itself as its lease's intent handler
  and mounts its view into the lease; a derived router takes its own lease rather than sharing the one
  beneath it.
- **Narrow dependencies.** The router depends only on the layer broker, the error reporter and the
  intent dispatcher. Everything else it needs comes from its own scope, and the root view that the
  participant tree realizes into is supplied by the platform rather than injected into the core.
- **A descriptor is reusable; a site is not.** Navigating twice with one descriptor produces two
  independent presentations, and nothing a navigation does may be visible on the descriptor afterwards.
