# Terminal.Gui — Platform Notes

> The Terminal.Gui v2 platform surface (`Everlong.Nester.TerminalGui`): the rendering and interaction
> facts confirmed by reading the Terminal.Gui source and by headless pixel experiments, and the
> conclusions on what the layer model degrades to on this platform.  Version baseline:
> Terminal.Gui **2.4.17** (net10.0).
>
> The surface is experimental: it rides the main line so it keeps being built and checked, and it stays
> off the packaging and release line until it earns a place there.

---

## 1. Render model: five facts to remember

1. **No ZIndex, no compositor.** Stacking is the `SubViews` collection order (painter's algorithm:
   later draws cover earlier ones, cell by cell, no alpha, no translucency).
2. **A source comment and the observed behaviour disagree (2.4.17).** `ViewBase/View.Drawing.cs`
   documents `DrawSubViews` as "SubViews earlier … drawn last (on top)", but headless pixel experiments
   show the **last added view draws on top**.  `TuiStage` orders by the observed behaviour (add in
   ascending z, the highest z last = top).  If Terminal.Gui is ever corrected to match its own comment,
   that ordering stops holding silently — a change here must be re-measured against the target version
   with the procedure in §3.  A fragile adaptation point, noted at `TuiStage.Rebuild`.
3. **A self-dirty View clears its whole viewport.** In `View.Draw`, a true `needsDrawSelf` (own
   `NeedsDraw`) runs `DoClearViewport` first, filling the entire viewport with the background before
   drawing the children.  Once a full-screen surface dirties itself and redraws, it wipes out the
   lower surfaces it covers, whole-screen.
4. **A dirty subtree is not a dirty self.** A child change sets only the parent's `SubViewNeedsDraw`:
   the parent redraws its children and does **not** clear its own viewport.  Hover / focus / partial
   content updates (subtree level) are therefore safe and do not erase across surfaces.
5. **A global redraw (resize / force redraw) redraws the whole tree**, naturally top-down, so overlays
   are safe.

### Corollary

A full-screen surface over another full-screen surface is **mutually destructive** on Terminal.Gui: the
upper one's self-dirty wipes the lower, the lower one's self-dirty wipes the upper, and the upper — being
"clean" — does not repaint.  An overlay (dialog / notice) must therefore be a **content-sized window**
(MessageBox semantics) that erases only inside its own small rectangle.

---

## 2. The layer model's degraded semantics

| Nester concept | Correct form on Terminal.Gui | Note |
|---|---|---|
| Ground navigation layer | full-screen page (a full-screen stage surface) | the only full-screen band |
| Dialog band | **content-sized, centred floating window** | `NesterView.FloatingSize` + `AdoptFloatingChain` shrinking the lease surface after the reveal |
| Notice band | content-sized, edge-anchored small overlay (future) | same "window" semantics; it cannot be a translucent bar |
| Dimmer (translucent scrim) | **does not exist** | no alpha; the `DefaultDimmerModel` TG view only carries the "window shell / positioning host" role |
| Lower layer alive + overlay stable | holds for static content; a **whole-surface** self-repaint of the lower layer wipes the overlay once | residual erase vector (see §4) |

An overlay is stable while no "full-screen and self-dirtying" surface shares its area.  Page content
changes travel the subtree-dirty path (safe); only when the navigation surface itself lays out / dirties
(rare) is the overlay wiped — and it is not repainted automatically.

---

## 3. Verification technique (headless pixels)

When troubleshooting there is no real terminal to look at; the technique used in the end is the one
Terminal.Gui's own tests use:

```csharp
using IApplication app = Application.Create().Init(DriverRegistry.Names.ANSI);
app.Driver?.SetScreenSize(120, 30);
// … start / navigate / open a dialog as usual …
// read the real screen buffer after rendering:
Cell[,] contents = app.Driver.Contents;   // contents[y, x].Grapheme
```

Inside the `app.Run` loop, `app.AddTimeout` schedules the "action → delay → capture screen" sequence and
dumps each frame's row text to disk.  A tree-level dump (recursive `SubViews` + Frame/Visible) only proves
something is **attached**; **only a pixel dump proves it is drawn** — both of the false alarms below were
exposed by the pixel dump (a dialog had the correct frame in the tree but never reached the screen).

One more environment fact: **in a full-screen TUI, `Console.Error` output is repainted away and cannot be
seen**; use `Debug.WriteLine` (or a file) for troubleshooting output.

---

## 4. Troubleshooting lessons

1. **A swallowed startup navigation failure = a blank screen and zero log.**  `MainViewModel.HandleError`
   returned `true`, consuming the exception (`Router`'s `ExecuteGuarded` → `ReportError` → consumed by the
   Director).  Diagnosis relies on a probe Director that does not swallow errors (`HandleError => false`),
   which throws the exception into the host error chain.  Fix: the shared template's Director
   `HandleError` now `Debug.WriteLine`s + `Console.Error`s before deciding what to return.
2. **An `[Inject]` member with no registration = page resolution throws.**  `LoginPageModel` injects
   `INoticeService` while the window container never called `AddNesterNotice()` → `GetRequiredService`
   throws → blank screen.  The Avalonia/WPF examples call it in the app shell; the TG one had missed it.
3. **A dialog that "looks like it did not open" was open.**  With an invisible overlay the Back intent
   reaches the dialog band first (higher z, served first), and "root of the overlay goes back = close"
   settles `ShowAsync` with `null` — the "Cancelled" that showed up on the page was in fact a closed
   invisible dialog.
4. **Console output is invisible in a TUI** (see §3); report errors through `Debug`/a file.

---

## 5. Other facts about interacting with Terminal.Gui v2

- **No data binding / INPC consumption**: `INotifyPropertyChanged` appears exactly once inside the library
  (layout-internal) and there is no binding mechanism.  Views update imperatively; a page that must follow
  its VM subscribes to `PropertyChanged` itself.
- **App model**: `Application.Create().Init(driverName)` → root view (`Runnable`) → `app.Run(root)`
  (blocking main loop) → `app.Dispose()`.  Sessions nest (`Begin/Run/End`); the top-level session owns input.
- **Cross-thread marshalling**: `IApplication.Invoke(action)` posts to the main loop;
  `AddTimeout(TimeSpan, Func<bool>)` schedules a timed callback.  The adapter `TerminalMainDispatcher`
  drains inline on the main thread and goes through `Invoke` off it.
- **Keyboard**: `View.KeyDown` (event args `Terminal.Gui.Input.Key`); modifiers `IsCtrl/IsAlt/IsShift`;
  the `KeyCode` enum lives in `Terminal.Gui.Drivers`.
- **Control events**: a button is `Accepted` (not v1's `Clicked`); a Dialog/MessageBox's **default key is
  the last added button** (Enter triggers it), so Esc/Cancel goes first.
- `View.Visible` / `Enabled` / `SetFocus()` / `BorderStyle` (`LineStyle`, the Drawing namespace) and similar
  are the commonly used surface.

---

## 6. Open / future direction (no rush once back on the main line)

- **A unified Nester redraw scheduler**: have the stage mark the whole tree dirty top-down in one pass after
  a composition change (`TuiStage.RedrawAll`), turning "mount erases" from a matter of luck into a
  deterministic discipline, repainting the overlay while it is mounted when needed.  The design is worked
  out; not implemented (the side line was paused here at the user's request).
- **A real Notice band**: `TerminalNoticeService` is a placeholder (trace) today.
- **A binding helper**: a very thin INPC subscriber (the projection stays the view author's choice) — undecided.
- **Z-order against future Terminal.Gui versions**: after a version bump, re-verify with the pixel technique
  in §3.
