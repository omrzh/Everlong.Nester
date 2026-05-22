# Hosting — The Process-Level Host

> Status: living spec. Code is the source of truth; this document fixes the vocabulary and the
> authoring rules of the Hosting domain.

## 1. Why a host of our own

A GUI process needs process-wide machinery — a main-thread dispatcher, a process container, an error
handler, a registry of live shells, one exit cascade — and the generic application host is the wrong
shape for it on three counts that decide the whole design:

- **The run loop belongs to the toolkit.** A generic host parks in its own loop until shutdown; a GUI
  process has to return to the toolkit's loop so its windows can pump messages. The host here is bound
  *inside* the toolkit's initialization and never owns the loop.
- **Shutdown has many authorities.** A window close, an Alt+F4, a session end, a lifecycle callback or
  a browser unload all end the same process. The host's job is translation rather than command: each
  of them becomes the host's own stopping signal, not the other way around.
- **Teardown is main-thread work.** Unmounting a surface, detaching the presented content and closing a
  window all touch the toolkit's objects, so the cascade runs on the main thread — which is why the
  dispatcher is itself a host-owned service, bound before anything can hop.

What the host keeps from the generic vocabulary is the shape, not the machinery: a lifetime is a pair
of stopping tokens, and the app-level surface adds exactly what a process owns.

## 2. The vocabulary

The contracts live in `Everlong.Nester.Abstractions`, namespace `Everlong.Nester.Hosting`.

`IHostLifetime` is two tokens and nothing else: `Stopping` (teardown has begun) and `Stopped` (the
cascade has completed). Both are live from construction and stay readable after disposal, because an
observer may look late — a window-owned task that only notices the shutdown after teardown finished.
They come from a source that is deliberately never disposed: there is nothing to reclaim in a token
that was already cancelled, and disposing it would turn a late read into an error.

`IAppLifetime` is the process surface over that pair: the main shell, the registry of live shells, the
dispatcher, the error handler and the process container.

## 3. The host

A platform-neutral base owns everything shared and leaves three things abstract: how the process
actually exits, whether the app is single-view, and how a single-view surface is replaced. The rest —
the registry, the cascade, the container, the error hooks — is one implementation.

- **The registry is serialized and the gate is never held across an await.** Tracking is idempotent,
  and it is refused once the cascade has begun: a shell that arrives mid-teardown would never be
  released. Untracking is what a shell does as its own teardown completes.
- **The process container is the user's, and the host's to release.** It goes last in the cascade,
  after every shell, so a service bridged into a shell's container dies after its consumers.
- **Declaring the main shell is a statement, not a display.** It requires a shell whose lifecycle has
  already started — an unstarted one would have nobody listening for the input — and in a single-view
  app it replaces the previous surface instead of keeping two.

Assigning the dispatcher binds the static dispatch facade as a side effect, so the two can never
disagree about who owns the main thread.

## 4. The cascade

Teardown is a single idempotent, best-effort pass: cancel the stopping signal, snapshot the registry,
release every shell — a failure is reported, never fatal — clear the leftovers, then release the
process container, the error handler and the dispatcher, and finally cancel the stopped signal.

Two rules follow from that shape. The cascade is **not retryable**: the guard is set before the work
starts, so a pass that failed halfway is not run again. And a broken participant must not strand the
rest: the shells beneath it are still released.

## 5. The facade

A participant deep in the tree needs the process host without passing a reference through every
constructor, and sometimes before any container exists at all. A static facade is that reach, and its
shape is the domain's second design decision: **binding happens once per process**, because a second
host would mean two owners of the same process.

The members split by what a missing host means. Those that cannot answer without one throw, so a
missing bootstrap surfaces where it is used; those that can only observe — the bound instance, the
main shell, the process conveniences — degrade quietly.

## 6. Error reporting

The host is a participant in error handling, not a kill switch. Process-level failures — unhandled
exceptions and unobserved task exceptions — are routed to the app's error handler, and when it
declines or is absent the options decide the rest: whether the failure is escalated, and whether an
unobserved task exception is marked observed once it has been reported.

Reporting is fire-and-forget by contract. The site that reports never inspects the outcome, and a
throwing handler cannot break the error path: it is contained and logged, while the original failure
still reaches the last hook.

## 7. Constraints

- **One host per process.** It is bound by the platform bootstrap and never constructed by user code,
  and binding a different instance is refused.
- **The process container is the user's shape.** Build it, hand it over, and bridge what each window
  needs into that window's container instead of resolving window-scoped services from the process one.
- **Observe shutdown through the lifetime contract.** `Stopping` and `Stopped` are the whole surface a
  service needs in order to react to the process ending, and they stay usable outside any window's
  container.
- **The signals are read-only facts.** Nothing outside the host cancels them; teardown order is the
  host's.
