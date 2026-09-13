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
