---
id: RUNE-12
title: LocateIconRow is outvoted by cell-interior ink in rows with few icons
status: Done
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
    id: a-2026-09-13t10-11-52-563z
  - type: activity
    user: Agent
    date: '2026-09-13T10:19:18.022Z'
    comment: Updated title. Updated description.
    id: a-2026-09-13t10-19-18-022z
  - type: comment
    user: Agent
    comment: >-
      Fixed and merged as PR #16. 944 passing, deployed at 21:11.


      **The approach this ticket proposed did not work, and I measured that
      before building it.** Bounding the zone by the row bar's horizontal rules:
      they reach only 52-53% dark on the wide rows and are absent on the narrow
      ones. Nothing to anchor to. The diagnostic is in the branch.


      **What worked instead:** try every placement in the zone at several
      plausible heights and keep the one that segments best. That only became
      affordable because RUNE-14 latched the panel read — segmentation now runs
      once per panel rather than every cycle, so a ~45-placement search per row
      is nothing. It also scores the thing actually being looked for instead of
      a proxy that glyph ink can imitate.


      **Scoring took two corrections, both caught by the existing fixture rather
      than by me:**

      1. Geometry alone chose bands a pixel off that measured the plate instead
      of the border, halving the gilded ring (0.31 → 0.19) and breaking the
      regression margin.

      2. Maximising the best ring alone let a band catch the brown row border,
      lifting every cell including plain ones (0.07 → 0.09, into the ambiguous
      band). It now maximises the *gap* between the best cell and the rest.


      Both are recorded in the code.


      **Result on `2 Raw.png`** — all five rows, gilded first in each, including
      the hovered row: rings 0.28-0.47 gilded against ≤0.013 plain. `1 Raw.png`
      unchanged in outcome with stronger rings than before (0.29-0.33, was
      0.27-0.31).


      **Residual risk:** validated on two fixtures at one resolution. The
      placement search assumes cells are square and a row's cells are equal
      width, which held on everything seen so far. A second resolution would be
      the real test — still RUNE-3's job.
    date: '2026-09-13T11:12:12.271Z'
    completionComment: true
    id: c-2026-09-13t11-12-12-271z
  - type: status_change
    from: Todo
    to: Ready
    user: Agent
    date: '2026-09-13T11:12:12.271Z'
  - type: comment
    user: Agent
    comment: >-
      Merged as PR #16. 944 passing, deployed. All five rows of the failing
      fixture now find their gilded rune, with gold-ring separation of 0.28-0.47
      against ≤0.013. The original proposed approach (bar rules) was measured
      and discarded; placement search replaced it, made affordable by RUNE-14's
      latch.
    completionComment: true
    date: '2026-09-13T11:12:21.984Z'
    completion:
      changedFiles:
        - src/OCR/RuneIconFingerprinter.cs
        - tests/src/OCR/RuneNarrowRowFixtureTests.cs
        - tests/src/OCR/RuneRowGeometryDiagnostics.cs
        - tests/src/OCR/RuneIconFingerprinterTests.cs
      decisions:
        - >-
          Search placements instead of voting on ink-run medians; affordable
          only because the panel read is latched.
        - >-
          Score on cell count first, then the GAP between the best gold ring and
          the rest, then width evenness — maximising the best ring alone lifted
          plain cells into the ambiguous band.
        - >-
          Keep the old median placement as a fallback for a zone nothing scores
          in.
        - >-
          Discarded the ticket's original bar-rule approach after measuring the
          rules at 52-53% dark on wide rows and absent on narrow ones.
      residualRisk: >-
        Validated on two fixtures at one resolution. The search assumes square
        cells of equal width within a row; a second resolution remains untested
        (RUNE-3).
      docsUpdated: false
    id: c-2026-09-13t11-12-21-984z
  - type: status_change
    from: Ready
    to: Done
    user: Agent
    date: '2026-09-13T11:12:22.283Z'
needsAction: null
baselineCommit: 6cd8ba4f1bc1f2afe4f5996302ff0abda9ee5b05
implementationLink: 'https://github.com/guybnd/RuneshapePriceChecker/pull/16'
swimlane: null
diffSummary:
  - file: src/OCR/RuneIconFingerprinter.cs
    additions: 92
    deletions: 2
  - file: tests/src/OCR/RuneIconFingerprinterTests.cs
    additions: 6
    deletions: 6
  - file: tests/src/OCR/RuneNarrowRowFixtureTests.cs
    additions: 131
    deletions: 0
  - file: tests/src/OCR/RuneRowGeometryDiagnostics.cs
    additions: 30
    deletions: 1
