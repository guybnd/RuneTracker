---
id: RUNE-22
title: >-
  Gilded purple rune in cell 6 is dropped in 5 of 6 rows — cell lattice drifts
  right of its gold frame
status: Grooming
priority: High
effort: None
assignee: unassigned
tags:
  - bug
  - runes
  - ocr
  - fixtures
createdBy: Agent
updatedBy: Agent
history:
  - type: activity
    user: Agent
    date: '2026-09-13T13:20:23.409Z'
    comment: Created ticket.
    id: a-2026-09-13t13-20-23-409z
  - type: activity
    user: Agent
    date: '2026-09-13T13:24:42.182Z'
    comment: >-
      Created worktree for branch
      flux/RUNE-22-gilded-purple-rune-in-cell-6-is-dropped-in-5-of-6-rows-cell-
    event: worktree-created
    id: a-2026-09-13t13-24-42-182z
branch: flux/RUNE-22-gilded-purple-rune-in-cell-6-is-dropped-in-5-of-6-rows-cell-
---
## Symptom

On a 2560x1440 Combinations panel with six-icon rows (capture region 663x715), every row has a gilded rune in cell 2 and a gilded purple rune in cell 6. The marker overlay frames all seven cell-2 runes but only one of the six purple cell-6 runes (row 4, Leylines). Nothing is logged at Information level: the dropped cells are classified `Ambiguous` and skipped with a Debug-only message.

Fixture: `E:\Git\RuneshapeCaptures\incoming\2026-09-13-purple-gilded-missed.png` (raw capture of the exact panel, taken with the app's region 69,205 663x715).

## What the fingerprinter actually does on that capture

Gold frame of cell 6 is at x=273–275 / 322–324 in **every** row (identical pixels, checked per column). So the game draws all six purple cells the same; the difference is purely in `RuneIconFingerprinter` segmentation.

Per-row dump (`DetectCells` + `SegmentIconCells` + border runs):

| row | located band | cells segmented (x, width) | cell 6 ring | result |
|---|---|---|---|---|
| 0 | [9,53] h=45 | 0w52 G57w52 115w52 169w56 **231w52 289w52** | 0.041 | plain |
| 1 | [105,157] h=53 | 0w52 G57w52 115w56 175w55 235w55 **295w55** | 0.095 | ambiguous |
| 2 | [224,268] h=45 | 0w52 G57w52 115w44 169w45 **231w45 289w45** | 0.106 | ambiguous |
| 3 | [320,374] h=55 | … 115w55 175w54 235w54 **295w54** | 0.092 | ambiguous |
| 4 | [439,483] h=45 | 0w52 G57w52 115w45 169w45 223w45 **G273w52** | 0.318 | GILDED ✔ |
| 5 | [537,589] h=53 | … 115w56 175w55 235w55 **295w55** | 0.091 | ambiguous |

True plain cells are 45 px wide at pitch 54 (right edges 160, 214, 268); cell 6's frame starts at 273. In the failing rows the cell-6 box lands 16–22 px to the right of the frame, so the ring band samples plate/glyph instead of gold.

## Two causes, both geometric

**A. Wrong band height (rows 1, 3, 5).** Text height 28 × `IconToTextHeightRatio` 1.9 = 53 is the first `CandidateRowHeights` entry; the true icon height 45 is only the 0.85 fallback. `ScorePlacement` scores on cell count first, and the 53-band segments the same six cells, so the tie is decided by ring-separation/squareness noise and flips row to row (45,53,45,55,45,53). With a 53 band, `CellWidthMinRatio` 0.85 → minWidth 46, which **rejects the real 45 px right border** of every plain cell; pairing skips to the next run, cells come out 55–56 wide, `RegulariseToLattice` derives pitch 60 instead of 54, and cell 6 is rebuilt at x=295.

**B. Silver cell 5's frame is at the ink threshold (rows 0, 2).** Band is right (45) but the light-grey frame of the silver-tier rune in cell 5 covers 37–39 of the 39 rows `BorderColumnCoverage` 0.85 demands (`IsIconInk` = s>0.28 or v<0.35; silver is low-saturation, mid-value). Row 0 loses its right border (37), row 2 loses both (38). With cell 5 mis-paired, the lattice pitch comes from the left-hand gaps, which are inflated to 57–58 by the decorated cells (blue-framed cell 1 and gilded cell 2 have their frame's outer edge as X), so slot 5 lands at 289 = 273 + 4×4 px of accumulated error. Row 4 only works because its four silver-frame columns all read exactly 39/39.

## Fix directions (to groom)

- Derive the lattice pitch from plain cells' **right edges** (as `AssignLatticeGlyphBounds` already does) rather than left edges, or from the dark bevel lines, so decorated cells cannot inflate it.
- Let `SegmentIconCells` accept a right border narrower than `CellWidthMinRatio × band` when the band is taller than the cells are wide (or score `ScorePlacement` so a band whose cells are square beats one whose cells are 8 px shorter than the band).
- Lower `IsIconInk` sensitivity to light frames or count the cell's dark bevel line as the border, so a silver-framed cell is not lost at 38/39.
- Log ambiguous drops at Information when the same row also has a confirmed gilded cell, so this fails loudly.
- Add the capture above as `tests/fixtures/runeicons/2560x1440/6 Raw.png` with an expectation of two gilded cells in rows 0–5 and one in row 6.

## Side note

The installed copy's `config/rune-catalog.json` (saved 23:03 local) holds only two unbound sprites: the silver glyph ×6 and one purple ×2. That was an earlier panel, not this one, and is unrelated to the drop above.
