---
id: RUNE-3
title: Capture the failing panel + a stability burst (spike-gate bar dropped)
status: Todo
priority: Medium
effort: S
assignee: unassigned
tags:
  - spike
  - ocr
  - fixtures
createdBy: Agent
updatedBy: Agent
history:
  - type: activity
    user: Agent
    date: '2026-09-13T04:24:57.522Z'
    comment: Created ticket.
    id: a-2026-09-13t04-24-57-522z
  - type: comment
    user: Agent
    comment: >-
      User shared three new Combinations-panel screenshots in chat (2026-09-13,
      Forbidden Rites League, monster level 79). Observed content:

      - Shot 1: 2 rows, 5 icons each ("5x Random Currency", "1x Uncut Spirit Gem
      (Level 19)").

      - Shot 2: 2 rows, 6 icons each with the name text wrapped BELOW the icons
      ("1x Uhtred's Saga", "3x Greater Chaos Orb") — the wrap-case sample the
      gate lacked; gilded rune at slot 5 in both.

      - Shot 3: 5 rows, 5 icons each ("1x Warding Rune of
      Hollowing/Bodyguards/Equinox/Obsession/Armature").

      - Shots 2 and 3 also show the in-world remnant socket bar with the
      inherited (carried) rune displayed right of the five sockets — a candidate
      future source for the carried set (deferred, see RUNE-2 out-of-scope).


      Chat copies are downscaled and unusable as pipeline fixtures. Asked the
      user to attach the original PNGs here (or give a folder path), and to
      hover each gilded rune so the tooltip names it, so sprites can be paired
      with rune names for RUNE-2's library.
    date: '2026-09-13T04:29:15.850Z'
    selfAttested: true
    id: c-2026-09-13t04-29-15-850z
  - type: comment
    user: Agent
    comment: >-
      **Capture drop folder (decided 2026-09-13):**
      `E:\Git\RuneshapeCaptures\incoming\` — outside the repo so nothing large
      is committed by accident. The user drops original, unscaled fullscreen
      PNGs there; bursts of identical frames go in a `burst-*` subfolder. The
      RUNE-3 work crops each into `tests/fixtures/runeicons/<profile>/` at the
      `OcrResolutionProfiles` offsets and records ground truth per file.
    date: '2026-09-13T05:13:10.496Z'
    selfAttested: true
    pin: true
    id: c-2026-09-13t05-13-10-496z
  - type: activity
    user: Agent
    date: '2026-09-13T08:57:33.313Z'
    comment: Updated title. Updated description.
    id: a-2026-09-13t08-57-33-313z
  - type: comment
    user: Agent
    comment: >-
      Rescoped after the user asked why the sample step was needed at all.


      Two things were conflated and are worth separating:


      **Unit confusion.** A "sample" here is one icon *cell*, not one
      screenshot. The existing single fixture (`2560x1440/1 Raw.png`) already
      yields 30 cells — 6 gilded, 24 plain, from rows of [6,6,6,4,4,4] with the
      gilded rune at index 2. So "≥30 gilded" was roughly five more screenshots,
      not ninety captures. The number read as far more work than it was.


      **The bar was ceremony anyway.** It was the evidence gate for a spike's
      go/no-go decision. That decision is settled — the detector is merged and
      running on the user's installed copy. Gathering evidence to decide
      something already decided is not worth a play session.


      What survives is narrow and cheap: the panel where Annihilation was missed
      and Stability's marker was misaligned (a real defect I cannot diagnose
      without the pixels — the obvious suspect measured neutral), and a 5-frame
      burst to rule out frame-to-frame flapping. Fixtures will accumulate from
      defect captures as they arrive.


      Also recorded on the ticket: debug images use fixed filenames in one
      directory and overwrite every cycle, so nothing accumulates passively
      today.
    date: '2026-09-13T08:57:43.137Z'
    selfAttested: true
    summary: >-
      Rescoped 2026-09-13: dropped the ≥30/class spike-gate bar (the go/no-go it
      served is already settled by shipping); ticket now targets the
      Annihilation/Stability defect capture plus a 5-frame burst. Clarified that
      a "sample" is one icon cell, not one screenshot — 30/30 was ~6
      screenshots, not 90.
    pin: true
    id: c-2026-09-13t08-57-43-137z
---
Follow-up to **RUNE-1**. Originally written to close RUNE-1's spike gate (≥30 gilded and ≥30 non-gilded samples across ≥2 profiles). **That bar is dropped** — see the rescope note below.

## Why the ≥30/class bar was dropped

RUNE-1 was scoped as a spike: build the detector, measure it, then decide whether to commit. The sample bar was the go/no-go evidence for that decision. The decision has since been made by other means — the detector is built, merged, and running on the user's installed copy. Re-gathering evidence for a settled decision is ceremony; what is left is two concrete, cheap things.

Fixtures still accumulate naturally: every capture sent in to chase a real defect becomes one.

## Captures actually needed

1. **The 11-row Combinations panel where "Warding Rune of Annihilation" was missed entirely and "Warding Rune of Stability"'s marker sat misaligned.** Fullscreen, native resolution, Save Debug Images on. This is a real unexplained defect — the obvious suspect (ambient band width) measured neutral at 0.21 vs 0.22 headroom, so any fix without these pixels is a guess.
2. **A burst of ≥5 back-to-back fullscreen shots of one unchanged panel.** ~10 seconds of work; catches the detector flapping frame to frame, which would surface in-game as markers flickering.
3. *(Optional)* One shot at any second resolution the user plays at. Skip if 2560x1440 is the only one, and record that as the single supported profile.

Drop location: `E:\Git\RuneshapeCaptures\incoming\` (bursts in a `burst-*` subfolder). Cropped or chat-downscaled screenshots are not usable — see RUNE-1's fixture findings.

## Work once captures exist

- Diagnose the Annihilation miss and the Stability misalignment; fix with a regression fixture covering both.
- Crop each capture to its `OcrResolutionProfiles` region under `tests/fixtures/runeicons/<profile>/` (`N Raw.png`), extending `RuneIconFingerprinterFixtureTests.Profiles()` and the `GroundTruth` table.
- Add a burst test: every frame yields identical `RuneKey` hashes and hue buckets per row.
- Check row 0: RUNE-1 noted the 2560x1440 capture region top (Y=205) clips the first row's icons. If the same rune in row 0 and another row hashes >8 bits apart, propose nudging that profile's Y up ~10 px.

## Acceptance

- [ ] Annihilation miss and Stability misalignment diagnosed and fixed, with a fixture that fails before the fix.
- [ ] Burst: identical keys across ≥5 frames.
- [ ] RUNE-1 acceptance criteria updated with measured values, and the dropped sample bar recorded there as a deliberate decision rather than an unmet one.

## Note: debug images do not accumulate

`OcrLeagueWindowReader` writes fixed filenames (`1 Raw.png`, `6 IconCells.png`, …) into one directory, so each cycle overwrites the last. There is no capture corpus today and no mechanism to build one passively. If a corpus is ever wanted, that is a small opt-in change (timestamped subdirectory per frame, capped) — not part of this ticket.
