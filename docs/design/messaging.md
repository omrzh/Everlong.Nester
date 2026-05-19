# Messaging — Facts Broadcast in Registration Order

> Status: living spec. Code is the source of truth; this document fixes the vocabulary and the
> authoring rules of the Messaging domain.

## 1. Why messages

Two parties often have to react to the same fact — a panel toggled, a document finished loading —
without either of them owning the other. A hub keeps that relation out of the object graph: the
publisher names a fact, every party that cares self-selects, and nobody holds a reference to anybody.
The publisher never learns who listened, and a listener never has to ask who published.

A message is therefore a **fact that has already happened**: its name is past tense, and it owes the
publisher no answer, no refusal and no ordering promise.

## 2. The vocabulary

The contracts live in `Everlong.Nester.Abstractions`, namespace `Everlong.Nester.Messaging`; a
thread-safe hub lives in the core runtime.

`IMessage` is a polymorphic marker with no members — what a message *is* is its type, what it *carries*
is its record payload. `IMessageRecipient` answers two questions: `CanReceive(message)`, whether this
fact is mine, and `Receive(message)`, what I do about it. The typed arities `IMessageRecipient<T1…T6>`
are default implementations over that pair, so one type can accept several shapes without writing the
untyped pair by hand; a type that implements more than one arity has to implement `IMessageRecipient`
itself, because the inherited defaults have no most-specific one. `IMessageHub` owns registration, the
broadcast, and whether anyone is currently registered. `MessageReceiveException` carries the recipient
failures of one broadcast.

## 3. One broadcast, in registration order

`Publish` runs synchronously, on the publisher's thread, and consults the recipients in registration
order. A recipient that publishes from inside its own `Receive` nests the broadcast — the nested one
settles completely before the outer one resumes, which is what lets a recipient observe a broadcast it
is part of.

Registration is by reference: the hub compares instances, never names or values, so two recipients that
compare equal are still two recipients. Registering the same instance twice is refused rather than
counted twice, and the returned token unregisters exactly the instance that registered. Disposing that
token twice is a no-op.

## 4. Failure is isolated per recipient

`CanReceive` is a **contract, not a try/catch site**: a throw from it ends the whole broadcast, because
the hub has no answer for "could not decide" and must not invent one.

A throw from `Receive` is a different matter. That recipient is skipped, the broadcast continues to the
rest, and the collected failures surface as one `MessageReceiveException` when it is done. One broken
recipient therefore cannot deprive the others of the fact, and a publisher that cares still gets to see
every failure at once.

## 5. Constraints

- **A message is a fact, never a request.** It reports what already happened, to whoever cares, and owes
  no answer back. A design that needs a verdict, an order or a right of refusal belongs to a protocol
  that returns one.
- **Keep `CanReceive` pure** — deterministic, side-effect free, cheap. It is asked before every
  broadcast, and it is the one place where a throw is fatal to the whole broadcast.
- **A failure in `Receive` is collected, not swallowed by the recipient.** The hub reports it; a
  recipient that catches its own exceptions takes that report away from the publisher.
- **Register by reference and unregister the same instance.** The registration token is the only
  handle.
- **The hub's lifetime is the caller's decision.** The contract fixes what a hub does, not how long one
  lives or how far it is shared.
- **A broadcast says nothing about threads.** A recipient that must run somewhere specific hops there
  itself.
