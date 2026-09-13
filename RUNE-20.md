---
id: RUNE-20
title: Toast near the cursor when the mark hotkey acts ("Power Rune saved")
status: In Progress
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
branch: flux/RUNE-20-toast-near-the-cursor-when-the-mark-hotkey-acts-power-rune-s
---
## Problem

Alt+V (panel cell or socket tooltip) and the right-click mark act silently apart from the left-edge magazine strip changing, so the user cannot tell whether a press landed (RUNE-18 feedback: "I don't think the hotkey is working at all" while the log showed every press succeeding).

## Plan

- New `RuneToastOverlay`: a small click-through, no-activate layered window that appears just above the cursor with text such as "Power Rune saved", "Power Rune dismissed", "No rune here", rises ~20 px and fades out over about 1.5 s.
- Fired from every mark path: panel toggle (hotkey and mouse hook), tooltip toggle, and misses.
- Colour cue: green for saved, grey for dismissed, dim/orange for a miss.
- Same overlay thread pattern as the other overlays (OverlayFormRunner), respects AllOverlaysDisabled.
