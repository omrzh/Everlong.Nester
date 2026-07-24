# RouteSync — Navigation Chrome Without Owning Navigation

> Status: living spec. Code is the source of truth; this document fixes the vocabulary and the
> authoring rules of the route sync domain.

## 1. Why

Navigation chrome — a menu, a breadcrumb, a tree — must show which item the user is on, and must
not own the stack to do it. The domain is therefore an *observation* contract plus a matcher: the
chrome holds items, the router owns the truth, and a highlight is derived — never stored.

## 2. Contracts

- **An item** is a title, an optional icon, its children, and an optional destination — a navigation
  request, absent for a pure group. A concrete immutable item implements it.
- **A participant's vote** lets a presented node claim a destination even when its type alone would not
  match; a node may also reject a destination, suppressing the default comparison for itself.
- **The matcher** is pure and static: the default target type of an item, whether an item is highlighted
  for the presented chain, and the descendant form that highlights an ancestor group.

## 3. How a highlight is derived

Matching walks the presented lineage from the current site upward. At each node the node's own vote wins
if it casts one; otherwise the node's type is compared with the item destination's content target. An
ancestor group is highlighted when any descendant matches.

Recompute is **pull-based**: the chrome listens to the stack's change notification for the current
location and recomputes. There is no push channel and no per-item subscription, so a rebuilt item
collection simply recomputes on the next notification.

## 4. Platform and entry

The chrome controls are the platform's, built over the shared matcher. The domain needs no registration:
an item collection is ordinary model data, and the source of a destination is the same descriptor the
rest of the routing domain uses.

## 5. Constraints

- **Items carry descriptors, never a realized site** — a highlight compares a descriptor with the
  presented chain.
- **Matching is pure**: no view state, no mutation, no remembering the last answer.
- **The domain is not part of the engine**: the router never consults it, and the chrome never asks the
  router to navigate.
- **Chrome is owned by the declaration** — the item's descriptor, or the presented chain — never by
  the engine; the router only follows what a declaration asks for.
