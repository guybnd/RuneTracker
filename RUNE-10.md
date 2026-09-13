---
id: RUNE-10
title: 'Make the naming UI readable: plate sprites at 64px, resizable window'
status: In Progress
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
