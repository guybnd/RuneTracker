---
id: RUNE-18
title: Mark a socketed rune from its tooltip with the mark hotkey
status: In Progress
priority: Medium
effort: M
assignee: unassigned
tags:
  - runes
createdBy: Agent
updatedBy: Agent
history:
  - type: activity
    user: Agent
    date: '2026-09-13T12:52:07.564Z'
    comment: Created ticket.
    id: a-2026-09-13t12-52-07-564z
  - type: activity
    user: Agent
    date: '2026-09-13T12:52:24.954Z'
    comment: >-
      Created worktree for branch
      flux/RUNE-18-mark-a-socketed-rune-from-its-tooltip-with-the-mark-hotkey
    event: worktree-created
    id: a-2026-09-13t12-52-24-954z
branch: flux/RUNE-18-mark-a-socketed-rune-from-its-tooltip-with-the-mark-hotkey
---
## Problem

Once runes are socketed into the remnant, the only way to record them as carried is to reopen the Combinations panel and Alt+V / right-click each rune again. The socketed rune's tooltip already shows its name in plain text ("Power Rune" above "Runes gain:").

## Plan

- When the mark hotkey (Alt+V) is pressed and the cursor is **not** over a marked panel rune, fall through to a tooltip read: capture a band above/around the cursor from the game window, OCR it with the existing Windows OCR engine, take the line preceding "Runes gain:", fuzzy-match against the 34 catalog display names, and toggle that rune's carried flag.
- Keyboard only. The right-click hook is not extended (right-clicking a socket does things in game).
- Show a short banner with the result so the user knows it landed.
- OCR runs off the hotkey window thread.
- Unit tests for the tooltip text → rune-name matcher with realistic OCR noise.

## Caveats

- Marks by rune id. If the panel sprite for that rune is still unbound in the catalog the grey slash will not appear on the panel until it is bound.
- Same capture constraints as the rest of the tool (borderless windowed, English OCR).
