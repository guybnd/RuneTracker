---
id: RUNE-25
title: >-
  Snappier panel and window reaction: foreground event hook, 50 ms anchor poll,
  1-frame open threshold
status: In Progress
priority: Medium
effort: M
assignee: unassigned
tags:
  - perf
  - overlay
  - ocr
createdBy: Agent
updatedBy: Agent
history:
  - type: activity
    user: Agent
    date: '2026-09-14T03:23:32.480Z'
    comment: Created ticket.
    id: a-2026-09-14t03-23-32-480z
  - type: activity
    user: Agent
    date: '2026-09-14T03:23:47.284Z'
    comment: >-
      Created worktree for branch
      flux/RUNE-25-snappier-panel-and-window-reaction-foreground-event-hook-50-
    event: worktree-created
    id: a-2026-09-14t03-23-47-284z
  - type: activity
    user: Agent
    date: '2026-09-14T03:29:24.350Z'
    comment: >-
      Prototype committed on the ticket branch (c6788fe) and published to the
      installed copy (%LOCALAPPDATA%\RuneshapePriceChecker, exe 2026-09-14
      13:29). Changes: Poe2WindowEventHook (SetWinEventHook for
      foreground/minimise/location, own message-loop thread),
      Poe2WindowResolutionService refreshes on events and exposes
      WaitForWindowStateChangeAsync (default interface impl keeps test fakes
      compiling), worker waits on that signal instead of Task.Delay(200) and
      polls the anchor at 50 ms while the panel is closed, LeaguePanelDetector
      OpenFrames=1 / CloseFrames=3. Targeted xunit run: 40/40 pass. Awaiting
      in-game test by Guy.
    id: a-2026-09-14t03-29-24-350z
branch: flux/RUNE-25-snappier-panel-and-window-reaction-foreground-event-hook-50-
---
## Goal
Cut the lag between the game/panel changing state and the overlay reacting.

## Changes
1. **Event-driven foreground tracking** — replace the 1 s poll in `Poe2WindowResolutionService` with `SetWinEventHook` (`EVENT_SYSTEM_FOREGROUND`, `EVENT_OBJECT_LOCATIONCHANGE` filtered to the PoE2 process) on a dedicated message-loop thread. The poll stays as a 1 s fallback/heartbeat. The worker waits on a signal instead of `Task.Delay(200)` when the game is in the background.
2. **50 ms anchor poll while the panel is closed** — the anchor check (~40K px `CopyFromScreen`) is decoupled from `ScanIntervalMs`; full OCR keeps the configured cadence.
3. **Open threshold 1 frame, close stays 3** in `LeaguePanelDetector`. A false open costs one empty OCR pass; a false close would flicker the overlay.

## Expected
Alt-tab in/out reacts in under ~50 ms (was up to 1.2 s). Panel open ~150 ms + first OCR (was ~300 ms + OCR). Panel close ~150 ms (was ~350 ms).

## Cost
Idle: near zero (hook is push-based). Closed panel: 20 screen reads/s instead of 10. Open panel: unchanged.
