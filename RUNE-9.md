---
id: RUNE-9
title: 'Anchor the glyph crop to the rune''s own dark plate, not to neighbouring cells'
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
