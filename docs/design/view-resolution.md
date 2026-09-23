# View resolution — A Locator Builds, Never Recycles

> Status: living spec. Code is the source of truth; this document fixes the vocabulary and the
> authoring rules of the view-resolution domain.

## 1. The locator is the application's table

A Nester application declares its view bindings once — `[ViewFor<TViewModel>]` on a view, or
`[Mapping<TModel, TView>]` on the locator trigger — and the generator turns those declarations into
**one** locator instance whose match claims all of them and whose build serves each data type the view
it declared. That instance joins the application's template list, which the platform reads as its own:
one table, one entry point.

The union is the point. The mapping set is the application's, and a locator that claimed only part of it
would make the declaration order carry meaning it does not have. It is also the property that makes the
platform's recycling contract unanswerable (why the union locator cannot answer, below).

**Resolution starts at the asking control, not at the application.** A view is resolved from the tree the
control that needs it sits in — so the nearest scope wins and the application's own table is the last
stop, exactly as the platform resolves a template. Nothing resolves through the shell: the shell owns no
mapping, and the host surface a shell presents is not a mapping at all (a shell declares its own host).
The vocabulary follows each platform's own: on Avalonia the entry point *is* `IDataTemplate` (a Nester
locator is one, and `FindDataTemplate` is the lookup), while WPF and Terminal.Gui declare their own
non-generic locator whose build returns their own control type.

## 2. What a recycling template asks

A platform recycling template adds one member to the plain template:

```csharp
Control? Build(object? data, Control? existing);
```

Its contract is a *question*: return the supplied `existing` control **if it is applicable to `data`**,
otherwise build. Three consequences matter here.

- **Deciding applicability is the template's job.** Only the template knows what control each data
  needs; the caller cannot tell it.
- **The caller's guarantee is provenance, not compatibility.** The offered control originated from the
  same template instance; nothing is said about whether the earlier data was the same *kind* as the new
  one.
- **Ignoring the offer is always allowed.** A template may build fresh every time.

The framework's only consumer offers the previous child through a single reference test: it reuses when
the template instance that built the current child is the instance now selected for the new content. It
then re-targets the presenter at the new content, so a recycled control presents the new data through
the old visual tree — which is sound exactly when the control's content comes from the data context.

The platform's own implementations show the three possible answers:

- **a data template** — always recycles. The visual tree is fixed by the template and its data type;
  whichever data matched this instance renders through the same tree.
- **a factory template** — recycles only when opted in (off by default). A user-supplied factory may
  branch on the data type, and the framework refuses to assume it does not.
- **a Nester-shaped template** — recycles only when the existing control is already the shape this data
  would produce. The one answer that gates on the type it would have built.

## 3. Why the union locator cannot answer

The reference test — *the same template instance built the current child* — is protective exactly
when **one template instance presents one kind of thing**. That is how the platform's designs are
shaped: a data template is fixed to a data type, a factory template is built around one factory, and
on WPF each mapping becomes its own template keyed by data type, so "the same instance" *is*
"the same view type". For a Nester locator the test degrades to "any view model this locator maps" —
precisely the case where
recycling is invalid. A presenter whose content changed type would keep the view built for the previous
content, silently: the second view model rendered through the first one's visual tree, with no error and
no fallback.

Two answers were available. The first gates per branch — recycle only an instance of exactly the
control type that mapping produces — which is correct and costs one reference comparison, but keeps the
two-argument member in the public contract, so every third-party locator inherits the same question and
can answer it wrongly the same way: `existing ?? Build(data)` is the natural thing to write, and it is
the defect. The second does not implement the recycling template at all: the contract declares a match
and a single-argument build, nothing is ever offered, and every mount is a fresh view. The second was
chosen.

## 4. Why refusing the offer is the right end state

- **The capability was never Nester's.** The platform-agnostic contract has always been a
  single-argument build, and the WPF locator has no two-argument build. The framework's own mount path
  has always called the single-argument overload, so every mount already built fresh; recycling was
  only ever reachable through a presenter the application owned.
- **A view instance does not outlive its data.** The mounted tree the framework holds *is* the cache: a
  view that leaves the tree dies and is rebuilt when it is mounted again. Recycling would let one view
  instance carry its local state — scroll offset, focus, an expanded flag — from one view model
  instance into another. Views present data; they do not accumulate it.
