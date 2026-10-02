# Notice — One Service, Three Channels, One Panel

> Status: living spec. Code is the source of truth; this document fixes the vocabulary and the
> authoring rules of the notice domain.

## 1. Why

Transient feedback comes in three shapes — a toast, a snackbar with an action, a notification
banner — but it is one concern for the caller: *say this now*. The domain gives the caller a single
service, the framework a single band, and the platform a single surface. Every visual decision —
where a region anchors, how it is spaced, how an entry moves — belongs to that surface; the service
carries none of it.

## 2. Contracts

- **The service** aggregates the three channels: toast, snackbar and notification, each with a
  fire-and-forget form and — for the two that carry an action — a form that completes with a result.
- **The entries** are one per channel. Each carries its own payload, its dismiss command and its
  settled result over a shared base, and each is pointer-aware so a hover can freeze its countdown.
  An entry is the model; the view is the panel's to build.
- **The engine** drives the three concurrent stacks: it presents an entry, starts its timer and
  dismisses it — the scene lifecycle, never the scene.
- **The panel** is the domain's platform surface (`INoticePanel`): it resolves each entry to a view,
  places it and animates it. It is resolved from the tree through a **panel model** — a registration
  in the application's template table — never injected.
- **The options split by owner**: the service's options hold durations, hover behavior and channel
  limits; the panel's model holds placement and spacing. The service hands the model through and
  reads nothing from it.

## 3. The shape

The service base is a **layer tenant, not a renderer**: it rents the notice band lazily on the first
show, hands every call to the engine, and — when the lease is evicted — drops its host and discards
later shows, because a notice with nowhere to land must not fault its caller. Shows are posted to the
main thread, so a caller may announce from any thread.

The band holds the panel. The engine knows the panel only as its surface contract; the panel knows
the entries only as models and the visual choices only as its own model. Neither side reaches across
the seam.

## 4. Platform split and entry

The panel, the engine and the item views live in the platform extension packages, and registering them
is one call. An application that skips those packages registers its own engine and tenant and maps the
entries to its own views.

An item view's own scene transition wins: the panel invokes it and steps aside. A view that implements
none gets the channel's default scene — derived from the region's anchor, not from the view.

## 5. Constraints

- **The entry carries the state.** The awaiting show completes on the entry's settled result (action,
  dismissal or timeout) — never on a view, and never on the entry's removal from the stack.
- **The engine is a port, not a policy.** Everything user-facing — durations, levels, actions — is
  decided before the engine is called.
- **The partition is by owner, not by layer.** A view is never injected; the panel reaches the tree
  through its model. A placement decision is the panel's alone — the service carries no anchor, no
  margin, no direction.
- **The domain knows nothing beyond the broker it rents from and the dispatcher it posts through.**
  A notice is a surface on the stack; it decides nothing beyond what it shows.
