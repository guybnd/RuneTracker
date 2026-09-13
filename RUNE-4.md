---
id: RUNE-4
title: 'Rune marker polish: meaningless top-pick star, hotkey warning spam'
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
    date: '2026-09-13T06:18:56.581Z'
    comment: Created ticket.
    id: a-2026-09-13t06-18-56-581z
  - type: activity
    user: Agent
    date: '2026-09-13T06:19:57.809Z'
    comment: >-
      Created worktree for branch
      flux/RUNE-4-rune-marker-polish-meaningless-top-pick-star-hotkey-warning-
    event: worktree-created
    id: a-2026-09-13t06-19-57-809z
  - type: comment
    user: Agent
    comment: >-
      Three fixes on `flux/RUNE-4-...`, commit a0125e8, branched from master
      ce4bedb. Suite 856/856 (4 new tests, 1 updated).


      1. **Hover wash** — the game tints a whole combination row gold under the
      cursor, which lifted the border-ring metric for every cell in that row and
      marked them all gilded. A gilded frame must now exceed a band sampled just
      outside the cell by `MinGoldContrast` (0.15). Calibrated on the real
      fixture: genuine gilded cells sit 0.20–0.30 above their surroundings,
      plain cells at most 0.04, and an ambient wash lifts both together so the
      difference collapses. A synthetic test renders the same cell on parchment
      and on a gold-washed row and asserts the two outcomes.

      2. **Top-pick star** — was awarded to every new rune tied at the maximum
      weight, so with nothing bound (all sprites at the same unknown weight)
      every rune on screen was starred, which the user saw. Now requires a
      strictly lower runner-up.

      3. **Hotkey noise** — `IOptionsMonitor.OnChange` fires several times per
      settings write and each fired a re-registration, logging the same failure
      eight times. Re-registers only on a real change; one warning per distinct
      hotkey; Win32 1409 now reads "another application already uses it" and
      names the fallback button.


      Note on the user's machine: `Ctrl+Alt+R` is genuinely taken by another
      application, so the hotkey will not work there until they choose a
      different combination in Settings. The Reset carried runes button is
      unaffected.
    date: '2026-09-13T06:24:36.704Z'
    completionComment: true
    id: c-2026-09-13t06-24-36-704z
  - type: activity
    user: Agent
    comment: 'PR created: https://github.com/guybnd/RuneshapePriceChecker/pull/4'
    date: '2026-09-13T06:24:41.648Z'
    id: a-2026-09-13t06-24-41-648z
  - type: status_change
    from: In Progress
    to: Ready
    user: Agent
    date: '2026-09-13T06:24:41.648Z'
  - type: activity
    user: Temper
    comment: >-
      Temper on — auto-reviewing this ticket. It will loop review →
      re-implementation until the reviewer approves (PR left open at Ready,
      never merged), or park after 2 re-implementation attempts.
    date: '2026-09-13T06:24:42.214Z'
    id: a-2026-09-13t06-24-42-214z
  - type: activity
    user: Furnace
    date: '2026-09-13T06:24:42.296Z'
    comment: "\U0001F3AF Launch focus: You are the ONLY reviewer for this ticket in this Furnace run — no orchestrator will synthesize other reviews, so you own the decision. Your review is not complete until you call `change_status` with `reviewState` set to \"approved\" or \"changes-requested\" to match your verdict. Posting a comment that starts with **APPROVED** or **CHANGES NEEDED** is not enough by itself — without the `change_status` call, the ticket will be parked for a human to unblock even though your review already happened."
    id: a-2026-09-13t06-24-42-296z
  - type: activity
    user: Agent
    date: '2026-09-13T06:24:42.859Z'
    comment: >-
      Created worktree for branch
      flux/RUNE-4-rune-marker-polish-meaningless-top-pick-star-hotkey-warning-
    event: worktree-created
    id: a-2026-09-13t06-24-42-859z
  - type: agent_session
    sessionId: 1b4bcb1a-abc8-460b-8724-41ace087f9ed
    startedAt: '2026-09-13T06:24:42.296Z'
    status: cancelled
    progress:
      - timestamp: '2026-09-13T06:24:45.867Z'
        message: >-
          ⚠️ Rate limited: allowed_warning [five_hour] (resets at
          2026-09-13T08:30:00.000Z)
    user: Claude Code
    date: '2026-09-13T06:24:42.296Z'
    enginePid: 21456
    outcome: Claude Code session stopped by user.
    endedAt: '2026-09-13T06:24:55.197Z'
    originalProgressCount: 1
    finalMessage: >-
      ⚠️ Rate limited: allowed_warning [five_hour] (resets at
      2026-09-13T08:30:00.000Z)
  - type: comment
    user: Agent
    comment: >-
      Merged. Three fixes from the first in-game test: the gold hover wash no
      longer marks a whole row as succession runes (contrast test against a band
      just outside each cell, calibrated on the real fixture), the top-pick star
      is only awarded when it beats a runner-up, and the hotkey registration
      failure is logged once instead of eight times. Suite 856/856.
    completionComment: true
    date: '2026-09-13T06:24:54.714Z'
    completion:
      changedFiles:
        - src/OCR/RuneIconFingerprinter.cs
        - src/Runes/RuneRowScorer.cs
        - src/App/GlobalHotkeyService.cs
        - tests/src/Runes/RuneMarkerPolishTests.cs
        - tests/src/Runes/RuneRowScorerTests.cs
      decisions:
        - >-
          Gilded requires gold contrast against surroundings, not absolute gold,
          so the hover highlight is rejected
        - Top-pick badge requires a strictly lower runner-up
      residualRisk: >-
        The hover fix is validated on a synthetic gold-washed row and the
        existing fixture; not yet confirmed against a real hover capture from
        the user.
    id: c-2026-09-13t06-24-54-714z
  - type: status_change
    from: Ready
    to: Done
    user: Agent
    date: '2026-09-13T06:24:54.917Z'
  - type: activity
    user: Temper
    comment: >-
      Temper disarmed — a finish/merge flow is taking over session teardown for
      this ticket.
    date: '2026-09-13T06:24:54.926Z'
    id: a-2026-09-13t06-24-54-926z
  - type: comment
    user: Agent
    comment: >-
      Evidence that the hover wash was the dominant failure, from the user's
      live catalog before the fix reached them.


      They kept running the pre-fix build at `obj/Release/publish` (the v2 build
      with this fix sat unused at `E:\Git\RuneshapeBuilds\v2`). In roughly 20
      minutes of play its library grew from 9 to **42** stored sprites.
      Rendering all 42 shows the crops are mostly well-formed cells — the
      segmentation is doing its job — but a clear majority are **not gilded at
      all**: blue corner-studded frames and plain tan frames are both present,
      and only a handful carry the gold frame with tabs. That is the signature
      of the row-wide gold tint being read as a gilded border on every cell in
      the row, which is exactly what `MinGoldContrast` rejects.


      A few entries are genuinely mis-cropped (overlapping or partial cells), so
      segmentation is not perfect either — worth revisiting if it persists after
      the fix, but it is a small minority next to the false-positive flood.


      Consequence worth noting for the design: every false positive becomes a
      permanent library entry the user is asked to name. A detector false
      positive is therefore not just a wrong marker, it is durable clutter. If
      the contrast fix does not fully settle this in play, the next step is a
      row-level guard (a row where nearly every cell reads gilded is a tinted
      row, not a jackpot) rather than more threshold tuning.


      The user was moved to a clean v3 build (this fix plus RUNE-5's layout)
      with `rune-catalog.json` removed so the polluted 42 do not carry over.
    date: '2026-09-13T06:41:34.793Z'
    selfAttested: true
    pin: true
    id: c-2026-09-13t06-41-34-793z
branch: flux/RUNE-4-rune-marker-polish-meaningless-top-pick-star-hotkey-warning-
needsAction: null
implementationLink: 'https://github.com/guybnd/RuneshapePriceChecker/pull/4'
swimlane: null
baselineCommit: ce4bedb50393b75fe4c8157e5a48553bd50ff120
diffSummary:
  - file: src/App/GlobalHotkeyService.cs
    additions: 18
    deletions: 3
  - file: src/OCR/RuneIconFingerprinter.cs
    additions: 51
    deletions: 2
  - file: src/Runes/RuneRowScorer.cs
    additions: 6
    deletions: 1
  - file: tests/src/Runes/RuneMarkerPolishTests.cs
    additions: 146
    deletions: 0
  - file: tests/src/Runes/RuneRowScorerTests.cs
    additions: 1
    deletions: 1
---
> **TL;DR** — First live test of RUNE-2 found two rough edges. Every marked rune gets the ★ badge when nothing is bound yet, because they all tie on weight, so the badge says nothing. And the reset hotkey logs the same registration failure eight times at startup.

Found by the user testing the merged build (master `ce4bedb`) in game, 2026-09-13. Detection itself works: the catalog learned 9 distinct gilded sprites across several panels and the frames land correctly on the icons.

## 1. Top-pick star is meaningless when weights tie

`RuneRowScorer` awards `IsTopPick` to every new rune whose weight equals the maximum. With nothing bound, every sprite scores `UnknownRuneWeight` (1.0), so **every** marked rune on screen gets a ★ — visible in the user's screenshot, four runes all starred.

**Fix:** only award the badge when the top weight is strictly greater than the next distinct weight among new runes on screen. Ties at the top with no runner-up mean there is nothing to choose between, so no badge.

## 2. Reset hotkey logs the same failure eight times

`Ctrl+Alt+R` is already taken on the user's machine (Win32 1409, `ERROR_HOTKEY_ALREADY_REGISTERED`), which is a legitimate outcome — the fallback button works. But `GlobalHotkeyService` re-registers on every `IOptionsMonitor.OnChange`, which fires many times per settings write, and logs a warning each time. Eight identical warnings at startup.

**Fix:** skip re-registration when the hotkey text has not changed, and log a given failure only once per distinct hotkey string.

## Acceptance criteria

- [ ] With every rune unbound (all weights equal), no ★ is drawn; the `?` badges and frames are unchanged.
- [ ] With one rune at a strictly higher weight than the rest, exactly that rune (or those tied at the top) is starred.
- [ ] Startup logs at most one hotkey warning per distinct hotkey string; changing the hotkey in the dashboard re-registers once.
- [ ] Suite stays green.
