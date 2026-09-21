# Shell — Assembly, Layers, Lifetime

> Status: living spec. Code is the source of truth; this document fixes the vocabulary and the
> authoring rules of the Shell domain.

## 1. Shape

The shell is one object per window: its surface (stage and host), its container, its layer ledger and
its teardown. `IShell` is deliberately thin — dispatch, error reporting, lifetime, services and the
host handle — and it is a contract, not a component registry.

`IShell` does not declare the layer broker (which grants leases) or the activation channel (which
receives activation intents). `ShellBase` implements both explicitly, and registration publishes the
shell once as `IShell`, `ILayerBroker`, `IErrorReporter`, `IIntentDispatcher`, its concrete type,
`IShellLifetime` and the container's non-keyed `IHostLifetime`. User code injects the broker; it never
reaches it through `IShell`.

`ShellBase` is the platform-neutral core — assembly, dispatch, error routing, the ledger, teardown —
and the platform shells supply the leaves of §4.

## 2. Lifecycle

`ShellLifecycle` walks `Created → Assembling → Assembled → Started → Disposed` exactly once.
Only two shortcuts exist: teardown may jump any state to `Disposed`, and re-entering `Assembling` is
idempotent (a constructor path plus `Start`, or a retry). Any other jump throws.

| State | Meaning |
|---|---|
| `Created` | The object exists; the container is not built. |
| `Assembling` | Services are being registered; the window scope is being cut. |
| `Assembled` | Container, stage and host exist — `Services` resolves. |
| `Started` | The intent pipeline answers dispatches. |
| `Disposed` | Everything owned by the shell is released. |

`IShellLifetime : IHostLifetime` exposes the state plus the signals: `Startup` (settles when the
startup flow settles, faults on startup failure or on disposal before startup finished), `Stopping`
(teardown began) and `Stopped` (the cascade completed). Both tokens stay readable after disposal — the
sources are never disposed on purpose.

State is a gate, not decoration:

- `Services` throws before assembly and after disposal.
- `DispatchIntent` returns `Pass` unless the lifecycle is `Started` **and** the pipeline is built —
  the ready-anchor window answers nothing.
- `Acquire` throws once the shell is disposed.

## 3. The assembly sequence

Creation is pure and precedes the container: a shell is constructed with its optional startup intent,
its Director declared by type, and nothing built. `Start()` then runs the following synchronously to
the presentation anchor:

1. `EnsureAssembled` — the shell builds its provider, and assigning the root cuts the window scope
   from it (assign-once).
2. `BindAgent` — the declared activation agent wins, otherwise the container supplies one; the agent
   is bound to this shell (§8).
3. `PrepareDirector` — a hand-assigned Director wins, otherwise its declared type is resolved through
   the container's injector; neither present throws.
4. `PrepareStage` — the platform stage, with the ledger connected to it.
5. `PrepareHost` — the window, a host view, or nothing; the platform decides between a direct mount
   and a fail-fast.
6. `ConnectHost` — attach to the visual root, mount the stage, present, promote; the shell is then
   tracked by the app lifetime.
7. `OnAssembled()` then the Director's own ready hook — services live, host connected, **pipeline not
   yet active**.
8. `Activate()` — fold the shell's intent pipeline (§5).
9. `ObserveStartup(OnStarted(RunStartupDispatch()))` — the startup dispatch, fire-and-forget.

A second `Start()` throws, because the lifecycle has already walked past `Assembled`.

### The startup dispatch

`RunStartupDispatch` sends the input through the shell's own pipeline: the bound agent flushes its
startup input, then the shell's own startup intent (if any), then — when nothing decided the
consultation — an empty shell-activation intent the Director interprets as the default first
navigation. "Decided" means exactly one thing: a dispatch returned something other than `Pass`.

`OnStarted` is the async tail (the default awaits the dispatch); its completion settles the startup
signal, and a failure faults it and routes an error report on the UI thread. Callers observe the
signal; they never await `Start()`.

## 4. Platform hooks

