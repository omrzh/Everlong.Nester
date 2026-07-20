# Absorption — Arguments, Engagement and In-place Revision

> Status: living spec. Code is the source of truth; this document fixes the vocabulary and the
> authoring rules of the absorption domain.

A route request carries arguments per position. A node's identity is `(type, served arguments,
instance)`, matched positionally inside its parent's children — the request is part of the path, not a
query string hanging off the leaf. Absorption is the participant's side of that: a live node may serve a
**revised** request in place instead of being rebuilt, and a participant may answer an unspecified
request with a default of its own.

This document is the contract for how a request becomes the arguments a node serves, and how the
participant's own engagement is settled against the framework's identity.

## 1. The contract

Two verbs, two phases. The match is pure and pre-commit; the delivery — the application — also runs
pre-commit, before the membership notifications, and the node's engagement is read back into its
identity.

```csharp
public interface IParameterized
{
  /// <summary>
  ///   The arguments this instance serves now, read once after
  ///   <see cref="DeliverArgs" />.  <see langword="null" /> means "no correction":
  ///   the requested arguments stand.
  /// </summary>
  IArgs? EngagedArgs { get; }

  /// <summary>
  ///   Adopts this engagement.  Called once per delivery, before the
  ///   membership notifications, for each fresh parameterized node and each
  ///   absorbed revision.
  /// </summary>
  void DeliverArgs(IArgs? args);
}

public interface IAdaptiveParameterized : IParameterized
{
  /// <summary>
  ///   Whether this live instance will serve the request without rebuilding.
  /// </summary>
  /// <remarks>No side effects — may be consulted speculatively during matching.</remarks>
  bool IsAdaptable(IArgs? requested);
}
```

The layering is exact: **`IParameterized` is the engagement plus its adoption; `IAdaptiveParameterized`
adds only the bypass consultation on a match miss.** A node that answers an unspecified request with a
default is not thereby saying it accepts in-place re-engagement, and a node that accepts re-engagement
is not thereby required to normalize — the two are orthogonal, so they live on different interfaces.

- **`EngagedArgs` is on the parameterized interface, not on the adaptive one.** Normalization is
  orthogonal to adaptation: a freshly materialized non-adaptive node must be able to answer an
  unspecified request with its own default, and a node that accepts in-place re-engagement need not
  normalize.
- **`IsAdaptable` receives the request only.** The framework holds what the node *serves* as its
  identity; the requested arguments are the negotiation input, and the node compares against its own
  engagement.
- **`EngagedArgs` may only change inside `DeliverArgs`.** This is the axiom `state' = f(impulse)`
  applied to arguments: the participant owns its live engagement, the framework owns the identity, and
  the engagement is only invalidated by a delivery. Changing it outside a delivery would leave the node
  serving something no request produced.
- **`null` means "no correction — the requested arguments stand".** The framework reads the property
  once, after delivery, to back-fill the node's identity; a raw implementor that never normalizes may
  leave it `null`.
- **`EngagedArgs` is not a default interface member.** The property's read and write are one pair; a
  default implementation would force an implementor to write through the interface instead of its own
  field.
- **Layout arguments are a supported shape, not a recommended one.** A shared parameterized ancestor
  always conflicts with a revision, so a revision rebuilds the subtree structurally. Query state belongs
  on the leaf.

## 2. The engagement is the identity

A parameterized node may serve something other than what was requested — a default for an
unspecified or invalid request, a clamped value, a normalized enum. The location's arguments hold
**what the node serves**, read back from the engagement after delivery (`null` falls back to the
request). A request that
equals the served value reuses the node; a request that does not either negotiates (adaptive) or
materializes a fresh node.

- The framework writes the engagement through delivery, then reads it once to record the node's
  identity — the node is the truth for what it serves, and the request is only the negotiation input.
- A node that does not normalize answers with the delivered request, so its identity is the request.

## 3. The pipeline — match, deliver, commit

- **match** (pre-commit, pure) — A presented position matches when the type equals and the arguments
  equal, **or** the adaptive consultation says yes. A position that neither matches nor adapts fails
  the whole scan and forces the structural walk, whose lookup still reuses by exact equality only.
  Nothing is written.
