# Transition — Moving Within a Layer and Across Layers

> Status: living spec. Code is the source of truth; this document fixes the vocabulary and the
> authoring rules of the transition domain.

## 1. Why

A change that moves the presented surface has one choreographer: the director the framework invokes
on the moving side. Most changes move inside one layer, where the routing context already hands the
director both sides of the change. A change that crosses layers has a second party: the layer that
stays behind. The director has to know which of the two it is running, and, when the change crosses,
where the interaction came from.

## 2. Two scopes

A transition has a **scope**:

- **In-layer** — source and destination share a layer, and the transition's own chains carry both
  sides. The routing context is the whole story; no other layer takes part.
- **Cross-layer** — source and destination sit in different layers. The routing context carries the
  moving layer alone, and the other layer — the **counterpart** — is invisible to it.

The scope belongs to the change, not to the director: the same view may direct an in-layer change and
later a cross-layer one. A director that wants a different strategy per scope reads the scope off the
change.

## 3. The counterpart

The counterpart is fixed when the moving layer is created, not resolved by scanning the stack. A layer
that derives another is that other's counterpart: the base surface derives an overlay and stays
beneath it, and an overlay derives the next one and stays beneath that. A derived layer therefore
knows the layer it was derived from for its whole life, and every transition it runs — entering and
leaving alike — reads the same counterpart: the layer it crosses to and from.

Resolution by stack position would have to answer which layer below is *the* counterpart — the
immediate one, the nearest one carrying an origin, or one named explicitly — afresh for every change.
Derivation answers it once, before the change exists, and the answer cannot drift while the stack
changes underneath.

## 4. The origin

The counterpart's contribution is an **origin**: what the interaction last rested on before the
foreground moved. It belongs to the surface, not to the view that triggered the change. The surface
captures it while it still holds the foreground and drops it once the foreground returns, so it is
valid exactly for the window a cross-layer transition runs in.

Parking the origin in a window-scoped slot is the trap this avoids: any layer could write the value
another layer reads, with no order and no identity, and a stale value would outlive the change that
put it there. The surface's own memory is bounded by the foreground — it cannot outlive the change,
and it cannot collide across windows or layers.

## 5. What the code cannot express

- **Why the origin lives on the surface, not on the lease.** A lease is identity and observed state;
  the origin is a behavioural capability only the surfaces that compete for the foreground can offer.
  A surface that never takes the foreground has nothing to hand over.
- **The temporal invariant.** The origin is captured when the surface loses the foreground and
  dropped when it regains it: it is valid only while the surface is in the background. No type states
  that, and a reader that holds it past the transition holds a spent value.
- **Why the counterpart is derivation-fixed.** The stack has no stable "below": overlays come and go
  and a z can host several leases. Derivation is the one moment both the layer a router belongs to and
  the layer it covers are known together.

## 6. Constraints

- **The routing context is the in-layer mechanism.** A transition inside a layer reads its own chains;
  it never reaches for a window-global slot.
- **A cross-layer origin is the counterpart's, never the moving side's.** Entering and leaving read
  the same counterpart; the moving layer's own memory is not the origin.
- **The origin is consumed, not owned.** A director reads it for its change and does not retain it;
  the surface clears it when the foreground returns.
