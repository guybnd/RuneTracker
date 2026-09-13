---
id: RUNE-16
title: Lay cells on an extrapolated lattice from the row's outer edges
status: Todo
priority: High
effort: M
assignee: unassigned
tags:
  - ocr
  - runes
createdBy: Agent
updatedBy: Agent
history:
  - type: activity
    user: Agent
    date: '2026-09-13T12:22:49.445Z'
    comment: Created ticket.
    id: a-2026-09-13t12-22-49-445z
  - type: comment
    user: Agent
    comment: >-
      Horizontal half of this landed in PR #22 (lattice regularisation:
      RegulariseToLattice / BuildCell / FitsLattice, comparing cell centres
      against lattice slot centres). Widths and pitch within a row are now
      consistent.


      The vertical half is NOT done and has been split out to **RUNE-19** (new):
      cell heights are still measured per row and come out 41-53 on `5 Raw.png`
      where the cells are ~52 square. That under-measurement crops the gilded
      frame's top and bottom arcs out of the ring sample, which is what produces
      the ambiguous 0.110-0.190 readings on rows 5 and 7.


      Two housekeeping notes:

      - The ticket body above references an earlier "RUNE-19" (scan stopping
      mid-row). That id has since been reissued to the height ticket; the older
      reference is to the wide-row truncation work (MaxIconStripFraction 0.50 ->
      0.80), not to a live ticket.

      - This ticket is still in Todo despite PR #22 having merged. Leaving the
      status alone rather than closing it, because its acceptance list mixes the
      horizontal criteria (met) with `4 Raw.png` row 0 pitch, which I have not
      re-measured since the lattice landed.
    date: '2026-09-13T12:56:23.573Z'
    selfAttested: true
    pin: true
    id: c-2026-09-13t12-56-23-573z
---
User's proposal, and it is the right model:

> "maybe to get around the first and last row etc, we should do only left and right edging, and account for a exact ish pixel amount of the left and right of a cell and extrapolate the rest from there?"

## Why the current model keeps failing

`SegmentIconCells` pairs every detected border run into a cell, so **every internal border has to be found correctly or the row is wrong**. Each failure so far has been a variation of one border being missed, doubled or misplaced:

- RUNE-12: cell-interior ink outvoting real borders on short rows.
- RUNE-19: the scan stopping mid-row and silently returning fewer cells.
- Still open, on `4 Raw.png` row 0 — the row clipped by the capture top — widths come out 60, 47, 39 where the pitch is plainly ~54.

## The proposed model

Cells in a row are identical and evenly pitched. So:

1. Find the row's leftmost cell's left edge and rightmost cell's right edge — the two strongest, least ambiguous borders, both against empty parchment rather than against another cell.
2. Measure the pitch from whatever interior borders *are* confident (median of consecutive differences), or from the span divided by the cell count.
3. Place every cell on that lattice.

An internal border that is missed, doubled or a pixel out then costs nothing: it is one vote in a median rather than a cell boundary. A clipped row keeps its horizontal geometry even when its vertical extent is wrong, which is exactly the case that has broken repeatedly.

`AssignLatticeGlyphBounds` already does this for `GlyphBounds`; the drawn `Bounds` — which the gold-ring metric and the markers use — is still the per-border box. This is largely a matter of extending what is already there and trusting it.

## Validation

Four fixtures now, covering 4, 5, 6, 8 and 10-row panels and 2 to 8 icons per row:

- Every row's cells equal width and evenly pitched, including `4 Raw.png` row 0.
- Gilded runes still found with the same ring separation.
- `RuneCropStabilityDiagnostics` drift stays at 0 bits.
- `NoTwoRowsClaimTheSameIcons` still holds.

## Acceptance

- [ ] Cell widths within a row vary by at most a pixel or two (gilded frames excepted).
- [ ] `4 Raw.png` row 0 matches the pitch of the rows below it.
- [ ] No fixture regresses on gilded detection or ring separation.
