---
id: RUNE-15
title: Glyph colour reads differently for the same rune between rows
status: Todo
priority: High
effort: M
assignee: unassigned
tags:
  - ocr
  - runes
  - bug
createdBy: Agent
updatedBy: Agent
history:
  - type: activity
    user: Agent
    date: '2026-09-13T11:36:07.770Z'
    comment: Created ticket.
    id: a-2026-09-13t11-36-07-770z
---
## Found while fixing the colour measurement (RUNE-16)

The user reported the same rune marked differently in different rows — green with "?" in two rows, orange with ★ in two others, on what is visibly one rune.

RUNE-16 fixed why gold was invisible at all (dark brown ink was outvoting it in the same hue bucket). But it surfaced a second, deeper problem, and this one is **reproduced on the existing fixture**:

```
1 Raw.png, rows 1 and 3 — ground truth says these are the SAME rune
  row 1: 162 gold pixels -> bucket 1
  row 3:  72 gold pixels -> no colour
```

The identity hash is stable across those rows (they match within 8 bits, asserted and passing). The measured colour is not — it lands either side of the threshold.

## Why it matters

Colour is the user's own suggestion for the one rune that matters most, and it is a good one: Opulent is the catalog's only gold tier, so gold uniquely names it with no sprite-binding at all. That is blocked until the reading is stable — `RuneCatalog.UnboundWeight` deliberately does not score gold, because scoring off an unstable reading would make the same rune worth 3 in one row and 1 in the next.

## What is known

- Both rows' cells are the same size (52x45), so it is not a gross placement error.
- The 32px glyph was worse than the 64px sprite (box-filtering desaturates edges); moving to 64px helped but did not fix it.
- A 5% margin is already excluded, so simple gold-frame bleed is not the whole story.
- Counting is over `s >= 0.35, v >= 0.45`, threshold 1% of the crop.

## Worth trying

- Measure what actually differs between the two crops — are the gold pixels in different places, or the same places at different brightness? A per-pixel diff of the two sprites would answer it in one run.
- The gilded frame's inner glow may reach further into the plate on some rows than others; a larger inset would test that cheaply.
- Normalise for overall brightness before thresholding, if one row is simply rendered dimmer.
- Consider judging colour on a rank statistic (e.g. the 90th-percentile saturated hue) rather than a count over a fixed threshold, which is what makes it a cliff edge.

## Acceptance

- [ ] Same rune reads the same tier colour in every row of both fixtures.
- [ ] The fixture's same-rune hue assertion is restored (it was dropped, not relaxed, in RUNE-16).
- [ ] Only then: gold maps to the gold tier's weight, so Opulent scores 3 unnamed.
