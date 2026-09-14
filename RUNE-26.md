---
id: RUNE-26
title: >-
  Combinations table misses currency recipes and quantity prefixes; add the
  first 1080p fixture
status: Done
priority: High
effort: S
assignee: unassigned
tags:
  - bug
  - runes
  - fixtures
createdBy: Agent
updatedBy: Agent
history:
  - type: activity
    user: Agent
    date: '2026-09-14T03:30:46.782Z'
    comment: Created ticket.
    id: a-2026-09-14t03-30-46-782z
  - type: activity
    user: Agent
    date: '2026-09-14T03:31:20.531Z'
    comment: >-
      Created worktree for branch
      flux/RUNE-26-combinations-table-misses-currency-recipes-and-quantity-pref
    event: worktree-created
    id: a-2026-09-14t03-31-20-531z
  - type: activity
    user: Agent
    date: '2026-09-14T03:37:03.233Z'
    comment: Updated description. Changed effort to S.
    id: a-2026-09-14t03-37-03-233z
  - type: comment
    user: Agent
    comment: >-
      Implemented in d78c6ed on flux/RUNE-26 (pushed). 314 recipes, runes from
      link targets, stack sizes in the identity, strict count/stack lookup,
      1080p fixture. Suite 1051/1051.
    date: '2026-09-14T03:37:09.444Z'
    id: c-2026-09-14t03-37-09-444z
  - type: status_change
    from: Grooming
    to: In Progress
    user: Agent
    date: '2026-09-14T03:37:09.444Z'
  - type: comment
    user: Agent
    comment: >-
      Ready to merge: flux/RUNE-26, commit d78c6ed, 1 ahead of origin/master.


      Fixes three defects in the shipped Combinations table found by the first
      1080p capture: currency cards were skipped by the name regex (no
      Divine/Chaos/Exalted/Mirror recipes), runes were read from tooltips that
      only sometimes carry the "Level N - M" prefix (72 of 644 cards lost runes
      — Aldur's Legacy shipped with 1 of 10, shifting every index), and the
      game's "10x" stack prefix was not parsed. Runes now come from icon link
      targets, stack size is part of the identity, the table has 314 recipes,
      and Lookup returns null on any cell-count or stack mismatch instead of the
      nearest recipe.


      Validation: 1051/1051. New 1920x1080 fixture: rows 1–3 segment exactly at
      ~34 px cells (gilded 0.39–0.44, plain ≤ 0.02) and name power; row 0
      (clipped by the capture top, 8 cells of 9) resolves to null, never a wrong
      rune.


      Residual: the clipped-top-row miss remains (follow-up). Any sprite the
      installed app auto-named from a currency or unique row between 00:19 and
      now could have been misnamed by the truncated recipes; the conflict log
      line will show it, and the Rune Library lets the user re-pick.
    date: '2026-09-14T03:37:23.938Z'
    completionComment: true
    completion:
      changedFiles:
        - ocr/rune-combinations.json
        - scripts/update-rune-combinations.ps1
        - src/Runes/RuneCombinationTable.cs
        - tests/fixtures/runeicons/1920x1080/1 Raw.png
        - tests/src/OCR/Rune1080pFixtureTests.cs
        - tests/src/Runes/RuneCombinationTableTests.cs
      decisions:
        - 'Runes read from link targets, not tooltips'
        - Stack size is part of recipe identity; 1x = no-stack
        - Cell count and stack mismatches return null
      residualRisk: >-
        Clipped top row still miscounted at 1080p; sprites auto-named from
        truncated recipes before this fix may be wrong
    id: c-2026-09-14t03-37-23-938z
  - type: activity
    user: Agent
    comment: 'PR created: https://github.com/guybnd/RuneshapePriceChecker/pull/30'
    date: '2026-09-14T03:37:28.874Z'
    id: a-2026-09-14t03-37-28-874z
  - type: status_change
    from: In Progress
    to: Ready
    user: Agent
    date: '2026-09-14T03:37:28.874Z'
  - type: activity
    user: Temper
    comment: >-
      Temper on — auto-reviewing this ticket. It will loop review →
      re-implementation until the reviewer approves (PR left open at Ready,
      never merged), or park after 2 re-implementation attempts.
    date: '2026-09-14T03:37:29.405Z'
    id: a-2026-09-14t03-37-29-405z
  - type: activity
    user: Furnace
    date: '2026-09-14T03:37:29.488Z'
    comment: "\U0001F3AF Launch focus: You are the ONLY reviewer for this ticket in this Furnace run — no orchestrator will synthesize other reviews, so you own the decision. Your review is not complete until you call `change_status` with `reviewState` set to \"approved\" or \"changes-requested\" to match your verdict. Posting a comment that starts with **APPROVED** or **CHANGES NEEDED** is not enough by itself — without the `change_status` call, the ticket will be parked for a human to unblock even though your review already happened."
    id: a-2026-09-14t03-37-29-488z
  - type: activity
    user: Agent
    date: '2026-09-14T03:37:29.989Z'
    comment: >-
      Created worktree for branch
      flux/RUNE-26-combinations-table-misses-currency-recipes-and-quantity-pref
    event: worktree-created
    id: a-2026-09-14t03-37-29-989z
  - type: agent_session
    sessionId: 660f96b9-61e7-4910-82cd-1d069ed184d5
    startedAt: '2026-09-14T03:37:29.487Z'
    status: cancelled
    progress:
      - timestamp: '2026-09-14T03:37:33.976Z'
        message: >-
          ⚠️ Rate limited: allowed_warning [seven_day] (resets at
          2026-09-16T17:00:00.000Z)
    user: Claude Code
    date: '2026-09-14T03:37:29.487Z'
    enginePid: 21456
    outcome: Claude Code session stopped by user.
    endedAt: '2026-09-14T03:37:42.444Z'
    originalProgressCount: 1
    finalMessage: >-
      ⚠️ Rate limited: allowed_warning [seven_day] (resets at
      2026-09-16T17:00:00.000Z)
  - type: comment
    user: Agent
    comment: >-
      Table parser reads runes from icon link targets and keeps stack sizes (314
      recipes, currency included); Lookup is strict on count and stack; first
      1920x1080 fixture added, rows 1–3 exact. Suite 1051/1051. User asked to
      run tests against the 1080p capture and act on the findings.
    completionComment: true
    date: '2026-09-14T03:37:41.901Z'
    id: c-2026-09-14t03-37-41-901z
  - type: status_change
    from: Ready
    to: Done
    user: Agent
    date: '2026-09-14T03:37:42.130Z'
  - type: activity
    user: Temper
    comment: >-
      Temper disarmed — a finish/merge flow is taking over session teardown for
      this ticket.
    date: '2026-09-14T03:37:42.149Z'
    id: a-2026-09-14t03-37-42-149z
branch: flux/RUNE-26-combinations-table-misses-currency-recipes-and-quantity-pref
needsAction: null
planReviewState: null
planReviewBodyHash: null
implementationLink: 'https://github.com/guybnd/RuneshapePriceChecker/pull/30'
swimlane: null
baselineCommit: 0cbfc4c05f1a58dda13170eb8673fa55962d503c
diffSummary:
  - file: ocr/rune-combinations.json
    additions: 315
    deletions: 212
  - file: scripts/update-rune-combinations.ps1
    additions: 20
    deletions: 13
  - file: src/Runes/RuneCombinationTable.cs
    additions: 46
    deletions: 17
  - file: tests/fixtures/runeicons/1920x1080/1 Raw.png
    additions: 0
    deletions: 0
  - file: tests/src/OCR/Rune1080pFixtureTests.cs
    additions: 168
    deletions: 0
  - file: tests/src/Runes/RuneCombinationTableTests.cs
    additions: 42
    deletions: 18
---
> **TL;DR** — On the user's first 1080p capture (four "Nx Divine Orb" rows) every table lookup returned null. Three causes in RUNE-24's table: the card parser skipped every currency card (name behind an `<img>` inside the link), runes were read from icon tooltips that only sometimes carry the "Level N - M" prefix (recipes lost up to nine of ten runes, shifting every index), and the game's "10x" stack prefix was never parsed. Fixed all three, made a cell-count or stack mismatch return null instead of guessing, and shipped the capture as the first 1920x1080 fixture.

## Evidence (capture `E:\Git\RuneshapeCaptures\incoming\2026-09-14-1080p-divine-orbs.png`, cropped to the 1920x1080 profile 52,154 497x536)

- Text rows: 4 found. Rows 1–3 (6, 5, 4 icons) segment exactly, gilded at index 2 with rings 0.386–0.442, plain ≤ 0.022, cells 33–35 px — the detector itself is fine at 1080p.
- Row 0 (9 icons, clipped by the capture top) yields 8 cells starting at x=35 instead of 9 from x=0; its two gilded cells come out at indices 1 and 5 of 8 instead of 2 and 6 of 9. The old `Lookup` fell back to the unfiltered candidate on a count mismatch and would have named the wrong rune with full confidence.
- `Lookup("10x Divine Orb", …)` → null: no "Divine Orb" recipe in the shipped JSON and the key normalised to `10xdivineorb`.
- Parser audit against the saved page: 72 of 644 cards lost runes to the tooltip regex — e.g. Aldur's Legacy shipped with 1 rune of 10.

## Implemented (commit d78c6ed on flux/RUNE-26)

1. `scripts/update-rune-combinations.ps1`: name regex allows an `<img>` inside the link; runes read from `<a href="X_Rune">` link targets (always present) instead of tooltip text; `xN` suffix parsed into `quantity`; suffix folded into the name only when it is neither a level nor a stack; JSON gains `"quantity"`; sanity floor raised to 300. Regenerated: 314 recipes (was 211).
2. `RuneCombinationTable`: `Quantity` on the model; `ParseRowText` returns `(Key, Level, Quantity)`, stripping a leading `10x ` or trailing ` x10`; `Lookup` is strict on cell count and stack size (null when no recipe has that shape), "1x" matches the no-stack recipe, level stays lenient.
3. Fixture `tests/fixtures/runeicons/1920x1080/1 Raw.png` + `Rune1080pFixtureTests`: rows 1–3 exact (counts, gilded index 2, ring margins, 28–42 px cells) and named power by the table; row 0 held to "never misnamed" (null allowed) with the clipped-top-row miss recorded for a follow-up.
4. `RuneCombinationTableTests` extended: quantity prefix/suffix, strict count and stack, Aldur's Legacy has 10 runes, shipped table ≥ 300.

## Acceptance criteria

- [x] Table contains the currency recipes; "10x Divine Orb" resolves cell 3 → power and cell 7 → toxic.
- [x] Every recipe carries all its runes (Aldur's Legacy = 10).
- [x] A row whose detected cell count or stack size matches no recipe resolves to null, never a guess.
- [x] 1080p fixture test green for rows 1–3; full suite 1051/1051.

## Follow-up

Clipped top row loses its first cell at 1080p (8 of 9) and hashes 13 bits off at 1440p — one ticket for "rows clipped by the capture top".
