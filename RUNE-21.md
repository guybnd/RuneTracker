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
