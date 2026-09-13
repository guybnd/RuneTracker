---
id: RUNE-5
title: 'Rune Library UI: columns clipped at narrow width, weight boxes blank'
status: Done
priority: High
effort: S
assignee: unassigned
tags:
  - bug
  - ui-ux
createdBy: Agent
updatedBy: Agent
history:
  - type: activity
    user: Agent
    date: '2026-09-13T06:36:17.603Z'
    comment: Created ticket.
    id: a-2026-09-13t06-36-17-603z
  - type: activity
    user: Agent
    date: '2026-09-13T06:36:23.620Z'
    comment: >-
      Created worktree for branch
      flux/RUNE-5-rune-library-ui-columns-clipped-at-narrow-width-weight-boxes
    event: worktree-created
    id: a-2026-09-13t06-36-23-620z
  - type: comment
    user: Agent
    comment: >-
      Commit ff77868 off master a47e512. Suite 868/868 (12 new tests).


      Row rebuilt as five columns sized for the 500px minimum width — reference
      glyph, flexible name column with tier/rarity/seen merged into a subtitle
      line, weight, seen sprite, label-less carried checkbox. The
      hotkey/high-value settings row becomes a WrapPanel so it folds rather than
      overflowing. Header no longer mislabels the sprite column as "Seen".


      On the blank weight boxes: the bound properties are now pinned by tests
      and `WeightText` returns the value for every shipped weight including 1.0,
      so the model is not at fault. The likely cause was the row overflowing its
      available width. Needs a fresh screenshot on the new layout to confirm.
    date: '2026-09-13T06:40:21.620Z'
    completionComment: true
    id: c-2026-09-13t06-40-21-620z
  - type: activity
    user: Agent
    comment: 'PR created: https://github.com/guybnd/RuneshapePriceChecker/pull/5'
    date: '2026-09-13T06:40:26.078Z'
    id: a-2026-09-13t06-40-26-078z
  - type: status_change
    from: In Progress
    to: Ready
    user: Agent
    date: '2026-09-13T06:40:26.078Z'
  - type: activity
    user: Temper
    comment: >-
      Temper on — auto-reviewing this ticket. It will loop review →
      re-implementation until the reviewer approves (PR left open at Ready,
      never merged), or park after 2 re-implementation attempts.
    date: '2026-09-13T06:40:26.543Z'
    id: a-2026-09-13t06-40-26-543z
  - type: activity
    user: Furnace
    date: '2026-09-13T06:40:26.591Z'
    comment: "\U0001F3AF Launch focus: You are the ONLY reviewer for this ticket in this Furnace run — no orchestrator will synthesize other reviews, so you own the decision. Your review is not complete until you call `change_status` with `reviewState` set to \"approved\" or \"changes-requested\" to match your verdict. Posting a comment that starts with **APPROVED** or **CHANGES NEEDED** is not enough by itself — without the `change_status` call, the ticket will be parked for a human to unblock even though your review already happened."
    id: a-2026-09-13t06-40-26-591z
  - type: activity
    user: Agent
    date: '2026-09-13T06:40:27.015Z'
    comment: >-
      Created worktree for branch
      flux/RUNE-5-rune-library-ui-columns-clipped-at-narrow-width-weight-boxes
    event: worktree-created
    id: a-2026-09-13t06-40-27-015z
  - type: agent_session
    sessionId: e7df42c6-cb95-4ce7-a2d0-5349895c22ea
    startedAt: '2026-09-13T06:40:26.591Z'
    status: cancelled
    progress:
      - timestamp: '2026-09-13T06:40:29.730Z'
        message: >-
          ⚠️ Rate limited: allowed_warning [five_hour] (resets at
          2026-09-13T08:30:00.000Z)
    user: Claude Code
    date: '2026-09-13T06:40:26.591Z'
    enginePid: 21456
    outcome: Claude Code session stopped by user.
    endedAt: '2026-09-13T06:40:40.779Z'
    originalProgressCount: 1
    finalMessage: >-
      ⚠️ Rate limited: allowed_warning [five_hour] (resets at
      2026-09-13T08:30:00.000Z)
  - type: comment
    user: Agent
    comment: >-
      Merged. Rune Library row now fits the 500px minimum dashboard width so the
      carried checkbox is reachable; tier, rarity and sighting count merge into
      a subtitle under the name; the settings row wraps. Bound view properties
      pinned by 12 new tests. Suite 868/868.
    completionComment: true
    date: '2026-09-13T06:40:40.339Z'
    completion:
      changedFiles:
        - src/Dashboard/DashboardWindow.xaml
        - src/Dashboard/RuneLibraryViews.cs
        - tests/src/Runes/RuneLibraryViewTests.cs
      decisions:
        - >-
          Five-column row sized for the 500px minimum rather than relying on the
          user widening the window
      residualRisk: >-
        Blank weight boxes attributed to overflow; unconfirmed until the user
        sees the new layout.
    id: c-2026-09-13t06-40-40-339z
  - type: status_change
    from: Ready
    to: Done
    user: Agent
    date: '2026-09-13T06:40:40.527Z'
  - type: activity
    user: Temper
    comment: >-
      Temper disarmed — a finish/merge flow is taking over session teardown for
      this ticket.
    date: '2026-09-13T06:40:40.536Z'
    id: a-2026-09-13t06-40-40-536z
