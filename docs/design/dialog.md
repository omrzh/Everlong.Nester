# Dialog — Presentation Sugar

> Status: living spec. Code is the source of truth; this document fixes the vocabulary and the
> authoring rules of the dialog domain.

## 1. What this domain is

The dialog domain is **presentation sugar**. It owns no engine, no window and no widget layer: a dialog
is an ordinary derived-router presentation, and what makes it a dialog is only that it carries a dimmer
slot below it, a result channel, and a dismissal rule.

## 2. The sugar it is made of

- The overlay stack and its result channel — the present-on-derived call and the ephemeral derivation
  that rents the dialog band.
- The dialog band lease that carries the dimmer and the session.
- Dismissal as a back-intent consultation.

## 3. The pieces of sugar

- **`ShowAsync(model[, dimmer])`** — the whole sugar in one call: it presents the chain `[dimmer,
  session]` on a derived router and returns the settled result. A dismissal yields `null`; the typed
  overload casts it. An omitted dimmer is the default one — light-dismiss; a caller that wants a
  specific dimmer hands one in.
- **`DialogSessionBase[<TResult>]`** — the write-back side of the result channel and the dismissal:
  the completion is captured from the routing context's features when the session joins the chain (at
  the commit, so a close racing the arrival still settles), `Close(result)` settles with a result, and
  `Close()` dismisses without one. `CloseText` labels the affordance that does the dismissing. A
  session carries a title or a body only where it declares one; the base forces neither, and the dimmer
  state is not among its members — it lives on the dialog's options.
- **`DefaultDimmerModel`** — the dimmer's own contract: `LightDismiss` (a backdrop tap dismisses) and
  `ShakeVetoedDismissal` (a refused attempt shakes). The dimmer is the arbiter: a tap becomes a back
  intent, and a non-light-dismiss dimmer vetoes external backs. The dialog's options state the same
  concern once, in their own spelling — `IsMandatory` inverts into `LightDismiss` in exactly one
  place — so an options type declares only what it adds.
- **`AddNesterDialog()`** — registers the dimmer model. The default session and dimmer views are the
  platform's; an application that does not take them maps the sessions and the dimmer to its own views
  through its locator.

## 4. Constraints

- **A session is a model like any other** — routable, arrival-hooked, releasable — and its state
  lives on the model, never on the view.
- **Dismissal is decided on the intent chain.** A control raises a back intent and the chain arbitrates;
  a session does not close itself behind the chain's back.
- **A dialog that draws its own dismissal draws it as Close**, not as a second cancel: cancel belongs to
  a dialog whose peers are *results*, while the dismissal settles nothing.
- **The show call never guarantees a result.** A dismissal is a normal outcome and the caller reads
  `null`.
- **The default views are replacements, not a mechanism.** Each is an ordinary view an application
  substitutes through its locator, and they share a theme contract, so an application restyles the
  shipped dialogs by redefining keys rather than by copying views.
