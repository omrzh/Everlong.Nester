# Responsive Controls — Slot-Measured Classification

> Status: living spec. Code is the source of truth; this document fixes the vocabulary and the
> authoring rules of the responsive controls domain.

## 1. Why the framework, and not the page

A framework control that cannot render itself in a narrow slot is a dead end. `RouteTreeItem` composes
an icon, a title, an active indicator and a chevron in one row; an application can restyle the theme,
but the control decides how many parts that row has, and nothing downstream can repair a row that does
not fit.

The rejected alternative is a page subscribing to a platform size event and classifying itself:

- **Each platform's event is asked separately**, so the chrome is authored once per platform and drifts.
- **The window is the only thing a page can measure cheaply.** A control in a rail slot and the same
  control in a drawer slot are the same window — the input is wrong for the question being asked.
- **The event arrives after the first layout pass**, so the full form is already on screen and then
  replaces itself — the narrow form flashes.
- **The classification is written wherever the handler is**, which is presentation state written outside
  every named point.

Platform detection is not the answer either: a phone in landscape and a desktop window dragged narrow
want the same treatment, and a phone in landscape is exactly where the two diverge.

### Why the extension chrome, and not the platform package

The platform package is the platform's **core** assembly: the routing surface, the layout and layer
plumbing, the transition protocol, the tree primitives the framework cannot run without. A slot-width
convenience is none of those — no member of the core package depends on it, and an application that
never narrows a pane never asks for it. It therefore ships with the extension chrome, beside the control
theme that is its first consumer.

The two platforms are not single-sourced for the classification's write side — one writes a class, the
other an attached property — but the rule they classify by is compiled into both, because that is the
part the two must not answer differently.

## 2. The one decision: the class comes from the element's own slot

An element is classified by **the width the layout gave it** — not by the window, and not by anything
the application declares about the window.

Two existing surfaces answer different questions and neither is this one: the host's bounds are a window
fact rather than a slot, and a node's arguments are identity, which the class must not become. The class
is a measured fact about presentation: it never enters identity, it is never read back, and it has no
second writer.

## 3. The mechanism

`CompactBelow` is an attached property holding a width in device-independent pixels. While the element's
slot is narrower than that width, the framework puts `compact` in the element's classes; when the slot is
no longer narrower, it takes it out.

- **The subscription is the element's own size-changed signal**, one subscription per element.
- **A width of zero means "not measured yet".** The element keeps the full form until a layout pass
  gives it a slot, so a freshly created or recycled container never renders compact because it has not
  been arranged.
- **A class, not a pseudoclass.** A behaviour outside the control cannot set a pseudoclass, but it can
  add a style class — and the class is what both sides share: the framework writes it, the theme
  selects on it.
- **The other platform states the same thing as a property.** It has no style classes, so the
  classification writes a read-only attached property and the control theme's trigger reads it directly.
  The shape is what the two platforms share — the element asks, the framework measures, the theme
  states the reaction — and the difference is only what the theme reads.
- **The rule is stated once, the plumbing twice.** Both platforms answer the same predicate: the
  threshold is live, the element has been measured, and the slot is narrower. Each platform keeps its
  own half — what a classification is written to, and which size signal raises it.
- **Without a threshold the element is unarmed**: no subscription, no class. Zero is the default, so
  opting in is the only way to be classified.

The class is a style class, not state: nothing reads it as an input, and the element it is put on does
not write it.

### The reaction is stated as a style

The rail form is a set of setters on the control theme, not code in the control: the fixed indicator
gutter goes, the header centers on the icon, and the title, the expand button and the children host are
hidden — children are never laid out in a rail. They are declared last in the theme on purpose: an
equal-specificity setter declared later wins, which is what makes them hold against the expansion and
highlight rules above them.

Naming the entry is the one part of the narrow form the theme does not state: the item sets its own
tooltip from its title when that property changes, in code. The title is the item's own value, and
setting it is not a reaction to the slot. The tip is set in both forms rather than only in the compact
one, because the item does not read its own classification.

### A local value outranks the class

The class changes what a style sets; it cannot change a value written on the element itself. A local
value outranks every setter that tries to narrow it — the class arrives, the element does not move, and
the neighbouring star column absorbs the whole resize instead. A sidebar therefore declares its width in
a style and lets the compact style narrow it.

