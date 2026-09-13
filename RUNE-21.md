---
id: RUNE-21
title: 'Rank the shortlist, award one badge, and ship the named sprites'
status: Ready
priority: High
effort: M
assignee: unassigned
tags:
  - runes
  - ux
  - feature
createdBy: Agent
updatedBy: Agent
history:
  - type: activity
    user: Agent
    date: '2026-09-13T13:15:12.942Z'
    comment: Created ticket.
    id: a-2026-09-13t13-15-12-942z
  - type: comment
    user: Agent
    comment: >-
      Implemented and deployed. PR
      https://github.com/guybnd/RuneshapePriceChecker/pull/27 (branch
      rune-22-ranked-shortlist-and-sprite-seed), 1009 tests passing, 15 new.
      Installed copy rebuilt at 23:14.


      **Weights** — ocr/rune-catalog.json: opulent 15, power 14, death 13, oath
      12, rebirth 11, life 10. `ShippedCatalogFileHasAll34RunesWithWeights`
      asserts both the exact order and that the lightest shortlisted rune
      outweighs three ordinary ones, so the gap cannot be closed by accident
      later.


      **Single badge** — `RuneRowScorer.Recommend` picks one row (`Beats`:
      score, then best single new rune, then topmost) and badges one key in it
      (heaviest non-carried, leftmost on a tie). `IsBest` is now singular too.
      On `1 Raw.png` rows 2 and 4 both hold Opulent and tie; the fixture test
      asserts exactly one badge, on the topmost.


      **Sprite seed** — `ocr/rune-seed.json`, 46 sprites / 33 runes / 430 KB,
      embedded and merged by `RuneCatalog.MergeSeed`. Additive only; once per
      `seedVersion` (persisted in the user layer) so a deliberately forgotten
      sprite stays forgotten; refused if `hashVersion` differs from
      `CurrentHashVersion`. `scripts/export-rune-seed.ps1` regenerates it.
      `TheShippedSeedIsCurrentAndCoversTheRuneList` fails loudly if
      `CurrentHashVersion` is bumped without a re-export — otherwise every new
      install would silently be back to naming 33 sprites by hand.


      **Library picker** — the six read "Top (15)" rather than "Custom (15)",
      but stay `Custom` as a level so picking a named level over one of them is
      an explicit change, never a silent rounding.


      Two notes for the reviewer:

      - `build-local.ps1 -FreshLibrary` now means "reset to the shipped seed",
      not "start empty". Help text updated.

      - The seed was exported from
      `obj/Release/publish/config/rune-catalog.json`, which held the user's 46
      named sprites. The installed copy's library only had 2 unbound sightings,
      so it will pick the seed up on next launch.
    date: '2026-09-13T13:15:31.800Z'
    completionComment: true
    completion:
      changedFiles:
        - ocr/rune-catalog.json
        - ocr/rune-seed.json
        - src/Runes/RuneRowScorer.cs
        - src/Runes/RuneCatalog.cs
        - src/Runes/RuneCatalogModels.cs
        - src/Dashboard/RunePriority.cs
        - RuneshapePriceChecker.csproj
        - scripts/export-rune-seed.ps1
        - scripts/build-local.ps1
        - tests/src/Runes/RuneSeedTests.cs
        - tests/src/Runes/RuneRowScorerTests.cs
        - tests/src/Runes/RuneCatalogTests.cs
        - tests/src/Runes/RuneMarkerFixtureTests.cs
        - tests/src/Runes/RunePriorityTests.cs
      decisions:
        - >-
          Row-first recommendation: the game's unit of choice is a row, so ties
          break rather than being shared.
        - >-
          Seeding is additive and versioned so Forget is not undone on the next
          launch.
        - >-
          Shortlist weights stay Custom as a priority level to avoid silently
          rounding 14 to 15 when a named level is picked.
      residualRisk: >-
        A user who already overrode one of the six weights by hand keeps their
        override; the new shipped rank does not reach them for that rune.
      docsUpdated: true
    id: c-2026-09-13t13-15-31-800z
  - type: status_change
    from: In Progress
    to: Ready
    user: Agent
    date: '2026-09-13T13:15:31.800Z'
  - type: activity
    user: Agent
    date: '2026-09-13T13:29:08.693Z'
    comment: >-
      Follow-up on the same PR (#27), commit b672e17 — the user asked to reorder
      priorities in the UI rather than pick levels.


      **The ladder replaces the named levels.** `src/Dashboard/RunePriority.cs`
      and its tests are gone; `src/Dashboard/RuneRanking.cs` takes over. Runes
      list most-wanted-first with ▲▼ arrows; weight is derived from position and
      never typed. Each rung is `Step` (2.0) above the one below, the bottom
      rung is anchored at `BaseWeight + Step` (4.0), and unranked runes share
      `BaseWeight` (2.0) sorted alphabetically. `MoveUp` on a crowd rune puts it
      on the bottom rung; `MoveDown` on the bottom rung returns it to the crowd
      — so the ladder has a way in and a way out, and reordering is reversible
      (`ReorderingIsReversible`).


      Why the levels had to go: two runes on one level are indistinguishable, so
      the scorer had no basis to prefer one row over another. That is the same
      defect the single-badge work in this ticket was fixing from the other end.


      **Shipped order** (the user's own): opulent 20, power 18, rebirth 16,
      death 14, bond 12, life 10, soul 8, time 6, oath 4; the other 25 at 2.0.
      `ShippedCatalogFileHasAll34RunesWithWeights` now asserts the JSON agrees
      with what `RuneRanking` derives from position, so the file and the UI
      cannot drift.


      **Two consequences worth flagging to a reviewer:**


      1. `RuneRowScorer` now uses `weight > HighValueWeight`, not `>=`. An
      unranked rune weighs exactly the 2.0 baseline and `HighValueWeight`
      defaults to 2.0, so the old comparison would have painted all 34 runes
      rare-yellow. No config migration needed, which is why the threshold was
      left at 2.0 rather than raised.


      2. New `RuneCatalog.CurrentWeightScale` (v1) stamped in the user layer.
      Overrides saved under the old levels topped out at 3.0, which reads as
      "keep this unranked" — the user's screenshot showed exactly this (Power on
      "Must have" = 3.0, Death/Life/Oath on "Wanted" = 2.0), and left alone
      those would have silently held four of their top six off the ladder they
      now ship on. Dropped once on load, logged, nothing else touched. Covered
      by `WeightOverridesFromTheOldNamedLevelsAreDropped`.


      1007 tests passing (RunePriorityTests removed, RuneRankingTests added).
      Deployed to the installed copy at 23:28.


      Not done, deliberately: drag-and-drop reordering (the arrows are
      unambiguous and testable; drag in a WPF `ItemsControl` is a lot of fiddly
      code), a "reset to shipped order" button, and negative/veto weights — the
      user named that last one as a later want.


      Also observed:
      `UpdateCheckerChangelogTests.WriteChangelog_OverwritesExistingChangelog`
      failed once and passed on the immediate re-run with no code change in
      between. Pre-existing flake, unrelated to this work, not investigated.
    summary: >-
      Second commit on PR #27 replaces the named priority levels with a
      reorderable ladder (RuneRanking), ships the user's order, and migrates
      stale weight overrides. 1007 tests passing, deployed 23:28.
    pin: true
    id: a-2026-09-13t13-29-08-693z
needsAction: null
---
Three asks from the user, all pointed at the same thing: less to think about while a remnant panel is open.

## 1. Rank the shortlist

> "we need to tier the good ones in this order so that like we give a top recommendation: opulent, power, death, oath, rebirth, life — they should have a weight like 15 14 13 12 etc"

Shipped weights become Opulent 15, Power 14, Death 13, Oath 12, Rebirth 11, Life 10. Everything else stays 1.0 (0.5 blue tier). The numbers encode two properties: a strict order among the six, and a gap wide enough that no pile of ordinary runes out-scores one of them.

## 2. One badge, not several

> "when we have 2 options together we pick the top weighted row as the recommended pick where only one gets the picker badge, to save user time from thinking about it"

The badge went to every rune tied at the top weight, which handed the comparison back to the user. The game's choice is a *row* — a row is taken whole — so the recommendation is a row first and a rune second. Ties break rather than being shared (best single rune, then topmost row), because two rows worth the same are interchangeable.

The RUNE-4 guard stands: with nothing bound every sprite weighs the same, and no badge is awarded at all.

## 3. Ship the named sprites

> "can we also save all my saved images of the runes into the build somehow like i dont want a user to have to recapture everything if we already have it"

Naming a sprite is the one step the detector cannot do. 46 named sprites covering 33 of 34 runes ship embedded as `ocr/rune-seed.json` and merge into a fresh library on first run — additive only, once per seed version, and refused outright if captured on a different identity hash generation.