`ShellBase` fixes the order and never references host or stage types. The platform supplies the leaves:
the provider, the lease, the stage, the host surface and its wiring, the window or view that
participates in the intent chain (a pass-through stands in when there is none), the platform's own
intent families as the chain's last link, the ready / startup / terminal-error hooks, the native
handle and the platform services. Each leaf states its own role.

The framework never presents the window: the host presents itself in the ready anchor (the
invisible-presentation convention), which is also where pre-flight assembly belongs. The UI thread must
not block there.

## 5. The intent chain

The shell's pipeline folds four stages: **layers → Director → host → fallback**. Layers are asked
topmost first (the stacking order the layer domain fixes); the Director is the application's decision
surface; the host optionally participates; the fallback is the platform's own families — the window
intents and the shell-lifecycle pair — reached only when nothing upstream settled.

Three shell-side facts belong here. The layers stage reports a throwing handler and lets the dispatch
continue. The Director stage arbitrates a throw through `HandleError`: accepted settles as `Pass`,
declined rethrows to the dispatch caller. The third is the two families themselves. A window intent is
answered where a window exists, so a surface without one leaves it `Pass` — consuming it would report a
window operation as performed on a surface that has no window. The shell-lifecycle pair is the other
way round: ending the shell is a fact about every surface, so every platform answers it.

## 6. The Director

`IShellDirector : IIntentHandler, IErrorHandler` adds the per-director ready hook. Its intent handling
is the application's own interpretation of shell intents (activation input, close probes, default
navigation); its error handling decides whether a reported exception is terminal for the app or
absorbed. A Director that does not guard its own handling must expect its error handler to be consulted
with that exception.

## 7. The layer broker

The shell is the broker: `ShellBase` implements `ILayerBroker` and the lease-facing `ILayerLedger`, and
registration publishes the broker as the container's injection point. Connecting the stage wires the
ledger to it, teardown evicts every live lease (§9 step 3), and `Acquire` throws once the shell is
disposed.

## 8. Activation binding

An activation agent is bound before `Start` and released at teardown. The declaration slot transfers
ownership to the shell; without it the shell resolves an agent from its own container (late assignment
— the registrant owns it). Binding registers the shell as the agent's activation channel; unbinding is
identity-checked, so a shell that was succeeded by another binding neither stops the agent nor reclaims
it. A declared agent is disposed at teardown only if this shell was still its bound channel.

## 9. Teardown

`IShell.DisposeAsync` is the single teardown entry — idempotent and re-entrant (the guard is set before
the first await). The order:

1. advance the lifecycle to `Disposed` and fault the startup signal (waiters never hang);
2. cancel the stopping signal (a broken observer must not abort the cascade);
3. evict every live lease, topmost first — tenants learn their slot is gone before the container does;
4. release the activation agent (a declared one is disposed only if this shell is still bound);
5. release the window container — scope, then root (the shell owns both);
6. untrack from the app-exit cascade, so a shell that closed at runtime leaves the registry here;
7. cancel the stopped signal — the teardown cascade is complete.

Platform shells override to detach their surface. Cancellation and unmount failures are best-effort:
the guard is already set, so a failed cascade is not retryable and must not abort the rest.

## 10. App lifetime and multi-shell

A shell is tracked when it is mounted, and it untracks itself as its own teardown completes — so the
app-exit cascade never disposes it twice. Declaring the main shell is a statement, not a presentation:
only a started shell may be promoted (an inactive shell would silently drop activation input), and in
single-view apps the previous main shell is replaced.

The shell's lifetime is registered as its container's non-keyed process lifetime, so window-scope
services observe the teardown signals without referencing the shell. The host itself — the registry,
the cascade order, the dispatcher and the error hooks — is the hosting domain's.

## 11. Authoring rules

- Never `new` a lifetime or manage its tokens — the shell owns its own, the app lifetime owns its own.
- Never show the window from framework or user code — the host presents itself in the ready anchor.
- Never resolve services before assembly; the container does not exist until `InitializeServices`
  returns.
- Never dispatch from the ready anchor expecting an answer; the pipeline activates afterwards.
- Never destroy the shell by closing the window directly — the close path is an intent whose end is
  the one destruction entry.
