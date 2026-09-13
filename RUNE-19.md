---
id: RUNE-19
title: >-
  Derive cell height from cell width — cells are square, but height is measured
  per row
status: Todo
priority: High
effort: M
assignee: unassigned
tags:
  - ocr
  - runes
  - bug
createdBy: Agent
updatedBy: Agent
history:
  - type: activity
    user: Agent
    date: '2026-09-13T12:56:09.766Z'
    comment: Created ticket.
    id: a-2026-09-13t12-56-09-766z
---
## Symptom

Two things the user has reported repeatedly turn out to be one bug:

1. Markers look squished / mis-sized, "especially the ones that have overflown".
2. Runes in some rows classify as *ambiguous* (gold ring 0.110–0.190) instead of cleanly plain or cleanly gilded, so they get no marker at all.

## Measurement

On `tests/fixtures/runeicons/2560x1440/5 Raw.png` (live capture from the user's panel), `LocateIconRow` returns these row heights:

```
41, 42, 43, 45, 49, 53
```

The cells in that panel are **~52 square**. Width is now measured correctly — the lattice work (RUNE-16 / PR #22) fixed the horizontal geometry. The vertical extent is still found independently per row by scanning for the icon band, and it under-measures by up to 11px on rows where the band blends into the row text or the row is clipped.

Rows 5 and 7 of that capture are the ambiguous ones. A short box crops the gilded frame's top and bottom arcs out of the sample, which is exactly the part of the ring that carries the gold.

## The fix

Rune cells are square. Once the lattice has a confident cell **width**, the height is that width — the row's vertical placement is then a centring problem (where does the square sit), not a measurement problem.

Sketch:
- `RegulariseToLattice` already produces a confident `cellWidth`. Use it as the height.
- Keep `LocateIconRow`'s result only as a *centre* hint: place the square so its centre matches the detected band's centre, clamped inside the row zone.
- `RowDistancePenalty` (currently an unproven guard — the RUNE-12 fixture passes with it at 0) may become redundant once the height stops varying; check whether it can go.

## Why this is its own pass

This moves every cell's box by a few pixels, and the box is what `GoldHueRingProportion` samples. It changes **classification**, not just drawing — rows that currently read correctly could flip. It must be measured against all five fixtures before and after, not bolted onto a UI change.

## Validation

Fixtures: `1 Raw.png` (6 rows), `2 Raw.png` (5 rows, 2-icon rows, one hovered), `3 Raw.png` (10 rows), `4 Raw.png` (8 rows, 8-icon rows), `5 Raw.png` (live capture).

## Acceptance

- [ ] Every row's cell height equals its cell width, within a pixel.
- [ ] `5 Raw.png` rows 5 and 7 read cleanly plain (ring well below the gilded band) rather than 0.110–0.190.
- [ ] No fixture regresses on gilded detection, and ring separation (best minus runner-up) does not shrink on any row.
- [ ] `RuneCropStabilityDiagnostics` drift stays at 0 bits.
- [ ] `NoTwoRowsClaimTheSameIcons` still holds.
- [ ] Decide `RowDistancePenalty`: keep with evidence, or remove.
