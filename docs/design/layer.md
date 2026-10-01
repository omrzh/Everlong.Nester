# Layer — Stacked Surfaces as Leases

> Status: living spec. Code is the source of truth; this document fixes the vocabulary and the
> authoring rules of the Layer domain.

## 1. Why layers

A presentation stack holds many surfaces at once — ground content, overlays, notices, tools, a canvas
that spans the whole stack. Each needs a stacking position and a life of its own, and the stack has to
stay in one pair of hands, or a surface that mounted itself onto the visual tree would escape both its
ordering and its teardown.

The domain's answer is a **lease on a stacked slot**. A tenant asks a broker for a plane, the broker
grants a z and mounts the tenant's content onto the stage, and the tenant keeps the lease for the
surface's lifetime. z is composition order, not ownership: the tenant owns the content and the intent
handler, the broker owns the stack.

The contracts live in `Everlong.Nester.Abstractions` (`Layer/`); the ledger, the stage and the plane
table live in the core runtime.

## 2. Two independent axes

Layer behaviour is two orthogonal questions, and conflating them is the mistake the model exists to
avoid:

- **Placement** — which slice of the z axis the surface renders in. A `LayerPlane` answers it.
- **Focus** — which single surface currently owns foreground interaction. The broker answers it, and
  only a tenant that implements `IFocusableLayer` competes.

A notice floats above a dialog by placement while the dialog keeps focus; a panel can sit above the
navigation surface without ever taking the foreground. Position never implies focus, and focus never
implies position.

## 3. Vocabulary

`ILayerBroker` grants leases and owns layer focus — the only layer entry point user code injects.
`ILayerLease` is the granted slot, single-use. `ILayerTenant` is the lease holder, notified once per
reclaimed lease, after the lease is dead. `IFocusableLayer` is the optional capability a tenant
implements to compete for layer focus. `LayerFocusContext` says why a transfer is running.
`ILayerLedger` / `ILayerStage` are the framework seams — liveness and removal, mounting and
unmounting a slot's surface.

A tenant holds a lease; it never holds the ledger or the stage. That is why `Content`, `IsVisible` and
`IntentHandler` are writable on the lease while the ledger stays an `[EditorBrowsable(Never)]` seam:
the tenant keeps shaping its surface, the stack's bookkeeping stays the broker's.

## 4. Planes

A plane is a closed z interval with a fixed stacking behaviour, named for its intent rather than for
its occupants:

| Plane | Intent | Stacking |
|---|---|---|
| `Ground` | Below the base surface — background services | Floor |
| `Base` | The base surface — the persistent navigation stack | Floor |
| `Dock` | Panels that coexist with the base surface | Above highest |
| `Overlay` | The ephemeral interaction chain — dialogs, palettes, popups | Above highest |
| `Notice` | Transient feedback above the interaction chain | Floor |
| `Debug` | Developer tools above feedback | Above highest |
| `Ghost` | The frontmost plane — transition ghosts | Floor |

A stacking plane grants one above its highest live lease, clamped to its ceiling; a non-stacking plane
grants every lease its floor, where the later acquisition sits above. `Overlay` is the one plane whose
occupancy churns: dialogs and palettes come and go, and their order is the grant order. Nothing rises
above `Ghost`.

Callers name a plane, never a z and never a position policy — a surface that wants a particular height
is saying it belongs in a particular plane.

## 5. The grant

`ILayerBroker.Acquire(tenant, content, plane)` never fails and never leaves the plane. A grant has no
failure mode because a layer is composition, not a reservation: there is no "plane full" outcome to
handle, only a ceiling to share. The z is resolved once and fixed for the lease's life.

## 6. The lease

A granted `ILayerLease` is single-use: `Z`, `Plane` and liveness are fixed at the grant, while
`Content`, `IsVisible` and `IntentHandler` stay writable for the slot's life, and `Release()` ends it.
`IsLive` reads liveness until the end of time for that lease — a released or evicted lease never
revives.

