# ComponentModel — Optional Model Bases

> Status: living spec. Code is the source of truth; this document fixes the vocabulary and the
> authoring rules of the component model domain.

## 1. Why

The framework's contracts are interfaces — routable membership, arrival, release, the parameterized
engagement, intent handling. The bases in this domain exist so a view model does not have to implement
the plumbing itself: they derive from an observable base, receive the injected services, and turn the
interface methods into overridable hooks.

They are convenience, not contract: a plain class that implements the interfaces is a first-class
participant.

## 2. The model bases

- **`RoutableModel`** — the injected shell and router, the routing state (`HasBeenRouted`,
  `HasBeenArrived`, `IsActiveLocation`), and the hooks a derived model overrides: routed-to,
  routed-from, arrived, released. It implements the routable, arrived and releasable contracts.
- **`ParameterizedModel<TArgs>`** — the parameterized contract with a strongly typed engagement,
  delivering through the adoption method into an overridable hook.
- **`AdaptiveParameterizedModel<TArgs>`** — the adaptive contract over `ParameterizedModel<TArgs>`:
  serves any revised request of the model's own argument type in place instead of being rebuilt.
- **`DialogSessionBase` / `DialogSessionBase<TResult>`** — the dismissal (a close text and a close
  command) and the result write-back channel; the typed half settles the presentation with a result.

## 3. Boundary

They live in the extensions assembly, over the MVVM toolkit. Neither the abstractions package nor the
core runtime references them, and nothing in the framework requires them.

## 4. Constraints

- **State lives on the model.** The bases flip the routing state and hand it to the hooks; the view
  never drives it.
- **A hook is a courtesy, not a lifecycle guarantee.** The arrival hook receives a "first arrival" flag
  because the framework does not remember "first time" beyond the routing state it owns.
- **The bases never touch views or platform types.** A base that reached for the platform would drag
  the whole stack into the model layer.
