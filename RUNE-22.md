---
id: RUNE-22
title: >-
  Gilded purple rune in cell 6 is dropped in 5 of 6 rows — cell lattice drifts
  right of its gold frame
status: Grooming
priority: High
effort: M
assignee: unassigned
tags:
  - bug
  - runes
  - ocr
  - fixtures
createdBy: Agent
updatedBy: Agent
history:
  - type: activity
    user: Agent
    date: '2026-09-13T13:20:23.409Z'
    comment: Created ticket.
    id: a-2026-09-13t13-20-23-409z
  - type: activity
    user: Agent
    date: '2026-09-13T13:24:42.182Z'
    comment: >-
      Created worktree for branch
      flux/RUNE-22-gilded-purple-rune-in-cell-6-is-dropped-in-5-of-6-rows-cell-
    event: worktree-created
    id: a-2026-09-13t13-24-42-182z
  - type: activity
    user: Agent
    date: '2026-09-13T13:28:26.294Z'
    comment: Updated description. Changed effort to M.
    id: a-2026-09-13t13-28-26-294z
  - type: activity
    user: Plan Gate
    comment: >-
      Plan-review gate on — looping review → revise until approved (approval
      flags you to confirm), or parking after 2 revise attempt(s).
    date: '2026-09-13T13:28:40.601Z'
    id: a-2026-09-13t13-28-40-601z
  - type: activity
    user: Furnace
    date: '2026-09-13T13:28:40.626Z'
    comment: "\U0001F3AF Launch focus: You are reviewing a TICKET PLAN, not committed code — this ticket is still in Grooming and has no diff. Read its full description (title, body, `## Acceptance criteria`) and its latest published artifact (if any) as the plan under review. Depth: standard. Full method for each check below: `read_skill('orchestrator', 'Plan-review methodology')`. Anchor check: verify every cited file/symbol/line still exists and means what the plan says — re-derive fresh every pass, never trust a prior citation. Artifact check (FLUX-1313): no `publish_artifact` revision exists for this ticket (confirmed deterministically by the pre-gate lint). If this plan is UI/UX-shaped (visual layout, a new component, an interaction change), flag it in the review comment as a gap — do not approve silently. This is a flag, not a blocker: note it and still record your verdict on the plan's own merits. Reground (FLUX-1048): check `.docs/release-notes/INDEX.md` + sibling/recently-Done tickets for work that already landed part of this plan. Acceptance-criteria coverage: confirm the AC checklist is testable and every item is addressed by the plan. Consequence tracing: for every destination this plan moves content/config into, name who consumes it and confirm the move still serves the plan's goal. Record your verdict via `change_status` — leave `newStatus` as \"Grooming\" (do NOT move the ticket) and set `planReviewState` to \"approved\" or \"changes-requested\" (never `reviewState`; that is a different field for the post-Todo code-review gate). Posting a comment that starts with **APPROVED** or **CHANGES NEEDED** is not enough by itself — without the `change_status` call the ticket will be parked for a human over an unrecorded verdict. Deterministic lint already ran and flagged this (non-blocking — already reflected in the artifact check above where applicable):\n- **W1**: No published artifact revision on an M+ plan. Flag only — whether this plan is UI/UX-shaped enough to need one is a judgment call this linter cannot make."
    summary: >-
      Launch focus recorded (2014 chars) — methodology detail via
      read_skill('orchestrator', 'Plan-review methodology'). Full text
      recoverable via expand.
    id: a-2026-09-13t13-28-40-626z
  - type: comment
    user: Agent
    comment: >-
      Groomed: root cause measured on the capture (band 4–8 px low / 8 px tall,
      one-gilded-cell bias in ScorePlacement); plan pins the band on the dark
      bevel line, snaps height to cell width, replaces the ring-separation term,
      and adds fixture 6 Raw.png with a test. User asked for grooming and
      implementation in one go, so implementation continues in this session on
      the RUNE-22 worktree.
    date: '2026-09-13T13:28:40.671Z'
    id: c-2026-09-13t13-28-40-671z
  - type: agent_session
    sessionId: ecfbb583-ed3e-4f1d-9e2a-fae7277da606
    startedAt: '2026-09-13T13:28:40.625Z'
    status: active
    progress: []
    user: Claude Code
    date: '2026-09-13T13:28:40.625Z'
    enginePid: 21456
branch: flux/RUNE-22-gilded-purple-rune-in-cell-6-is-dropped-in-5-of-6-rows-cell-
planGateRunning: true
planGateAttempts: 0
planGateMode: loop-confirm
baselineCommit: 9f5fd742fe208726269b3ea8887c20b9b8d88b3f
---
> **TL;DR** — On six-icon rows the gilded purple rune in cell 6 is framed in only one row of six. The icon-row locator places every band 4–8 px below the cells (or 8 px too tall), which puts the silver cell's light frame right at the border-detection threshold and lets the lattice drift 16–22 px right of the gold frame. Pin the band's bottom on the cells' dark bevel line and snap its height to the cells' width, stop rewarding rows with exactly one gilded cell, and lock it with a new fixture.

## Problem / Motivation

On a 2560x1440 Combinations panel with six-icon rows (`E:\Git\RuneshapeCaptures\incoming\2026-09-13-purple-gilded-missed.png`, capture region 663x715), every row has a gilded rune in cell 2 and a gilded purple rune in cell 6. The marker overlay frames all seven cell-2 runes but only one of the six purple cell-6 runes (row 4). The dropped cells are classified `Ambiguous` and skipped with a Debug-only log, so at the default Information level nothing shows.

