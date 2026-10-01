# Session Ending — The Exit Arbitration

> Status: living spec. Code is the source of truth; this document fixes the vocabulary and the
> authoring rules of the session-ending domain.

## 1. What this domain is

A process is asked to end from directions the app does not own — the OS, a session logoff, a window's
close box — and the participants that hold the user's work are the only ones that know whether ending
is acceptable. This domain is the single arbitration between the request and the process actually
ending: one broadcast, so every participant can ask its own question, and one gate that decides
whether the end proceeds.

It is a broadcast rather than a call because the arbiter cannot know the participants: they register
with the message hub, and the hub is the channel the app already has. Three members carry the domain.
`SessionEndingMessage` is the broadcast, and `Guard` is how a recipient registers — the prompt to put
to the user, and the action that runs once every guard is confirmed. `SessionEndingArbitration.Run` is
the pass over the guards. `IModalPrompt` is the surface the answers come from.

## 2. Why the prompt is synchronous

The arbitration runs on the way out, on the thread that is ending the process, and it cannot give that
thread up: a hook that returns before its work has settled hands the platform a loop it is about to
tear down, and the continuations still queued with it are lost. Awaiting is therefore unavailable
here, whatever shape the question takes — the answer has to be on the stack when the hook returns.

`IModalPrompt` is that shape, and its synchronicity is the contract rather than an implementation
detail: a call blocks the calling thread until the user answers. Both default implementations reach it
the same way, by running a nested message loop — the platform's own message box on WPF, a dispatcher
frame on Avalonia — and a nested loop is also why a request that arrives while a prompt is up is
answered as a refusal: the arbitration declines to run inside itself.

## 3. The pass

Two rules follow from asking before acting. The first is that the questions come first and the actions
second: every guard is put to the user before any confirmed action runs, so a refusal late in the list
stops an action early in it from having run. The second is that the confirmation is the user's
informed one — by the time a guard's action runs, the user has seen every question.

Two more follow from the end being irreversible. The pass is single-flight: a second request while one
is in flight is refused rather than queued. And a confirmed action is best-effort — it runs once, and a
failing one does not strand the others, because there is no second chance to offer. An action that
throws is contained rather than reported into the exit path, which has no one left to answer it.

## 4. Constraints

- **The prompt does not decide.** A guard owns its question and its action; the surface owns the
  answer. Nothing else may convert an answer into a decision.
- **No awaited work belongs here.** A participant that has asynchronous work to flush must have
  finished it by the time its guard is registered, because the pass will not wait for it.
- **The guards are ordered by registration, and that order is all the ordering there is.** A
  participant that depends on another's action has no way to say so.
