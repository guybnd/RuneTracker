---
id: RUNE-20
title: Toast near the cursor when the mark hotkey acts ("Power Rune saved")
status: Done
priority: Medium
effort: S
assignee: unassigned
tags:
  - runes
createdBy: Agent
updatedBy: Agent
history:
  - type: activity
    user: Agent
    date: '2026-09-13T13:07:20.987Z'
    comment: Created ticket.
    id: a-2026-09-13t13-07-20-987z
  - type: activity
    user: Agent
    date: '2026-09-13T13:07:49.556Z'
    comment: >-
      Created worktree for branch
      flux/RUNE-20-toast-near-the-cursor-when-the-mark-hotkey-acts-power-rune-s
    event: worktree-created
    id: a-2026-09-13t13-07-49-556z
  - type: comment
    user: Agent
    comment: >-
      Implemented on flux/RUNE-20 (commit 5bd5897), pushed.


      - `src/Overlay/RuneToastOverlay.cs` (new): `RuneToast` (pure words +
      rise/fade curve: 1.5 s, 22 px rise ease-out, opaque for the first 35% then
      eased fade; green saved / grey dismissed / orange miss) and
      `RuneToastOverlay` (click-through, no-activate OverlayFormBase form on its
      own STA thread via OverlayFormRunner; rounded dark pill with coloured text
      centred above the cursor, clamped to the screen; respects
      AllOverlaysDisabled and OverlayScale).

      - `RuneMagazine.ToggleAtCursor(out runeName)` and
      `RuneTooltipMarkService.TryToggleFromTooltip(out runeName)` overloads so
      the toast can name the rune; parameterless forms kept.

      - `GlobalHotkeyService` toasts after the panel toggle or the tooltip read;
      `RuneMouseMarkService` toasts on a swallowed right-click. No toast when
      the game is not in front or a read is already running.

      - Tests: `RuneToastTests` (words for every outcome, curve endpoints,
      monotonic rise/fade, clamping). Suite 994/994 green.
    date: '2026-09-13T13:11:39.157Z'
    completionComment: true
    id: c-2026-09-13t13-11-39-157z
  - type: activity
    user: Agent
    comment: 'PR created: https://github.com/guybnd/RuneshapePriceChecker/pull/26'
    date: '2026-09-13T13:11:43.972Z'
    id: a-2026-09-13t13-11-43-972z
  - type: status_change
    from: In Progress
    to: Ready
    user: Agent
    date: '2026-09-13T13:11:43.972Z'
  - type: activity
    user: Temper
    comment: >-
      Temper on — auto-reviewing this ticket. It will loop review →
      re-implementation until the reviewer approves (PR left open at Ready,
      never merged), or park after 2 re-implementation attempts.
    date: '2026-09-13T13:11:44.646Z'
    id: a-2026-09-13t13-11-44-646z
  - type: activity
    user: Furnace
    date: '2026-09-13T13:11:44.719Z'
    comment: "\U0001F3AF Launch focus: You are the ONLY reviewer for this ticket in this Furnace run — no orchestrator will synthesize other reviews, so you own the decision. Your review is not complete until you call `change_status` with `reviewState` set to \"approved\" or \"changes-requested\" to match your verdict. Posting a comment that starts with **APPROVED** or **CHANGES NEEDED** is not enough by itself — without the `change_status` call, the ticket will be parked for a human to unblock even though your review already happened."
    id: a-2026-09-13t13-11-44-719z
  - type: activity
    user: Agent
    date: '2026-09-13T13:11:45.298Z'
    comment: >-
      Created worktree for branch
      flux/RUNE-20-toast-near-the-cursor-when-the-mark-hotkey-acts-power-rune-s
    event: worktree-created
    id: a-2026-09-13t13-11-45-298z
  - type: agent_session
    sessionId: b6f517be-2440-4ef2-9d19-5996acc1f78a
    startedAt: '2026-09-13T13:11:44.719Z'
    status: cancelled
    progress: []
    user: Claude Code
    date: '2026-09-13T13:11:44.719Z'
    enginePid: 21456
    outcome: Claude Code session stopped by user.
    endedAt: '2026-09-13T13:11:56.850Z'
    originalProgressCount: 0
  - type: comment
    user: Agent
    comment: >-
      Toast above the cursor on every mark action: "<Rune> saved" (green),
      "<Rune> dismissed" (grey), "No rune here" / "Can't read the tooltip"
      (orange); rises 22 px and fades over 1.5 s, click-through and never
      activated. Fired from the hotkey (panel and tooltip halves) and the
      right-click hook. Both toggles now expose the rune name. New
      RuneToastTests; suite 994/994 green.
    completionComment: true
    date: '2026-09-13T13:11:56.342Z'
    id: c-2026-09-13t13-11-56-342z
  - type: status_change
    from: Ready
    to: Done
    user: Agent
    date: '2026-09-13T13:11:56.556Z'
  - type: activity
    user: Temper
    comment: >-
      Temper disarmed — a finish/merge flow is taking over session teardown for
      this ticket.
    date: '2026-09-13T13:11:56.573Z'
    id: a-2026-09-13t13-11-56-573z
branch: flux/RUNE-20-toast-near-the-cursor-when-the-mark-hotkey-acts-power-rune-s
needsAction: null
implementationLink: 'https://github.com/guybnd/RuneshapePriceChecker/pull/26'
swimlane: null
baselineCommit: 440ba15406a52509e89b03c31ea732cf3d0a492b
diffSummary:
  - file: src/App/GlobalHotkeyService.cs
    additions: 0
    deletions: 0
  - file: src/App/RuneMouseMarkService.cs
    additions: 11
    deletions: 2
  - file: src/App/RuneTooltipMarkService.cs
    additions: 10
    deletions: 3
  - file: src/Overlay/RuneToastOverlay.cs
    additions: 276
    deletions: 0
  - file: src/Program.cs
    additions: 1
    deletions: 0
  - file: src/Runes/RuneMagazine.cs
    additions: 7
    deletions: 1
  - file: tests/src/Runes/RuneToastTests.cs
    additions: 84
    deletions: 0
---
## Problem

Alt+V (panel cell or socket tooltip) and the right-click mark act silently apart from the left-edge magazine strip changing, so the user cannot tell whether a press landed (RUNE-18 feedback: "I don't think the hotkey is working at all" while the log showed every press succeeding).

## Plan

- New `RuneToastOverlay`: a small click-through, no-activate layered window that appears just above the cursor with text such as "Power Rune saved", "Power Rune dismissed", "No rune here", rises ~20 px and fades out over about 1.5 s.
- Fired from every mark path: panel toggle (hotkey and mouse hook), tooltip toggle, and misses.
- Colour cue: green for saved, grey for dismissed, dim/orange for a miss.
- Same overlay thread pattern as the other overlays (OverlayFormRunner), respects AllOverlaysDisabled.