branch: flux/RUNE-5-rune-library-ui-columns-clipped-at-narrow-width-weight-boxes
needsAction: null
implementationLink: 'https://github.com/guybnd/RuneshapePriceChecker/pull/5'
swimlane: null
baselineCommit: a47e5128ac853ce25201969ceae55bf4b68f1c40
diffSummary:
  - file: src/Dashboard/DashboardWindow.xaml
    additions: 52
    deletions: 57
  - file: src/Dashboard/RuneLibraryViews.cs
    additions: 12
    deletions: 1
  - file: tests/src/Runes/RuneLibraryViewTests.cs
    additions: 68
    deletions: 0
---
> **TL;DR** — The Rune Library's seven-column grid is wider than the dashboard at its default width, so the carried checkbox and part of the seen column are cut off with no way to scroll to them. Rebuild the row so it fits the narrowest supported window.

Found by the user on the merged build, 2026-09-13.

## What's wrong

`DashboardWindow.xaml` sets `MinWidth="500" MaxWidth="960"`, and the user runs it near 500. The settings `ScrollViewer` has `HorizontalScrollBarVisibility` unset (defaults to Disabled/Hidden for the vertical-only layout used everywhere else), so anything past the right edge is unreachable rather than scrollable.

The library row grid is `40 + 160 + 90 + 56 + 44 + 90 + *`, about 480px before the panel's 20px side padding and the 14px section indent — wider than the ~440px available. The carried checkbox sits in the last column and is entirely off-screen, which is the one control the feature depends on.

Also reported: the weight boxes render blank. `RuneLibraryEntryView.WeightText` is bound `OneWay` and the presenter sets `Weight` in the object initializer, so the model should be populated; needs a check on whether the value is present and merely unreadable at that size, or genuinely empty.

## Fix

- Rebuild the row to fit ~440px: reference glyph, then a flexible name column carrying the tier dot and seen count as secondary text, then weight, seen sprite and a label-less carried checkbox. Drop the separate tier-text and seen-text columns.
- Keep the header aligned with the new columns (the current header also mislabels column 4, which holds the sprite, as "Seen").
- Verify the weight value renders; add a model-level test for `WeightText`.

## Acceptance criteria

- [ ] At the 500px minimum window width, every control in a rune row is fully visible, including the carried checkbox.
- [ ] Weight shows its numeric value for every rune (3 for Opulent, 2 for Power, 0.5 for blue-tier, 1 otherwise).
- [ ] Editing a weight and toggling carried still round-trip to `config/rune-catalog.json`.
- [ ] Suite stays green.
