---
id: RUNE-23
title: Drag to reorder the rune priority ladder
status: Todo
priority: Medium
effort: M
assignee: unassigned
tags:
  - ux
  - runes
  - dashboard
createdBy: Agent
updatedBy: Agent
history:
  - type: activity
    user: Agent
    date: '2026-09-13T13:51:07.616Z'
    comment: Created ticket.
    id: a-2026-09-13t13-51-07-616z
---
The priority ladder (RUNE-21, PR #27) reorders with ▲▼ arrows, one rung per click. Moving a rune from the bottom of the crowd to rank 2 is a dozen clicks. Dragging the row is the obvious gesture and the one the user asked for.

The arrows were shipped first because they are unambiguous, keyboard-reachable and testable; drag is the upgrade, not a replacement. **Keep the arrows** — they are the accessible path and the only way to do this without a mouse.

## What already exists

`src/Dashboard/RuneRanking.cs` holds all the ordering logic and is fully tested (`RuneRankingTests`):

- `LadderOf(runes)` — the ladder implied by current weights
- `MoveUp` / `MoveDown` — one rung, with join-at-bottom and fall-off-bottom semantics
- `WeightsFor(ladder)` / `Diff(before, after)` — which runes changed weight and to what
- `Sort(runes)` — ladder first, then the crowd alphabetically
- `RankLabel(ladder, id)`

Drag needs one new operation and nothing else from the model:

```csharp
/// Moves runeId to sit at `index` in the ladder, shifting the rest. An index at or past the
/// end of the ladder drops it back into the unranked crowd.
public static IReadOnlyList<string> MoveTo(IReadOnlyList<string> ladder, string runeId, int index)
```

`MoveUp`/`MoveDown` should then be expressed in terms of it rather than duplicating the swap, so there is one definition of what a position means.

## UI

The list is an `ItemsControl` bound to `RuneLibrary` in `DashboardWindow.xaml` (around the `RuneRankUpButton` block), with `MoveRank` in `DashboardWindow.xaml.cs` writing changes back through `_runeCallbacks.SetWeight`.

- A drag handle on the row rather than whole-row drag: the row also carries a Carried checkbox and the arrows, and whole-row drag makes those fiddly to hit.
- An insertion line between rows while dragging. Dropping into the crowd region unranks the rune; dropping above rank 1 makes it rank 1.
- Reuse `MoveRank`'s write-back path exactly — compute the new ladder, `Diff` it, push only the changed weights, then `ApplyRuneFilter()` so the row lands under the cursor rather than after the presenter's 250ms debounce.

## Watch out for

- The ItemsControl sits inside an already-scrolling settings pane. Dragging near the top or bottom edge needs to scroll that `ScrollViewer`, or a rune cannot be dragged past the visible window.
- The list is filtered (`RuneLibraryFilters`). Under a filter the visible rows are not contiguous positions on the ladder, so a drop target has to resolve to a real ladder index, not a visible row index. Simplest correct answer: work out the ladder index of the row being displaced. Worth considering whether drag should just be disabled under a filter other than "All runes" — decide deliberately rather than shipping something that silently reorders the wrong thing.
- The presenter pushes a fresh `RuneLibraryEntryView` list every catalog change; a drag in flight must not be left holding stale view objects.

## Acceptance

- [ ] `MoveTo` covered by tests, including dropping into the crowd and dropping at the top, with `MoveUp`/`MoveDown` re-expressed in terms of it and their existing tests still green.
- [ ] Dragging a row to a new position reorders the ladder and persists, with the same write-back path as the arrows.
- [ ] The arrows still work and are still tested.
- [ ] Dragging near the pane's edge scrolls it.
- [ ] Behaviour under a non-"All runes" filter is decided and documented in the code, not accidental.
