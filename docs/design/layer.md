# Layer — Stacked Surfaces as Leases

> Status: living spec. Code is the source of truth; this document fixes the vocabulary and the
> authoring rules of the Layer domain.

## 1. Why layers

A presentation stack holds many surfaces at once — ground content, overlays, notices, tools, a canvas
that spans the whole stack. Each needs a stacking position and a life of its own, and the stack has to
stay in one pair of hands, or a surface that mounted itself onto the visual tree would escape both its
ordering and its teardown.

The domain's answer is a **lease on a stacked slot**. A tenant asks a broker for a band, the broker
grants a z and mounts the tenant's content onto the stage, and the tenant keeps the lease for the
surface's lifetime. z is composition order, not ownership: the tenant owns the content and the intent
handler, the broker owns the stack.

The contracts live in `Everlong.Nester.Abstractions` (`Layer/`); the ledger and stage seams live in
the core runtime.

## 2. Vocabulary

`ILayerBroker` grants leases — the only layer entry point user code injects. `ILayerLease` is the
granted slot, single-use. `ILayerTenant` is the lease holder, notified once per reclaimed lease, after
the lease is dead. `LayerBand` is a closed z interval; `LayerPolicy` says where inside a band a lease
lands. `ILayerLedger` / `ILayerStage` are the framework seams — liveness and removal, mounting and
unmounting a slot's surface.

A tenant holds a lease; it never holds the ledger or the stage. That is why `Content`, `IsVisible` and
`IntentHandler` are writable on the lease while the ledger stays an `[EditorBrowsable(Never)]` seam:
the tenant keeps shaping its surface, the stack's bookkeeping stays the broker's.

## 3. Bands

`KnownLayers` publishes the closed bands, and each band is a reserved slice of the z axis:

| Band | Slice |
|---|---|
| `Neutral` | `0` |
| `Backdrop` | `100`–`299` |
| `Navigation` | `1000`–`1999` |
| `Floating` | `2000`–`2999` |
| `Dialog` | `3000`–`3999` |
| `Notice` | `4000`–`4999` |
| `DevTool` | `9000`–`9999` |
| `Flying` | `int.MaxValue` |

A band is closed so one domain's slice can never collide with another's. `Neutral`, `Backdrop`,
`Floating` and `DevTool` carry no framework occupant; `Navigation`, `Dialog` and `Notice` do. Nothing
rises above `Flying`.

## 4. The grant

`ILayerBroker.Acquire(tenant, content, band, policy)` never fails and never leaves the band. The
policy is the position asked for, not a key: `Floor` is the band's floor, `Ceiling` its ceiling, and
`AboveHighest` one above the band's highest live lease — the floor when the band holds none. A grant
that cannot go higher degrades by sharing the ceiling, where the later acquisition sits above. Same-z
co-tenants share the position; occupied z values are never rejected.

A layer is composition, not a reservation, so a grant has no failure mode — there is no "band full"
outcome to handle. `Acquire(tenant, content, z)` grants an exact z for callers that need a pinned
position.

Rules to read by: ask for the position you want (`band` + `policy`), report the position you got
(`lease.Z`), and never assume z is unique.

## 5. The lease

A granted `ILayerLease` is single-use: `Z` and liveness are fixed at the grant, while `Content`,
`IsVisible` and `IntentHandler` stay writable for the slot's life, and `Release()` ends it. `IsActive`
reads liveness until the end of time for that lease — a released or evicted lease never revives.

`IntentHandler` defaults to a pass-through handler, so a slot with no opinion declines instead of
blocking. `Content` is the surface the tenant presents; `IsVisible` hides the slot without ending the
lease. `Release()` is idempotent, and it carries no eviction notice.

## 6. From grant to stage

The broker grants in one order: create the lease, assign the content, mount the slot, record the
entry. A failed mount therefore leaks no entry, and the ledger only ever knows live slots.

The stage is optional at grant time. Acquiring before a stage is connected is a pure ledger entry.
Rebinding a stage unmounts every live lease from the old stage and mounts it onto the new one;
connecting the same stage twice is a no-op.

## 7. Eviction

Teardown evicts topmost first: the slot is removed from the ledger, unmounted, and only then is the
tenant notified through `OnEvictedAsync` — the notice arrives after the lease is dead, which is what
makes it safe for the tenant to drop its content without racing the stack. A tenant callback that
throws is reported and the cascade continues, because a broken tenant must not strand the leases
beneath it.

Two shapes of reaction cover the practical cases: a holder that owns a whole sub-stack tears it down,
and a holder with a single transient surface drops it, so a late use is discarded rather than faulted.

## 8. One order for everything

**z descending, latest grant first within a z** is the domain's single ordering. It is the order
surfaces stack in, the order leases are evicted in, and the order an intent dispatch consults lease
handlers in — so the topmost surface has the first say and dies first.

## 9. Layer and Intent

A lease carries an `IntentHandler`, which is how a leased surface joins an intent dispatch: the
handler slot is the layer domain's whole contribution to the intent protocol, and the broker itself
stays out of it. The consultation order is the stacking order of §8 — the topmost lease has the first
say — and a handler that does not recognize an intent forwards it to the next one, exactly as the
intent protocol prescribes.

Because the slot stays writable for the lease's life, a tenant swaps its handler as the surface's role
changes without touching the lease's position or liveness. A slot that never assigns one keeps the
pass-through default and declines rather than blocking.

## 10. Constraints

- **Inject `ILayerBroker`.** Never keep the ledger or the stage — a tenant's handle is its lease,
  never the stack's bookkeeping.
- **Take z from a band.** A bare number cannot be reasoned about across domains; `KnownLayers` is the
  shared vocabulary, and a band's slice is what keeps domains out of each other's way.
- **One lease per surface, and release it.** A lease is single-use and its content slot is the only
  channel; releasing ends the surface, leaking the lease keeps a dead slot on the stack.
- **Never assume z is unique.** Same-z co-tenants are normal, and the later acquisition sits above.
- **Keep presentation state on the lease** (`Content`, `IsVisible`) instead of in a service field, so
  an eviction is the whole cleanup.