---
## Corrected diagnosis

The ticket originally blamed the capture region clipping the top row. That is real but it is **not** the main cause, and the title was wrong. Measured on the new fixture, the icon-row height comes out wrong on four rows of five:

```
2 Raw.png (663x715, 5 rows)         expected icon height 44-51
row 0: iconRow h=32   cells 36x32   <- 5 icons, clipped at the capture top
row 1: iconRow h=45   cells 45x45   <- 5 icons, correct
row 2: iconRow h=35   cells 41x35   <- 2 icons
row 3: iconRow h=41   cells 55x41   <- 2 icons
row 4: iconRow h=29   cells 26x29   <- 2 icons
```

Against `1 Raw.png`, where every row has 4-6 icons, all six rows come out at h=45 with 45x45 cells. **The variable is how many icons the row has, not whether it is clipped.**

## Mechanism

`LocateIconRow` collects every column whose contiguous ink run is 0.6-1.35x the expected icon height, then takes the **median** top and bottom. For row 2 the real border runs are clearly present:

```
x= 50: 127-180(54)   x= 51: 126-187(62)   x= 52: 133-188(56)
x= 54: 122-181(60)   x= 61: 113-171(59)   x=105: 127-171(45)
```

but so are ~18 runs like these, from x=0 to x=48 — inside the gilded cell:

```
x= 11: 153-188(36)   x= 19: 158-188(31)   x= 35: 153-187(35)
x= 12: 155-187(33)   x= 32: 157-187(31)   x= 47: 160-188(29)
```

Those are the glyph's dark strokes in the lower half of the cell running continuously into the cell's bottom bevel, the shadow beneath it and the row separator. They are the right *length* to pass the filter, so they are counted as border lines.

With 18 spurious against 8 real, the median lands in the spurious cluster and the whole row is placed ~26px too low. A wide row has 5-6 cells and therefore enough genuine border columns to outvote the same noise — which is exactly why `1 Raw.png` never showed this and why row 1 here is fine.

## Approaches already ruled out (on this data)

- **Discard runs truncated at the zone boundary.** Removes some spurious runs but also two real ones (`x=52`, `x=53`), and 9 spurious survive against 6 real. Still loses the vote.
- **Raise `BorderRunMinRatio` to ~0.8.** Cuts spurious from 18 to 5 against 8 real, but the surviving medians still give a 61px extent — and it narrows the window for genuinely short rows.
- **Reject wide contiguous groups of candidate columns.** The spurious columns are not contiguous (0, 4-5, 8-13, 18-20, 32-33, 35-39, 47-48), so grouping does not separate them.

## Proposed approach

Bound the search zone by the **row bar's own horizontal separators** instead of by multiples of the text height. Each Combinations row is drawn as a bar with a dark rule above and below spanning the full panel width — an unambiguous, full-width feature, unlike the ±text-height guess that currently lets the zone run into the separator and the next row. That would:

- remove the separator and next-row ink from the candidate pool entirely, which is where every spurious run here comes from;
- fix the clipped top row too, since a bar's bottom rule is present even when its top is off-frame;
- replace a calibrated ratio with a measured structure, which is the same move that fixed RUNE-9.

Not attempted yet. It is a rewrite of the zone derivation, and shipping a guessed heuristic into the shared OCR path would risk the rows that currently work.

## Landed so far

- `tests/fixtures/runeicons/2560x1440/2 Raw.png` — the user's own capture, reproducing the bug. Also satisfies RUNE-3's second-fixture need.
- `RuneRowGeometryDiagnostics` — prints per-row zone, located icon row and every detected cell with its gold ring, for both fixtures.
- `IsInkAt` made internal so the diagnostics can walk the same predicate.

## Ground truth for 2 Raw.png

5 rows, icon counts [5, 5, 2, 2, 2], gilded cell at index 0 in **every** row (confirmed by eye at 4x on the raw capture — all five first cells carry the gold frame and its three tabs). Row 3 is hovered, so it also exercises the RUNE-4 gold-wash path.

## Acceptance

- [ ] All five rows of `2 Raw.png` locate an icon row of 44-51px and place cells on the icons.
- [ ] Gilded cell found at index 0 in all five rows, or the hovered row documented as a known limitation with evidence.
- [ ] `1 Raw.png` still passes unchanged.
