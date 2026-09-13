---
id: RUNE-3
title: Close the RUNE-1 spike-gate sample bar with more real captures
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
---
Follow-up to **RUNE-1**, which moved to Ready on single-fixture evidence by user decision (2026-09-13). The detector passes every gate check on `tests/fixtures/runeicons/2560x1440/1 Raw.png`; what remains is sample size and stability, which need captures only the user can produce.

## Captures needed (from the user, next play session)

1. **More Combinations panels at 2560x1440** — fullscreen, native resolution, different panels so new gilded runes appear. Each one adds gilded/non-gilded samples toward the ≥30/class bar and seeds RUNE-2's catalog with new sprites.
2. **A burst of ≥5 back-to-back fullscreen shots of one unchanged panel** — for (c1): identical keys frame to frame.
3. **One fullscreen shot at any other resolution the user plays at** — for (c3) and the second-profile half of (a2)/(b). Skip if the user only plays at 2560x1440; then record that as the supported profile.

Attach them to this ticket; cropped or scaled screenshots are not usable (see RUNE-1's fixture findings).

## Work once captures exist

- Crop each capture to its `OcrResolutionProfiles` region and add it under `tests/fixtures/runeicons/<profile>/` (`1 Raw.png` naming, or `N Raw.png` for extra panels — extend `RuneIconFingerprinterFixtureTests.Profiles()` and the `GroundTruth` table accordingly, ground truth confirmed by eye at 3–4× zoom).
- Add a burst test: every frame yields identical `RuneKey` hashes and hue buckets per row.
- Re-run the gate; record per-check results in a pinned evidence comment on RUNE-1 (or here) and update RUNE-1's acceptance-criteria checkboxes.
- Check row 0: RUNE-1 noted the 2560x1440 capture region top (Y=205) clips the first row's icons. If the same rune in row 0 and another row hashes >8 bits apart, propose nudging that profile's Y up ~10 px.

## Acceptance

- [ ] ≥30 gilded and ≥30 non-gilded cell samples across ≥2 profiles (or documented single-profile decision), zero gold-ring overlap.
- [ ] Burst: identical keys across ≥5 frames.
- [ ] RUNE-1 acceptance criteria updated with measured values.
