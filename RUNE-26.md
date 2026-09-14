---
id: RUNE-26
title: >-
  Combinations table misses currency recipes and quantity prefixes; add the
  first 1080p fixture
status: Grooming
priority: High
effort: None
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
branch: flux/RUNE-26-combinations-table-misses-currency-recipes-and-quantity-pref
---
> **TL;DR** — On the user's first 1080p capture (four "Nx Divine Orb" rows) every table lookup returned null. Two causes in RUNE-24's table: `update-rune-combinations.ps1` skips every currency card because poe2db wraps currency names in `<a><img/>Name</a>` and the name regex allows no tag inside the link (all 120+ currency recipes are missing), and `ParseRowText` does not strip the game's leading quantity ("10x Divine Orb") nor keep the quantity as part of the identity ("Divine Orb x10" vs "x3" are different recipes). Fix both, make a cell-count mismatch return null instead of guessing, and ship the capture as `tests/fixtures/runeicons/1920x1080/1 Raw.png`.

## Evidence (capture `E:\Git\RuneshapeCaptures\incoming\2026-09-14-1080p-divine-orbs.png`, cropped to the 1920x1080 profile 52,154 497x536)

- Text rows: 4 found. Rows 1–3 (6, 5, 4 icons) segment exactly, gilded at index 2 with rings 0.386–0.442, plain ≤ 0.022, cells 33–35 px — the detector itself is fine at 1080p.
- Row 0 (9 icons, clipped by the capture top) yields 8 cells starting at x=35 instead of 9 from x=0; its two gilded cells come out at indices 1 and 5 of 8 instead of 2 and 6 of 9. With the current `Lookup`, a count mismatch falls back to the unfiltered candidate and would name the wrong rune with full confidence — must return null instead.
- `Lookup("10x Divine Orb", …)` → null: no "Divine Orb" recipe in `ocr/rune-combinations.json` (grep finds none) and the key normalises to `10xdivineorb`.

## Plan

1. Script: name regex `<span><a [^>]*>(?:<img[^>]*/?>)?([^<]+)</a>([^<]*)</span>`; parse `x(\d+)` in the suffix into `quantity` (alongside `level`); JSON gains `"quantity": N` (0 when none). Regenerate — expect ~330 recipes.
2. `RuneCombinationTable`: `Quantity` on the model; `ParseRowText` strips a leading `^\d+\s*[x×]\s*` and a trailing `[x×]\s*\d+` and returns the quantity; `Lookup` narrows by quantity when the text states one, and returns null when `cellCount > 0` matches no candidate's rune count (no guessing from a miscounted row).
3. Fixture `1920x1080/1 Raw.png` + `Rune1080pFixtureTests`: rows 1–3 exact (counts 6/5/4, gilded index 2, ring margins), rows' lookups name the table's rune; row 0 asserts only that no lookup returns a rune other than the table's (null allowed) and records the clipped-row miss for the follow-up on clipped top rows.
4. Table tests: "10x Divine Orb" → cell 2 of 9 and cell 6 of 9 per the recipe; "Divine Orb" with a quantity that matches no recipe → null.

## Acceptance
- [ ] Table contains the currency recipes; "10x Divine Orb" resolves both gilded cells.
- [ ] A row whose detected cell count matches no recipe resolves to null, never a guess.
- [ ] 1080p fixture test green for rows 1–3; existing suites unchanged.
