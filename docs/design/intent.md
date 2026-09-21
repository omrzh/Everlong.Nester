# Intent — Impulse as the Only Causal Input

> Status: living spec. Code is the source of truth; this document fixes the vocabulary and the
> authoring rules of the Intent domain.

## 1. Why intents

Nester's axiom is `state' = f(impulse)`: the level describes what *is*, and only an impulse moves it.
`IImpulse` marks the causal input of a state transfer:

> The level never moves the level — only an impulse does.

`IIntent` is its only implementation. Passing input through an intent buys **provenance**: when a
control's own state drives the transfer directly, that state can no longer tell whether it is the
driver or the driven — it would have to be display state and input at once. A translated intent makes
the cause explicit, consultable and refusable.

## 2. The vocabulary

The contracts live in `Everlong.Nester.Abstractions`, namespace `Everlong.Nester.Intent`.

`IIntent : IImpulse` is a polymorphic marker with **no members**: what an intent *is* is its type,
what it *carries* is its record payload. `IntentResult` is the outcome of a consultation:

| Value | Meaning |
|---|---|
| `Pass` | no handler engaged — the consultation came back unanswered |
| `Handled` | a handler performed the intent's business |
| `Vetoed` | a handler blocked the intent |

`IntentContext` is the shared state of one dispatch: the intent, its sender, the settled outcome and
the handler that settled it, plus a scratch channel. `IIntentHandler` consults that state and may
settle it; `IIntentDispatcher` starts the dispatch; `IntentDelegate` is the next handler in the chain.

`Sender` is attribution, not control flow: it lets a handler recognize a dispatch it originated, and
lets diagnostics name the source.

## 3. Settlement

Settlement is single-shot. `Handle(by)` and `Veto(by)` set the outcome once and record who set it; a
second settlement throws `InvalidOperationException`. The failed second settlement leaves the original
outcome intact, so the first verdict is the only one a caller can observe.

`Items` is the only cross-handler channel. It is created lazily, it is scratch space rather than the
answer, and it stays writable after settlement: the outcome is data, the channel is a hand-off.

## 4. The consultation protocol

Handlers fold into one chain. Each handler receives the next one as an `IntentDelegate` and decides
whether to invoke it, which splits its body into two phases:

- **tunnel** — work before the next call: pre-processing, refusal, veto;
- **bubble** — work after the next call: observing the settled outcome through `IntentContext.Result`
  and `IntentContext.HandledBy`.

At the end of the chain the delegate is a no-op, so the last handler needs no null check. Settling does
**not** prevent a later next call: the settled outcome is state on the context, and downstream handlers
receive it and must not settle again.

## 5. Dispatching

`DispatchIntent` owns three things and nothing else: sender attribution, the handler order, and the
returned `IntentResult`. A dispatcher may decline outright without consulting anyone, and `Pass` is
exactly the signal for "no handler engaged" — the consultation came back unanswered, with `Result`
untouched.

The order is the dispatcher's business, not the context's; nothing in the protocol fixes a shape on it.

## 6. Defining an intent

An intent is a record whose payload is the request data. A family is a grouping interface over such
records, **not a base class**: members stay independent records, and the family exists so a handler can
answer a whole set of them with one type test. A family is named after its **subject** — what its
members are about — and never after the handler that answers the set: one handler may answer several
families without merging them, and a member whose subject differs from its family's belongs in another
one. A responder-named family states a fact about the chain instead of about the requests it carries,
and the two part company as soon as a second handler answers the same subject.

```csharp
internal interface IEditIntent : IIntent;                  // the family
internal sealed record RenameIntent(string Name) : IEditIntent;
internal sealed record MoveIntent(int Offset) : IEditIntent;
```

There is no result payload: request data belongs on the record, a hand-off in `Items`, and the answer
is one of the three `IntentResult` states.

The names in this section and the next are illustrative — this document fixes the shape of an intent,
not a vocabulary.

## 7. Writing a handler

Decide, then either settle or forward.

```csharp
internal sealed class RenameGuard : IIntentHandler
{
  public ValueTask HandleAsync(IntentContext context, IntentDelegate next)
  {
    if (context.Intent is not RenameIntent rename)
      return next(context);               // not mine — forward

    if (string.IsNullOrWhiteSpace(rename.Name))
    {
      context.Veto(this);                 // mine, refused — settle, do not forward
      return ValueTask.CompletedTask;
    }

    return next(context);                 // mine, permitted — forward
  }
}
```

- **Settle what you answer; forward what you do not.** Settling is the answer; calling next is not an
  acknowledgement.
- **Tunnel for refusal, bubble for reaction.** A guard vetoes before next; feedback runs after next and
  reads what settled.
- **Forward at most once.** The chain has no re-entry guard.
- **`Items` carries a hand-off**, not a mailbox: write only what a later handler needs, keyed by an
  object the participants agree on.
- **Do not dispatch from inside a handler.** A nested dispatch nests the protocol, and the ordering
  guarantees of the outer one do not survive it.

## 8. Exception discipline

The protocol catches nothing. A handler that throws aborts its chain — the handlers behind it are not
consulted — and the exception is never translated into a result.

- **A throw is neither a `Pass` nor a `Veto`.** An intent that was neither handled nor refused must not
  look settled to the caller; a `catch` that settles hides a handler's bug as a policy decision.
- **`Veto` is a decision, not an error path.** A refusal is a first-class outcome, never an exception's
  consolation prize.
- **Cleanup belongs in a `finally`**, not in a log-and-swallow.
- **Nothing inside the protocol decides whether an exception is swallowed.** If one is, the dispatcher's
  own stage decided it, and that decision is not visible on `IntentContext`.

## 9. Constraints

- **The outcome is state, not a return value.** `HandleAsync` returns `ValueTask`; the answer is shared
  on the context so any stage can observe it, and the three outcomes stay distinguishable — a `bool`
  cannot express both `Handled` and `Vetoed`.
- **An intent is not a command bus.** No result payload, no queue, no retry: one dispatch consults the
  chain once.
- **An exception is never a decision** (§8).
- **The input surface is the intent vocabulary.** A component that keeps the state a transfer governs in
  a side channel competes with the protocol for authority over that state.