- **settle** (pre-commit, pure) — Records the delivery set: the created nodes with their requested
  arguments, or the revised nodes with theirs. A revision still refuses a node pinned by another
  entry. No identity write, no delivery.
- **deliver** (pre-commit, after the match, before the notifications) — For every entry of the set:
  adopt the request, then write the engagement back (`engaged ?? requested`). A throwing delivery
  aborts the transaction — nothing commits.
- **commit** — Supersede, the direction's own apply (a structural route attaches its fresh nodes and
  pushes the chain; an absorption is empty), and the landing.
- **ceremony** (post-commit) — The convergence, with the arriving side as the arrival-replay set.

Placement and discipline are load-bearing:

- **Before the commit.** The delivery and the notifications run while the transaction is still
  reversible: a throwing delivery aborts the transaction and nothing commits. A node already delivered
  before a sibling threw keeps its side effect — there is no rollback past user code.
- **Synchronous.** The transaction pipe is a non-supersedable critical section: a request arriving
  mid-pipe is only enqueued. An `await` would yield the dispatcher and reopen the interleaving window.
- **Identity after delivery.** The identity rewrite happens right after a successful delivery, before
  the membership notifications — a notifier observes the node's true engagement, never the request.
- **A revision is presented.** The moving suffix is the arriving side, so what the convergence
  assembles comes from it; the framework mounts and switches visibility only for entering and replaced
  elements, and a re-engaged element keeps its view.

### Why the delivery set, not the arriving side

The arriving side is the **arrival-replay** set: the moving suffix, whole for a refresh. A delivery
driven by it would re-deliver every re-engaged position, whose arguments may be untouched — a silent
product change. The delivery set is the fresh parameterized nodes plus the absorbed revisions; the
arrival replay and the delivery differ by exactly the re-engaged positions below the first change.

| | delivery set | arriving side |
|---|---|---|
| Filled by | the chain resolution and the absorption | the transfer's first positional difference |
| Carries | a node and its requested arguments | sites |
| Drives | the delivery | the arrival replay |

## 4. Invariants

1. **One adoption channel.** The delivery method is the only thing that may change a participant's
   engagement, and the only writer of the engagement property.
2. **The engagement is the identity; the request is the negotiation input.** The location's arguments
   are what the node serves — read back after delivery — so the tree keys on the served value, and an
   equal request reuses the node.
3. **The decision is pure; the delivery is not.** The match phase mutates no live participant. The
   delivery runs pre-commit, on live participants — a throw aborts the transaction, and a node already
   delivered before the throw keeps its side effect (no rollback).
4. **The framework is the sole writer of a location's arguments.** The value is read back from the
   engagement after delivery, written at one named point — the delivery stage, for a creation and a
   revision alike.
5. **No transaction state persists on a location.** "Delivered in this transaction" is
   transaction-scoped; it lives on the transaction's context, never on a node that outlives it.
6. **The arriving side is downward-closed.** A change at depth *k* re-engages *k* and everything below
   it and never the prefix, and a position whose arguments did not change replays its arrival without
   being delivered — the arrival set and the delivery set differ by exactly those positions.
7. **Identity uniqueness is not an invariant.** The lookup matches a same-arguments sibling by list
   order, and duplicates are constructible through explicit instance targets; the presented chain is
   authoritative, and the lookup is a reuse hint.

## 5. Caller and observer visibility

- After a route request settles, the current location's arguments are what the node serves, and the
  participant's own engagement reads the same value. The delivery runs before the landing completes.
- A transaction-phase observer sees the served value — the delivery runs before the notifications.
- A convergence observer sees the effective value, and the arriving side names the moving suffix — the
  revised position and everything below it. Exposing the two sides on the transfer makes a re-engagement
  discoverable outside the participant that produced it.
- Rebuilding a descriptor from a site rebuilds from the served value, so the rebuilt descriptor
  reproduces the request that would fast-hit this node.
- A layout chrome mirrors its body through the **body's observable state**, not through the
  body-changed callback — that callback reports the body's membership, not the state it holds, and it
  stays silent on a re-engagement in place.
