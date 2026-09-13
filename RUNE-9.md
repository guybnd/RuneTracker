---
id: RUNE-9
title: 'Anchor the glyph crop to the rune''s own dark plate, not to neighbouring cells'
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
    date: '2026-09-13T09:33:42.996Z'
    comment: Created ticket.
    id: a-2026-09-13t09-33-42-996z
  - type: activity
    user: Agent
    date: '2026-09-13T09:34:40.956Z'
    comment: >-
      Diagnostic committed as 2cac205 on branch `rune-9-crop-anchor` (not yet
      pushed or PR'd — it belongs with the fix). It is test-only, no production
      change.


      Also noting for the record: PR #10 (RUNE-8) did eventually merge, despite
      `gh` returning 500/502 on every attempt. Two of those "failed" calls
      actually went through server-side, so master carries two commits titled
      "RUNE-8 … (#10)" — a00a4ae and eeabff9. The diff between them is empty, so
      the tree is correct; it is a cosmetic duplicate in history only. Not
      rewriting pushed master history to tidy it.
    pin: true
    id: a-2026-09-13t09-34-40-956z
  - type: comment
    user: Agent
    comment: >-
      Implemented and open as PR #11
      (https://github.com/guybnd/RuneshapePriceChecker/pull/11). Release gate:
      900 passed, 0 failed. Published and deployed to the installed copy.


      **Result against the acceptance bar:**


      ```
                      worst +/-1px   worst +/-2px
      lattice (old)      26 bits        39 bits

      plate   (new)       0 bits         0 bits

      ```


      Bit-identical across the whole ±2px grid, against a bar of "under 8".
      Plate found on 6/6 gilded cells. The existing fixture assertions (same
      rune ≤8 bits, distinct runes ≥16) still pass.


      **Two things the fixture forced, neither of which was in the plan:**


      1. Only the sides and bottom of the frame sit inside the detected cell —
      the row extent clips the top (38-42% gold at the top line versus 77-96% at
      the bottom, and one row with no top gold at all). Requiring all four sides
      found zero plates. The plate is square, so a missing edge is now derived
      from the width.

      2. Row 4's cell is 51px where its neighbours are 52, so under a leftward
      shift its right frame falls outside the box entirely. The scan now looks a
      bounded distance outside the box, but only after failing inside — reaching
      outward first would cross into the neighbouring cell's frame.


      **Migration added beyond the ticket's scope, and it was necessary:** every
      stored hash is stale. Without a version drop, the user's 57 dead bindings
      would sit at the 64 `MaxUnboundBindings` cap and no new sprite could ever
      be stored — the new build would look dead on arrival. The user layer now
      carries `hashVersion` and drops older generations on load, keeping weights
      and rune-level carried flags.


      **Residual risk:** validated on one fixture, six gilded cells, all from
      one resolution. The plate scan's coverage thresholds are calibrated on
      that capture. A second resolution or a panel with a differently-lit frame
      could read differently — that is what RUNE-3's captures are for.


      **Follow-on worth doing:** auto-matching sprites against the poe2db
      reference icons was rejected on evidence gathered with the unstable hash.
      That rejection no longer holds and should be re-tested; if it works,
      manual naming disappears entirely.
    date: '2026-09-13T09:44:40.287Z'
    completionComment: true
    id: c-2026-09-13t09-44-40-287z
  - type: status_change
    from: Todo
    to: Ready
    user: Agent
    date: '2026-09-13T09:44:40.287Z'
  - type: comment
    user: Agent
    comment: >-
      Merged via PR #11 (confirmed MERGED at 2026-09-13T09:45:00Z). 900 tests
      passing, deployed to the installed copy.


      Crop drift went from 26 bits at ±1px to 0 bits across the whole ±2px grid,
      against an 8-bit bar. Stale bindings are dropped automatically via the new
      `hashVersion`, so the user does not have to clear the 57 by hand.
    completionComment: true
    date: '2026-09-13T09:44:57.146Z'
    completion:
      changedFiles:
        - src/OCR/RuneIconFingerprinter.cs
        - src/Runes/RuneCatalog.cs
        - src/Runes/RuneCatalogModels.cs
        - tests/src/OCR/RuneCropStabilityDiagnostics.cs
        - tests/src/Runes/RuneCatalogTests.cs
      decisions:
        - >-
          Anchor identity crop to the cell's own gold-to-plate edge instead of
          the row lattice.
        - >-
          Derive a missing frame edge from the width, since the row extent clips
          the top frame.
        - >-
          Scan outside-in, and only look outside the box after failing inside,
          to avoid a yellow glyph and the neighbouring cell respectively.
        - >-
          Version the identity hash and drop older-generation bindings on load,
          or the 64-binding cap would block all new sprites.
      residualRisk: >-
        Calibrated on one fixture, six gilded cells, one resolution. A second
        resolution or differently-lit frame may read differently — RUNE-3's
        captures cover this.
      docsUpdated: false
    id: c-2026-09-13t09-44-57-146z
  - type: status_change
    from: Ready
    to: Done
    user: Agent
    date: '2026-09-13T09:44:57.363Z'
needsAction: null
baselineCommit: 2cac205663df8022ab52846f4965911026063703
implementationLink: 'https://github.com/guybnd/RuneshapePriceChecker/pull/11'
swimlane: null
diffSummary:
  - file: src/OCR/RuneIconFingerprinter.cs
    additions: 179
    deletions: 1
  - file: src/Runes/RuneCatalog.cs
    additions: 25
    deletions: 0
  - file: src/Runes/RuneCatalogModels.cs
    additions: 8
    deletions: 0
  - file: tests/src/OCR/RuneCropStabilityDiagnostics.cs
    additions: 50
    deletions: 21
  - file: tests/src/Runes/RuneCatalogTests.cs
    additions: 47
    deletions: 0
---
## The report

User's Rune Library shows **57 unbound sprites**. The game has 34 runes, and only a handful appear per panel. The same rune is being stored many times over.

## Measured cause

`AssignLatticeGlyphBounds` does not anchor the identity crop to the rune. It derives it from the *surroundings*:

```
right = prevPlain.Bounds.Right + pitch * slotsAway   // neighbour's border edge + integer-divided median pitch
glyph = new Rectangle(right - rowHeight, rowTop, rowHeight, rowHeight)   // rowTop/rowHeight from OCR row detection
```

Every input there can land a pixel off between frames: the neighbour's detected border, the integer division in the pitch median, and the OCR text row's top/height.

`RuneCropStabilityDiagnostics.GlyphHashUnderSubPixelCropDrift` measures what that costs on `2560x1440/1 Raw.png` (6 gilded cells), shifting the crop and re-hashing:

```
worst-case Hamming distance from the unshifted hash, per crop shift:
  dy=-2:   39   28   18   21   37
  dy=-1:   39   26   14   15   35
  dy= 0:   37   19    .   17   38
  dy= 1:   34   19   11   19   38
  dy= 2:   33   24   20   27   39

worst within +/-1px: 26 bits
worst within +/-2px: 39 bits
```

`MatchHammingThreshold` is 8, and the fixture's own `DifferentRuneMinHamming` is 16. **One pixel of drift exceeds both** — the same rune re-hashes as what the matcher reads as a different rune. 57 sprites for 34 runes follows directly.

This also explains why matching in-game sprites against the poe2db reference icons failed (margins were noise, chamfer picked "wisdom" for 4/6 cells): the hash is dominated by crop alignment, not glyph shape.

## The fix (user's suggestion, and it is the right one)

> "can we not crop the image to the black square and enlarge it... its always the same size and shape and color"

Anchor the crop to the cell's own dark inner plate. The gold frame to dark plate transition is a high-contrast edge belonging to *this* cell, so the crop stops depending on neighbours, on pitch arithmetic, and on OCR row geometry.

Sketch: within `cell.Bounds`, walk outward from the centre until the gold frame is met on each side; that box is the plate. Fall back to the current lattice box when no clean plate is found, so a bad cell degrades rather than throws.

## Validation

Re-run the diagnostic against the new crop. The bar is worst-case within +/-2px dropping under `MatchHammingThreshold` (8). The existing fixture assertions must keep holding: rows 1&3 and rows 2&4 within 8 bits of each other, distinct runes 16+ apart.

## Follow-on, once this lands

- The user's stored 57 sprites are junk and should be cleared (the "Forget all" button from RUNE-8 does it).
- Re-test auto-matching against the reference icons. It was rejected on evidence gathered with an unstable hash, so that rejection is no longer sound.

## Acceptance

- [ ] Crop anchored to the cell's own plate, with a documented fallback.
- [ ] Worst-case drift within +/-2px under 8 bits, measured by the diagnostic.
- [ ] Existing fixture same-rune/different-rune assertions still pass.
- [ ] Diagnostic kept as a regression test rather than deleted.
