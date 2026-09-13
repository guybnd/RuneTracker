---
id: RUNE-12
title: Top row's cells are mis-segmented because the capture region clips them
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
    date: '2026-09-13T10:11:52.563Z'
    comment: Created ticket.
---
## Evidence

`6 IconCells.png` from the user's own session, preserved at `E:\Git\RuneshapeCaptures\incoming\2026-09-13-clipped-top-row\`. That image draws `cell.Bounds` directly on the capture, so it separates detection from the overlay — and it is **detection** that is wrong.

In the top row (Courtesan Mannan's Rune of Cruelty) every box is shifted right and down: the gilded box sits inset inside its cell, and each plain box straddles two cells, offset right by roughly half a cell. In the row directly below (Lady Hestra's Rune of Winter) every box wraps its cell exactly.

The top row is the one the capture region clips. `OcrResolutionProfiles["2560x1440"]` is `(69, 205, 663, 715)` and the capture begins partway down the first row's icons — the cells' top border is not in the frame at all.

The chain: a clipped icon row measures shorter than it is, so `LocateIconRow` returns a wrong height; `SegmentIconCells` accepts a left/right border pair by `CellWidthMinRatio`/`CellWidthMaxRatio` **of that height**, so the pairing window moves and the wrong border runs get paired.

This is the same row-0 clipping RUNE-3 flagged ("the 2560x1440 capture region top (Y=205) clips the first row's icons"), now with a capture that shows what it costs. It is also a strong candidate for the earlier unexplained "Warding Rune of Annihilation missed, Stability misaligned" report.

## Fixture

`raw.png` from that folder is 663x715 — exactly the capture region — so it drops straight in as `tests/fixtures/runeicons/2560x1440/2 Raw.png` and **reproduces the bug**. The existing fixture does not: its row 0 is only 1px off (`110,0 53x50` against `111,…,52x50` for every other row).

This also satisfies RUNE-3's "second fixture" need.

## Candidate fixes

1. **Make segmentation robust to a clipped row.** When the icon row's top touches the search-zone top, its measured height is not trustworthy — derive it from cell width instead, since cells are square. Contained, and testable on the new fixture.
2. **Move the capture region up ~10px.** Simpler, but the region is shared with text OCR and pricing: pulling in the panel header risks `DetectRowPositions` inventing a row. Cannot be tested without a fullscreen capture, which we do not have — the debug images are already cropped to the region.

Prefer 1. Try 2 only if 1 cannot recover a clipped row, and then only with a fullscreen capture to test against.

## Acceptance

- [ ] `2 Raw.png` added with ground truth; a test fails on it before the fix.
- [ ] Top-row cells land on their icons, matching the rows below.
- [ ] Existing fixture still passes unchanged.
- [ ] Re-check whether Annihilation/Stability reproduce once the top row is right.