- **The mount site owns the decision, not the type mapping.** The declaration attributes are about
  *types*, and they are single-source across the platform pair. A per-mapping recycling parameter would
  be a platform-only switch on a cross-platform declaration: silently ignored on the other platform, or
  a fork of a single-source file.
- **Where per-item reuse actually pays, the platform already exposes the right switch.** A virtualizing
  panel that rebinds a live container is the one place this matters, and there the switch belongs to the
  list's item template: a plain data template, recycled unconditionally and safely because one template
  instance matches one data type — exactly the property the union locator lacks — or a factory
  template with recycling opted in. That granularity is the application's, and it is finer than a
  framework-wide mapping flag could be.

The cost is a view construction per re-mount of the same type. The tree the framework holds is where an
application gets its reuse; a list that needs more says so at the list.

### If an application wants the reuse anyway

- **Prefer the platform's shapes.** A data template fixed to the item's data type, used as the list's
  item template, is recycled safely: one template instance matches one data type, which is exactly the
  property that makes the offer answerable. The explicit form is a factory template with recycling
  opted in.
- **Implement the recycling template only as a single-purpose template** — one instance per kind of
  data, registered where the rebinding happens, accepting the offered control only when that control is
  the exact type it would have built:

  ```csharp
  public Control? Build(object? data, Control? existing)
    => existing?.GetType() == typeof(ItemView) ? existing : new ItemView();
  ```

  "Written once" means *one template per kind of thing*, not *one per application*: two lists with two
  item types are two templates, one each.
- **Never wrap the application locator in a recycling factory for a heterogeneous source.** The wrapper
  matches every data type the locator maps, so the instance test answers nothing and the previous view
  is served for the new data — the union problem with the safety check removed. A wrapper is sound only
  when the source is known to be homogeneous.

## 5. What a locator looks like

On Avalonia the contract is the platform's own — a locator is an `IDataTemplate`:

```csharp
partial class AppViewProvider : IDataTemplate          // match + Build(object?) — no recycling
{
  private readonly FrozenSet<Type> _supportedTypes;   // one entry per declaration

  public bool Match(object? data)
  {
    if (data is null) return false;
    return _supportedTypes.Contains(data.GetType());
  }

  public Control? Build(object? data)
  {
    if (data is null) return null;
    var type = data.GetType();
    if (type == typeof(MainViewModel)) return new MainView();
    if (type == typeof(DetailViewModel)) return new DetailView();
    return null;
  }
}
```

WPF and Terminal.Gui declare the same two members on their own non-generic `IViewLocator`, whose build
returns `FrameworkElement` and `View` respectively: those platforms have no template the framework can
ask to build, so the mapping has to be a contract of its own.  On WPF a locator is also a
`ResourceDictionary`, so the plain `DataTemplate`s it registers stay resolvable from the tree.

## 6. Constraints

- **A locator is a pure builder.** It claims the application's mapping set and serves a view per data
  type; it never hands back a child it built for other data.
- **Reuse belongs to the mount site.** The tree the framework holds is the cache; a list that needs
  per-item reuse says so at the list, through the platform's own template shapes.
- **The declaration is about types, not about instances.** A recycling switch on a mapping would be a
  platform-only knob on a cross-platform, single-source declaration.
- **A declared mapping outranks a plain template.** A locator can build a view no template can express — a
  control configured in code, or one whose shape depends on the instance — so on a platform whose locator
  is not the template (WPF, Terminal.Gui) it is consulted *before* the tree's plain templates. The reverse
  order would hand the mount a template's wrapper instead, and a wrapper hides the view's own
  `ISceneTransition` from the reveal.
- **A locator answers before it builds.** `Match` claims the data type and `Build` serves it, so a chain
  reads claim-then-build and a data type no locator claims is a fact the caller can report rather than a
  silent miss.  Avalonia's `IDataTemplate` already carries both members; WPF and Terminal.Gui declare them
  on their own locator.
- **A miss is not an error.** `Build` returning `null` says no view claims the data *yet*. The routing
  view leaves the node unassembled and assembles it on the next convergence, so a mapping registered
  after the first navigation still resolves; a visible placeholder would mount into the chain and could
  never be replaced by the real view. A surface with nowhere to retry answers differently — the notice
  host, which mounts once, reports the miss in place instead.
- **A shell presents a host; it does not map one.** The host surface is declared by the concrete shell,
  never resolved from the mapping table — a window is the shell's container, not a view of a Director.
