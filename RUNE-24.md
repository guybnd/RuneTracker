---
id: RUNE-24
title: >-
  Resolve gilded runes from the Runeshape Combinations table by row name and
  cell index
status: Grooming
priority: High
effort: M
assignee: unassigned
tags:
  - feature
  - runes
  - ocr
createdBy: Agent
updatedBy: Agent
history:
  - type: activity
    user: Agent
    date: '2026-09-13T14:05:02.269Z'
    comment: Created ticket.
    id: a-2026-09-13t14-05-02-269z
  - type: activity
    user: Agent
    date: '2026-09-13T14:06:00.786Z'
    comment: >-
      Created worktree for branch
      flux/RUNE-24-resolve-gilded-runes-from-the-runeshape-combinations-table-b
    event: worktree-created
    id: a-2026-09-13t14-06-00-786z
  - type: activity
    user: Agent
    date: '2026-09-13T14:07:53.517Z'
    comment: Updated description. Changed effort to M.
    id: a-2026-09-13t14-07-53-517z
branch: flux/RUNE-24-resolve-gilded-runes-from-the-runeshape-combinations-table-b
---
> **TL;DR** — The game's Combinations table is public data: poe2db lists every combination with its runes in the order the panel draws them. The app already OCRs each row's name and knows which cell is gilded, so `(row name, icon count, gilded cell index)` names the rune outright. Ship the table, resolve from it first, and let it name sprites in the library automatically so the "pick which rune this is" step disappears.

## Problem / Motivation

Rune identity today rests on sprite hashing plus a human naming each sprite once. Hashing drifts (the clipped top row of a panel hashes 13 bits from the same rune elsewhere, past the 8-bit threshold), so the same rune can be stored twice, and until someone names a sprite it scores as "unknown". The combination table removes both problems for every row whose name OCR reads.

## Source data (verified 2026-09-13)

`https://poe2db.tw/Runeshape_Combinations`: 211 distinct combinations (the page lists each twice), runes in explicit order in each icon's `data-bs-title="Level 70 - 100 Oath Rune"`. Every name has two variants — "(Level 20)" Lv70+ with 6 runes and a plain Lv65-74 one with 4 — which the in-game prefix ("Skill Level 20:" vs "Support:") and the row's icon count disambiguate. All 33 rune names used exist in the app's 34-rune catalog (only Bait never appears); catalog ids are the first word lower-cased.

Checked against fixture `6 Raw.png` cell for cell: Skyfall = Tempest, Celestial, Protective, Ward, Wisdom, Oath — gilded cell 2 is Celestial, blue rare frame at 4 is Ward, silver glyph at 5 is Wisdom, purple gilded 6 is Oath. The detector's hash groups agree: cell 2 of Skyfall/Triskelion/Leylines (table: Celestial) hash within 7 bits; cell 2 of the other four rows (table: Prismatic) within 5 bits; cell 6 of Skyfall and Animus Exchange (table: Oath) 5 bits apart.

## Implementation plan

1. **Data + refresh script.** `scripts/update-rune-combinations.ps1` downloads the page, splits it on the combination card markup, parses name / "(Level N)" suffix / level tier / ordered rune names, maps names to catalog ids (fails on an unknown rune), dedupes, and writes `ocr/rune-combinations.json` (`{ source, fetchedUtc, combinations: [{ name, level, tier, runes[] }] }`). Embedded via `RuneshapePriceChecker.csproj` like `unique-category-map.json`.
2. **`RuneCombinationTable`** (`src/Runes/RuneCombinationTable.cs`, singleton). `Lookup(rowText, cellCount, cellIndex)`: normalise the OCR row text (drop everything up to the last `:` — "Skill Level 20: Skyfall" → "Skyfall", "Support: Healing Runes" → "Healing Runes"; take `Level N` from the prefix or a "(Level N)" suffix; lower-case letters/digits only), exact key match first then `StrComp.AreFewCharsAway` (1 edit under 8 chars, 2 otherwise), filter candidates by `runes.Count == cellCount`, then by level when known; return `runes[cellIndex]` only when every surviving candidate agrees at that index, else null.
3. **Carry the cell position on the key.** `RuneKey` gains `CellIndex` and `CellCount` (optional, defaulted, so existing constructions compile); `RuneIconFingerprinter.ExtractRowKeys` fills them from the segmented cell list. `RuneRowKeys` gains `ItemName`; `OcrLeagueWindowReader.FilterRuneRowsToMatchedRows` takes the matched item names and attaches them by index (RuneRows already align 1:1 with ItemNames by row Y).
4. **Resolve from the table first.** `RuneCatalog.Resolve(RuneKey key, string? knownRuneId)`: when the table names a rune, the resolution is that rune (weight, carried id) regardless of the hash. Side effects under the lock: a persisted *unbound* binding matching the hash is bound to the rune (carried flag migrated, revision bumped) — the library names itself; a binding bound to a *different* rune is left as the user set it and a warning is logged once per binding id. `RuneRowScorer` takes an optional `RuneCombinationTable` and calls `Lookup(row.ItemName, key.CellCount, key.CellIndex)` per key.
5. **Tests.** `RuneCombinationTableTests` (normalisation incl. OCR noise, count and level disambiguation, ambiguity → null, index bounds; shipped table loads with ≥200 entries and every rune id in the catalog). `RuneCatalogTests` additions: auto-bind on resolve, conflict keeps the user's binding and logs. Fixture test on `6 Raw.png` with the in-game row names: rows resolve to (celestial, oath), (celestial, bond), (prismatic, death), (prismatic, soul), (celestial, life), (prismatic, oath), (prismatic).

Hard-to-reverse: the JSON schema above is the one shipped and refreshed by script; rune ids are the catalog's existing slugs, no new id space.

Open Questions (non-blocking) — using default: on a table/user-binding conflict the table decides the resolution and the binding is left alone with a warning; overwriting a deliberate user choice silently would be worse than one log line.

## Acceptance criteria

- [ ] `ocr/rune-combinations.json` is generated by the script, embedded, and loads with ≥200 combinations whose rune ids all exist in the catalog.
- [ ] On `6 Raw.png` with the in-game row names, every gilded key resolves to the rune the table names (list in step 5) without any binding having been made by hand.
- [ ] After resolving, the matching persisted bindings are bound to those runes; a pre-existing conflicting binding is untouched and a warning is logged once.
- [ ] A row whose name is unreadable or unknown falls back to hash resolution unchanged; all existing tests pass.

## Recommended Tests

Unit: `RuneCombinationTableTests`, `RuneCatalogTests` (new cases). Fixture: `RuneCombinationFixtureTests` on `6 Raw.png`. Script: run `scripts/update-rune-combinations.ps1` once and diff the output against the committed JSON.