`IntentHandler` defaults to a pass-through handler, so a slot with no opinion declines instead of
blocking. `Content` is the surface the tenant presents; `IsVisible` hides the slot without ending the
lease. `Release()` is idempotent, and it carries no eviction notice.

## 7. Layer focus

Layer focus is the single grant of foreground interaction. At most one lease holds it, and it is the
highest live lease whose tenant is `IFocusableLayer` and accepts the consultation: the broker walks
the stack z-descending, asks each eligible tenant `TryFocus`, and the first acceptance wins. A tenant
that declines defers to the next; a stack whose eligible tenants all decline has no focused lease. A
tenant that does not implement `IFocusableLayer` never competes.

The election re-runs whenever a lease is granted or leaves the stack, and `RequestFocus` lets a live
layer claim the grant directly — the shape a click into a coexisting panel takes. A transfer is
sequential and observable: the outgoing layer gets `OnUnfocusing` while it still holds focus, the
incoming layer gets `OnFocusing`, the broker commits the new holder, then the outgoing gets
`OnUnfocused` and the incoming `OnFocused`. The pre-hooks are what make an order-sensitive effect —
snapshotting the element focus a surface is about to lose — safe; the post-hooks are the settled
state.

Layer focus is not the visual top, not the element keyboard focus, and not liveness. It is the
foreground grant, and a layer that wants an intent, a keyboard focus or a dismissal to reach it must
hold it.

## 8. From grant to stage

The broker grants in one order: create the lease, assign the content, mount the slot, record the
entry, then re-run the focus election. A failed mount therefore leaks no entry, and the ledger only
ever knows live slots.

The stage is optional at grant time. Acquiring before a stage is connected is a pure ledger entry.
Rebinding a stage unmounts every live lease from the old stage and mounts it onto the new one;
connecting the same stage twice is a no-op.

## 9. Eviction

Teardown evicts topmost first: the slot is removed from the ledger, unmounted, and only then is the
tenant notified through `OnEvictedAsync` — the notice arrives after the lease is dead, which is what
makes it safe for the tenant to drop its content without racing the stack. A tenant callback that
throws is reported and the cascade continues, because a broken tenant must not strand the leases
beneath it.

Two shapes of reaction cover the practical cases: a holder that owns a whole sub-stack tears it down,
and a holder with a single transient surface drops it, so a late use is discarded rather than faulted.

## 10. One order for everything

**z descending, latest grant first within a z** is the domain's single ordering. It is the order
surfaces stack in, the order leases are evicted in, the order an intent dispatch consults lease
handlers in, and the order the focus election walks.

## 11. Layer and Intent

A lease carries an `IntentHandler`, which is how a leased surface joins an intent dispatch: the
handler slot is the layer domain's whole contribution to the intent protocol, and the broker itself
stays out of it. The consultation order is the layer stack's order — the topmost lease has the first
say — and a handler that does not recognize an intent forwards it to the next one, exactly as the
intent protocol prescribes.

Because the slot stays writable for the lease's life, a tenant swaps its handler as the surface's role
changes without touching the lease's position or liveness. A slot that never assigns one keeps the
pass-through default and declines rather than blocking.

## 12. Constraints

- **Inject `ILayerBroker`.** Never keep the ledger or the stage — a tenant's handle is its lease,
  never the stack's bookkeeping.
- **Take a plane.** A bare number cannot be reasoned about across domains, and a position policy is a
  plane's business; `LayerPlane` is the shared vocabulary.
- **One lease per surface, and release it.** A lease is single-use and its content slot is the only
  channel; releasing ends the surface, leaking the lease keeps a dead slot on the stack.
- **Never assume z is unique.** Same-z co-tenants are normal — a non-stacking plane hosts every lease
  on its floor — and the later acquisition sits above.
- **Keep presentation state on the lease** (`Content`, `IsVisible`) instead of in a service field, so
  an eviction is the whole cleanup.
- **Compete for focus deliberately.** Implementing `IFocusableLayer` is a decision to take the
  foreground; a layer that only wants to render does not.