## 4. The declaration lives where the knowledge is

A threshold is a statement about a control's content, so the control that knows its content states it.

- The rail item declares its own threshold in its constructor: the item is the thing that composes
  icon, title and chevron.
- An application's layout declares the threshold at which it replaces a pane with a rail, and the
  sidebar declares the one its labels need.
- A tree primitive states nothing: it renders a header the application supplies and cannot know whether
  that header needs room.

The layout's threshold and the control's threshold are **independent and need not agree**. The layout
decides when a sidebar becomes a rail; the tree inside it decides when its titles no longer fit. Neither
knows the other's number, and the same tree placed in a drawer keeps its titles because the drawer is not
narrow for a tree.

### A form that cannot express containment gets a projection

A rail has no room for the children a heading exists to hold, so a heading rendered there is an icon that
does nothing. The application therefore feeds its two forms two projections of one menu: the tree for the
docked form, and the destinations the tree exposes — depth first, headings dropped — for the rail.
The projection is derived from the menu, so the two cannot drift, and every destination survives,
which also means a navigable section keeps its place ahead of its own children rather than being
replaced by them.

The projection is what makes the rail safe rather than a free choice: the compact reaction hides the
children host and the expand button, so a tree that keeps its hierarchy below its threshold cannot reach
a group's children at all. The two forms differ in what they can express, not only in what they show.

## 5. A restructure that does not move the wide layout

Centering the icon needs the indicator's fixed column gone, and a column definition cannot be resized by
a style. The fix is to keep the gutter as an element and drop its fixed column: the indicator moves
inside a fixed-width panel and the column becomes automatic.

- **Wide**: the panel is always its width, so the column is, and the icon sits on one position for every
  row.
- **Compact**: the panel is hidden, an invisible element is not measured, the column is zero, and the
  header centers on the icon with no width-dependent margin.

## 6. Invariants

1. **The class is measured, never declared by the element's own code.** An element asks to be
   classified; it does not classify itself, and it never writes the class.
2. **The window is not the input.** An element is classified by its own slot, so a control in a narrow
   container is narrow whatever the window does.
3. **Zero width is not narrow.** The full form stands until a layout pass measures the element.
4. **The class is not identity.** It is not an argument, it is not recorded, and it is never read back.
5. **The reaction is a style.** A control's own code states the threshold; what the narrow form looks
   like is the theme's, so the wide and narrow layouts cannot disagree about structure.
6. **No cross-platform detection.** A platform answering for a width is the bug this replaces.
7. **A reaction is a style setter over a property the element does not set locally.** A class cannot
   outrank a local value, so an element that writes the property in markup is never narrowed.
8. **A compact slot carries destinations only.** The threshold is a statement about the row the item
   composes, while the compact reaction also drops the chevron and the children host — so a tree fed a
   hierarchical menu below its threshold loses the way to reach a group's children, silently. The
   control states the row's width; the application states the projection.

## 7. Why not the platform's own container queries

The platform already carries a styling feature in this territory: a container declares its name and
sizing, and a query in a style applies setters to the container's descendants while its size is inside
the query's range. It is not the mechanism here, for three reasons.

- **The query is about a named ancestor, not the element's own slot.** The item would be styled from the
  tree's width, so its threshold would be a statement about its parent, and an application that placed
  the tree somewhere narrower would have to state it again.
- **It reaches styles only.** The control cannot read its own classification, so it cannot react in
  code.
- **It is one platform's.** The chrome compiles the same shared file into both packages, where the
  signal does not exist on the other.

The two coexist: a container query is the tool for a theme that restyles a subtree from a declared
container, and this mechanism is the tool for a control that knows its own content's width needs.

## 8. Limits

- **One threshold per element.** The property produces `compact` or nothing. Three regimes — expanded,
  rail, overlay — need a second threshold or a named list, and nothing in the mechanism prevents it.
- **Width only.** There is no height threshold.
- **The classification is visible to styles, not to code.** A model that needs the class would be the
  first caller that reads it.
- **The terminal surface has no implementation.** Its slot is a character count, so the vocabulary holds
  but no threshold transfers.
