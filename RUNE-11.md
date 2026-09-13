---
id: RUNE-11
title: 'Rune magazine: hotkey to mark the hovered rune as taken'
status: Todo
priority: High
effort: M
assignee: unassigned
tags:
  - runes
  - overlay
  - ux
createdBy: Agent
updatedBy: Agent
history:
  - type: activity
    user: Agent
    date: '2026-09-13T10:07:40.694Z'
    comment: Created ticket.
---
User's design, in their words:

> "when the panel is open and there are markings, the player can use a shortcut like alt+v while hovering inside the relevant rune to mark as 'selected' and this will add it to his rune magazine, which means it can show in the future remnants that this one is already taken."

## Why this matters

This is the missing half of the tracker. The carried set is what makes the grey "already taken" marker mean anything, and today the only way to fill it is to alt-tab to the dashboard and tick a checkbox on one of 34 rows — unusable mid-run. When the user asked "how does it know that we picked it?", the honest answer was that it does not.

Every piece needed already exists:

- `RuneKey.CellBounds` carries each rune's drawn rectangle in capture-region coordinates (added for the marker overlay).
- `GlobalHotkeyService` already registers a system-wide hotkey on a message-only window.
- `RuneCatalog.SetCarried(id, bool)` already accepts either a rune id or a binding id, and `Bind` migrates the flag when an unbound sprite is later named.
- The grey/slashed carried marker is already painted by `RuneMarkerPainter`.

## Sketch

1. Second hotkey in `RunesOptions` (`MarkCarriedHotkey`, default `Alt+V`), registered alongside the existing reset hotkey.
2. `LeaguePricingWorker` retains the last rendered `RuneScoreSheet` plus the capture-region origin.
3. On press: `GetCursorPos` → subtract the capture origin → hit-test against each key's `CellBounds` → resolve to `CarriedIdFor` → toggle.
4. **Toggle, not one-way.** A mis-press on the wrong cell must be undoable without opening the dashboard.
5. No hit → do nothing, and log at debug. Never guess at the nearest cell.

## Decisions to settle before building

- **Feedback on press.** The marker flips to grey on the next render, which may be up to a second later at the current OCR cadence. That may be enough, or it may need an immediate repaint of that one cell.
- **Unbound runes.** Carrying works on a binding id, but the library only exposes the carried checkbox on the 34 named rows — so a rune marked by hotkey while still unnamed would be invisible in the UI and unclearable. The unbound card needs the carried state too (this gap predates the ticket).
- **Run boundaries.** The magazine has to empty when a new run starts. Today that is the manual reset hotkey. Automatic run detection stays deferred.

## Acceptance

- [ ] Configurable hotkey, default Alt+V, registered without clashing with the reset hotkey.
- [ ] Pressing it over a marked rune toggles it carried; pressing over anything else does nothing.
- [ ] Hit-testing is unit-tested against cell bounds — cursor mapping, inside/outside, and overlapping edges.
- [ ] Carried state is visible and clearable for unbound sprites, not just named runes.
