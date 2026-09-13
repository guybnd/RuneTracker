---
id: RUNE-17
title: >-
  Merge rune rows per read instead of freezing the panel, so scroll and drag
  update without hover flicker
status: Todo
priority: High
effort: M
assignee: unassigned
tags:
  - runes
  - overlay
  - bug
  - architecture
createdBy: Guy
updatedBy: Guy
history:
  - type: activity
    user: Guy
    date: '2026-09-13T12:47:09.537Z'
    comment: Created ticket.
    id: a-2026-09-13t12-47-09-537z
---
## Problem

`LatchRuneRows` in `src/App/LeaguePricingWorker.cs` reads the Combinations panel once and freezes the result for the whole panel session. That was built on the assumption that the panel's contents never change while it is open. They do:

- The panel has a **scrollbar** — scrolling brings new combination rows into view and pushes others out. The frozen read never sees them.
- The panel is **draggable** — after a drag every `RuneKey.CellBounds` is stale and the markers sit where the panel used to be.

The freeze exists for a good reason that still holds: re-reading every frame flickers. Hovering a row tints it gold, which collapses the contrast the gilded test needs, so the rune under the cursor is exactly the one that drops out of the read. Ordinary OCR jitter also loses and regains cells frame to frame.

So the latch has to become something that can **grow and shift, but never shrink from a single bad read**.

## Approach

Replace the frozen list with a per-row cache, merged every read. Match fresh rows to cached rows **by the runes in them** — overlap of `ShapeHash`es (Jaccard or "shares ≥ N−1 keys") — never by position (scroll/drag moves them) and never by OCR text (jitters).

| Fresh row… | Meaning | Action |
|---|---|---|
| matches a cached row, same or more runes | stable, or a better read | adopt new runes and new `CellBounds`/`RowY` |
| matches a cached row, **fewer** runes | hover glow or jitter ate one | keep cached runes, adopt only the new geometry |
| matches nothing | scrolled into view | add it |
| cached row unseen for ~2 consecutive reads | scrolled out | drop it |

This one rule covers hover (identity kept, no flicker), scroll (rows shift, set changes), drag (all rows shift by the same delta, identities kept) and OCR jitter (never-downgrade absorbs it).

Matching by *overlap* rather than exact signature is the load-bearing detail: a hovered row is missing a rune and must still match its cached self.

### Geometry on a no-downgrade match

When the cached runes are kept but the fresh read has fewer cells, the missing cell's `CellBounds` can't be taken from the fresh row. Translate it by the row's delta (fresh matched cell minus cached matched cell — same for every cell in the row, since the row moves as a unit).

### Catalog observation

`runeCatalog.Observe` currently runs only when `ReferenceEquals(rows, snapshot.RuneRows)` (a fresh read), to avoid inflating sightings while a panel sits open. With a merging cache, observe **only keys newly added to the cache** — a scrolled-in row teaches the catalog once; a hovered row re-teaches nothing.

### Session boundary

Keep the session bounded by `InterfaceDetected` as today; clear the cache when the panel closes. `RuneLatchSettling` can go — the merge handles a mid-animation first read on its own (later reads only add).

## Optional second lock (follow-up, not this ticket)

Scrolling is mouse-driven (`WM_MOUSEWHEEL`, or left-drag on the scrollbar) and so is dragging. Both are visible in the `WH_MOUSE_LL` hook `RuneMouseMarkService` already runs. If a row ever still flickers after the merge lands, gate row **additions/removals** to a short window after a wheel tick or `WM_LBUTTONUP`, and pin the row set otherwise. Build the merge first; add the gate only if needed.

## Acceptance

- Scroll the Combinations list: rows that scroll in get markers, rows that scroll out lose them, within a read or two.
- Drag the panel: markers follow, with no visible re-read or flicker.
- Hover any row for several seconds: its markers do not blink or drop.
- Sighting counts in the catalog do not climb while the panel sits open and untouched.
- Unit tests in `tests/src/Runes/` for the merge: no-downgrade keeps the cached set and takes the new geometry; unmatched fresh row is added; cached row absent for N reads is dropped; a whole-panel translation updates every row's geometry without changing identities; newly-added keys are the only ones reported for observation.

## Touch points

- `src/App/LeaguePricingWorker.cs` — `LatchRuneRows`, `RenderRuneMarkers`, `_latchedRuneRows`, `RuneLatchSettling`
- `src/Contracts/RuneKey.cs` — `RuneRowKeys` / `RuneKey.CellBounds` (read-only; no shape change expected)
- New `src/Runes/RuneRowCache.cs` (or similar) so the merge is testable without the worker loop
- `tests/src/Runes/`