## Root cause (measured on the capture)

The gold frame of cell 6 sits at x=273–275 / 322–324 in **every** row; the game draws all six identically. The difference is in `RuneIconFingerprinter.LocateIconRow`:

- The cells' true extent is 45 px tall with a **dark bevel line** (HSV value < 0.25, ≥78% of the icon strip's columns) at y=45, 153, 261, 369, 477, 585. The located bands were [9,53], [105,157], [224,268], [320,374], [439,483], [537,589]: 4–8 px low, or 53–55 px tall.
- `ScorePlacement` is flat across those shifts (cell count dominates and every shifted band still segments six cells), so `RowDistancePenalty` pulls the band down towards the text line, and the 53 px candidate (text height 28 × `IconToTextHeightRatio` 1.9) beats the 45 px one on tie-breaks.
- **Consequence 1 (rows 0, 2):** with the band 6–8 px low, border columns cover only 37–39 of the 39 rows `BorderColumnCoverage` demands. The silver-framed cell 5 and cell 4's right border drop out; `SegmentIconCells` pairs 169→223 (56 px) and 223→324; `RegulariseToLattice` then takes pitch 58 and width 52 from the decorated left-hand cells and rebuilds cell 6 at x=289.
- **Consequence 2 (rows 1, 3, 5):** a 53 px band makes `CellWidthMinRatio` reject the real 45 px right borders, cells pair one border too far (55–56 px), pitch inflates to 60, cell 6 lands at x=295.
- **Consequence 3 (all rows):** the ring-separation term `bestRing − restRing` rewards a band in which exactly one cell reads gold, so a row with two gilded cells is scored *against* its correct placement (both ≈0.30 → separation ≈0) and *for* the misplaced one (0.22 vs 0.04).

On the correct band [433,477] every border column covers 41–45 of 45 rows, and in row 4 (where cell 5's columns happened to scrape 39/39) segmentation is already right.

## Implementation plan

1. **Score the bevel in `ScorePlacement`** (`src/OCR/RuneIconFingerprinter.cs`). After `SegmentIconCells`, measure the fraction of `IsDarkAt` pixels along `y = bottom` over the x-span from the first cell's left to the last cell's right (span-relative, so two-icon rows on `2 Raw.png` score the same as six-icon rows). Add `fraction × BevelLineWeight` (new constant, ~40 000: above the ring term's ceiling of 6 000 and a 9 px text-distance penalty of 3 600, below one cell's 100 000). Reuses `IsDarkAt` and `DarkLineMaxValue`, which `LocateIconRowByInkRuns` already uses for the same line.
2. **Replace the ring-separation term** with the largest gap between consecutive rings sorted descending (the natural two-cluster split). A row with one or two gilded cells scores its correct band equally; a row with none stays near zero. Keep the 20 000 weight.
3. **Snap the band to square cells in `LocateIconRow`.** After the best placement is chosen, take the median width of its non-gilded cells; if it is within `SquareSnapTolerance` (new constant, 8 px) of the band height, keep `bottom` and set `top = bottom − width + 1`. Cells are square in the game, so the width — measured from dark borders — is the better height. Do not snap when fewer than two plain cells exist (single gilded cell rows on `2 Raw.png`).
4. **Fixture.** Copy the capture to `tests/fixtures/runeicons/2560x1440/6 Raw.png` (same 663x715 region as fixtures 1–5). Add `RuneTwoGildedRowFixtureTests` (`tests/src/OCR/`) modelled on `RuneNarrowRowFixtureTests`: 7 rows, icon counts [6,6,6,6,6,6,4], gilded at index 1 in every row and index 5 in rows 0–5; assert zero ring overlap with the existing 0.20/0.08 margins, `ExtractRowKeys` yields 2 keys in rows 0–5 and 1 in row 6, every cell height within [38,60], and each row's two keys are ≥16 bits apart.
5. **Regression.** `1 Raw.png` and `2 Raw.png` tests (`RuneIconFingerprinterFixtureTests`, `RuneNarrowRowFixtureTests`, `RuneMarkerFixtureTests`, `RuneCropStabilityDiagnostics`) must stay green; identity hashes are plate-anchored (`FindPlateBounds`) so a vertical band shift of ≤8 px must not move them.

Open Questions (non-blocking) — using default: the Debug-only "ambiguous — dropped" log stays at Debug; `ExtractRowKeys` runs per OCR cycle and an Information line there would spam.

## Acceptance criteria

- [ ] On `6 Raw.png`, rows 0–5 each yield exactly two gilded keys (cells 2 and 6) and row 6 yields one; no plain cell is gilded or ambiguous.
- [ ] Gilded rings ≥ 0.20 and plain rings ≤ 0.08 on `6 Raw.png`, with zero overlap.
- [ ] Every located band on `6 Raw.png` ends on the dark bevel line (bottom ∈ {45,153,261,369,477,585,693}) and is 45 px tall in the six-icon rows.
- [ ] All existing rune fixture tests pass unchanged.

## Recommended Tests

Fixture-level (`dotnet test tests/Tests.csproj --filter FullyQualifiedName~Rune`): the new fixture test above plus the four existing fixture classes. The unit test `SegmentIconCells_SyntheticBorderedCells_FindsEachCellAndFlagsTheGoldOne` covers the synthetic path and should be unaffected since it feeds `SegmentIconCells` directly.
