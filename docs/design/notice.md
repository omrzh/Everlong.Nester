# Notice — One Service, Three Channels

> Status: living spec. Code is the source of truth; this document fixes the vocabulary and the
> authoring rules of the notice domain.

## 1. Why

Transient feedback comes in three shapes — a toast, a snackbar with an action, a notification
banner — but it is one concern for the caller: *say this now*. The domain gives the caller a single
service and the framework a single band, and lets the platform own how the three stacks actually
render.

## 2. Contracts

- **The service** aggregates the three channels: toast, snackbar and notification, each with a
  fire-and-forget form and — for the two that carry an action — a form that completes with a result.
- **The entries** are one per channel. Each carries its own payload, its dismiss command and its
  settled result over a shared base, and each is pointer-aware so a hover can freeze its countdown.
- **The options** hold the defaults per channel: durations with and without an action, the stacking
  limits, and the placement and transition settings.
- **The engine** is the framework seam a platform implements: the three concurrent entry stacks and
  their scene lifecycle.

## 3. The shape

The service base is a **layer tenant, not a renderer**: it rents the notice band lazily on the first
show, hands every call to the engine, and — when the lease is evicted — drops its host and discards
later shows, because a notice with nowhere to land must not fault its caller. Shows are posted to the
main thread, so a caller may announce from any thread.

## 4. Platform split and entry

The engine implementation, the transition and the item views live in the platform extension packages,
and registering them is one call. An application that skips those packages registers its own engine and
tenant and maps the entries to its own views. Channel-specific payloads and the built-in look belong to
those packages.

## 5. Constraints

- **The entry carries the state.** The awaiting show completes on the entry's settled result (action,
  dismissal or timeout) — never on a view, and never on the entry's removal from the stack.
- **The engine is a port, not a policy.** Everything user-facing — durations, levels, actions — is
  decided before the engine is called.
- **The domain knows nothing beyond the broker it rents from and the dispatcher it posts through.**
  A notice is a surface on the stack; it decides nothing beyond what it shows.
