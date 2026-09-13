---
id: RUNE-10
title: 'Make the naming UI readable: plate sprites at 64px, resizable window'
status: Done
priority: High
effort: S
assignee: unassigned
tags:
  - ux
  - runes
createdBy: Agent
updatedBy: Agent
history:
  - type: activity
    user: Agent
    date: '2026-09-13T09:56:11.510Z'
    comment: Created ticket.
    id: a-2026-09-13t09-56-11-510z
  - type: comment
    user: Agent
    comment: >-
      Implemented and merged as PR #12
      (https://github.com/guybnd/RuneshapePriceChecker/pull/12). Release gate:
      901 passed, 0 failed. Deployed to the installed copy at 19:55.


      Measured: gold is now 0.0-1.7% of each stored sprite (was the whole
      frame). Sprite stored at 64px.


      Root-caused an older complaint along the way: the window has never been
      resizable because `WM_NCHITTEST` returned `HTCLIENT` for every point, so
      `ResizeMode="CanResize"` had no effect and no edge could be grabbed.
      Height was separately pinned by `MinHeight == MaxHeight == 732`. Both
      fixed.


      **Residual risk:** the WPF surface cannot be exercised in the test host.
      The sprite crop and size are pinned by a fixture test, but the resize
      hit-test and the enlarged card are verified only by reading the code. If
      dragging an edge does nothing, or the window can be dragged to an unusable
      size, that is where to look.
    date: '2026-09-13T09:57:39.090Z'
    completionComment: true
    id: c-2026-09-13t09-57-39-090z
  - type: status_change
    from: In Progress
    to: Ready
    user: Agent
    date: '2026-09-13T09:57:39.090Z'
  - type: comment
    user: Agent
    comment: >-
      Merged as PR #12, 901 tests passing, deployed to the installed copy.
      Sprite is now the plate at 64px (gold 0.0-1.7%), the naming card is
      roughly 2.3x larger, and the window resizes for the first time. WPF
      surface still needs one confirmation by eye.
    completionComment: true
    date: '2026-09-13T09:57:48.812Z'
    completion:
      changedFiles:
        - src/Contracts/RuneKey.cs
        - src/OCR/RuneIconFingerprinter.cs
        - src/Runes/RuneCatalog.cs
        - src/Dashboard/DashboardWindow.xaml
        - src/Dashboard/DashboardWindow.xaml.cs
        - tests/src/OCR/RuneCropStabilityDiagnostics.cs
        - tests/src/Runes/RuneCatalogTests.cs
        - tests/src/Runes/RuneMarkerPolishTests.cs
        - tests/src/Runes/RuneRowScorerTests.cs
      decisions:
        - >-
          Display sprite uses the identity plate box, not the cell, so what the
          user reads is what the matcher sees.
        - >-
          Display at 64px while identity stays 32px — dHash reduces to 8x8
          either way.
        - >-
          Implement a real WM_NCHITTEST edge test rather than raising the pinned
          height, since ResizeMode was inert.
        - Bump the sprite generation so old framed 32px sprites are re-learned.
      residualRisk: >-
        WPF surface unverified in tests: resize hit-test and enlarged naming
        card confirmed only by code reading.
      docsUpdated: false
    id: c-2026-09-13t09-57-48-812z
  - type: status_change
    from: Ready
    to: Done
    user: Agent
    date: '2026-09-13T09:57:49.061Z'
needsAction: null
baselineCommit: 1d9d64a4d3947ce82b14fe4fceec191a41326dd7
implementationLink: 'https://github.com/guybnd/RuneshapePriceChecker/pull/12'
swimlane: null
diffSummary:
  - file: src/Contracts/RuneKey.cs
    additions: 18
    deletions: 5
  - file: src/Dashboard/DashboardWindow.xaml
    additions: 26
    deletions: 17
  - file: src/Dashboard/DashboardWindow.xaml.cs
    additions: 48
    deletions: 2
  - file: src/OCR/RuneIconFingerprinter.cs
    additions: 16
    deletions: 4
  - file: src/Runes/RuneCatalog.cs
    additions: 10
    deletions: 7
  - file: tests/src/OCR/RuneCropStabilityDiagnostics.cs
    additions: 41
    deletions: 0
  - file: tests/src/Runes/RuneCatalogTests.cs
    additions: 1
    deletions: 1
  - file: tests/src/Runes/RuneMarkerPolishTests.cs
    additions: 1
    deletions: 1
  - file: tests/src/Runes/RuneRowScorerTests.cs
    additions: 1
    deletions: 1
---
User, on a sprite thumbnail showing a purple glyph inside its gold frame: *"do you see how it matched also some of the background?"* and *"we need to make the assignment UI more workable, its too small for me to see."*

## What they were actually looking at

Not a matching failure. RUNE-9 fixed the *identity* crop; the *display* sprite was untouched and still came from `NormalizeTo32x32(rgb, …, cell.Bounds)` with the default `marginRatio: 0.08` — the whole cell **plus 8% beyond it**. So the thumbnail was frame, parchment and a slice of whatever sat outside the cell, with the glyph occupying maybe half the pixels and off-centre.

Measured after the fix, gold is 0.0-1.7% of each sprite — the frame is gone.

## Changes

- Display sprite is the plate crop, same box the identity hash uses, at 64px instead of 32px. Identity stays at 32px: dHash reduces to 8x8 regardless, so the extra pixels only ever mattered to the human reading the glyph.
- `RuneKey.Sprite32Rgb` → `SpriteRgb` with `RuneKey.SpriteSize`, since the name no longer describes it.
- `NormalizeTo32x32` delegates to a size-parametric `NormalizeTo`.
- Unbound card: sprite 48px → 112px on a dark plate, picker 190px → 260px with 44px reference glyphs.
- **Window is now actually resizable.** `WM_NCHITTEST` returned `HTCLIENT` unconditionally, so despite `ResizeMode="CanResize"` no edge could ever be grabbed — this is the "window is not resizable" complaint from earlier, root-caused. Added a proper edge/corner hit test. Height was also pinned by `MinHeight == MaxHeight == 732`; now 600-1600, width max 960 → 1600.
- `CurrentHashVersion` 2 → 3, so the old small framed sprites are dropped and re-learned rather than sitting alongside the new ones.

## Acceptance

- [x] Sprite crop excludes the frame (gold < 15% of sprite; measured 0.0-1.7%).
- [x] Sprite stored at 64px.
- [x] Window resizable by dragging any edge or corner.
- [ ] Confirmed by eye in the running app — WPF cannot be exercised in the test host.
