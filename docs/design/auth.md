# Auth — Policy Engine and Gates

> Status: living spec. Code is the source of truth; this document fixes the vocabulary and the
> authoring rules of the auth domain.

## 1. Why

*Who the user is* is the application's business; *whether this action may run* is a declarative,
inspectable question. The domain answers only the second: a policy engine plus the gates that read it,
so a requirement lives next to the code it protects instead of inside a page's condition.

## 2. The engine

- **One runtime fact.** A single service owns the current principal and user, login and logout, and the
  authorization question; it raises a change notification for the authentication state and another for
  the authorization state.
- **The application supplies the user.** The engine models the authenticated user as a contract the
  application implements; it never dictates an identity shape.
- **A decision is a value, not an exception.** The engine answers with a result carrying its state and
  how it was reached; "no" is a normal answer.
- **Policies are composed, not hard-coded.** Named policies and their requirements are built at
  registration and consulted by name; the engine holds no policy of its own.

## 3. The declarative surface

- An authorization attribute marks a resource with the roles it allows, a policy name, or both, and a
  registry resolves a type to the descriptor the attribute produced.
- A registry class is marked, and the generator emits its implementation; other registries are appended
  and consulted after the generated one misses.
- A requirement with its handlers evaluates one question; the options register the handlers.
- A whole navigation request is evaluated in chain order, outermost first — the first failing node
  denies the request, and a node that declares no requirement never denies.
- The attribute's string form parses into a descriptor, so a requirement can be declared where it
  cannot be spelled as a type.

## 4. Gates and entry

`AddNesterAuth(options?)` registers the engine. The view-level gates are the platform's: a gate is an
ordinary binding against the service, so a control exposes or enables itself from the answer. An
application that references only the platform core gets no auth types.

## 5. Constraints

- **The engine has zero UI knowledge.** Gates are views binding to the service, not the other way
  around.
- **A denial is a decision, not an exception.** Authorization answers a question; it does not throw on
  "no".
- **A route verdict is not cached.** Gating re-evaluates per request against the registry, so no verdict
  outlives the authentication state that produced it.
