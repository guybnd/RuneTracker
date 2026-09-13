---
id: RUNE-2
title: Score combination rows against carried succession runes
status: Grooming
priority: Medium
effort: L
assignee: unassigned
tags:
  - feature
  - ui-ux
createdBy: Agent
updatedBy: Agent
history:
  - type: activity
    user: Agent
    date: '2026-09-12T14:38:01.513Z'
    comment: Created ticket.
    id: a-2026-09-12t14-38-01-513z
  - type: comment
    user: Guy
    comment: >-
      Weight input from the user (chat, 2026-09-13), verbatim intent:


      The succession rune alphabet (as known so far): 1. Bond, 2. Time, 3.
      Death, 4. Power, 5. Opulent, 6. Rebirth.


      Priority: **Opulent is the most important, Power is second most; the rest
      are high value** (i.e. still worth carrying, below those two).


      Direction: build a library of every rune that exists, mark each as
      important/tiered, in a nice UI in the app that the user can prioritise
      against. Screenshots to populate the library are coming.
    date: '2026-09-13T04:27:50.059Z'
    selfAttested: true
    pin: true
    id: c-2026-09-13t04-27-50-059z
  - type: activity
    user: Agent
    date: '2026-09-13T04:31:51.501Z'
    comment: Updated description. Changed effort to L.
    id: a-2026-09-13t04-31-51-501z
---
> **TL;DR** — RUNE-1 now tells us, per Combinations row, which gilded (carry-forward) runes it grants, as stable keys with sprites. This card turns that into the thing the player actually wants: a **rune library** in the dashboard where every discovered rune shows its sprite, gets a name and a weight (Opulent > Power > the rest), a **carried-this-run set** with a reset, and an **overlay line per row** saying which new runes that row grants and which row is the best pick. Unknown runes are shown as "new, unweighted", never silently scored zero.

**Card B of two.** Pure consumer of **RUNE-1** (Ready). Rationale and rejected alternatives: SCRATCH-1's pinned DESIGN RECORD v2 + ADDENDUM. User's weight input is the pinned comment on this ticket.

## Problem / Motivation

Succession runes carry forward along a remnant chain; picking one already carried wastes the slot. The player eyeballs this today. With per-row rune keys in hand, the tool can rank rows and name the runes — but only if the opaque keys get a human naming layer and a place to prioritise them, which is what the user asked for ("a library I can prioritize against, possibly a nice UI in the app").

## Locked decisions (do not re-open)

- **Score:** `rowScore = Σ weight(rune)` over the row's gilded runes **not** in the carried set. Highest wins.
- **Weights:** shipped hand-authored table, user-editable. Seed: Opulent 3, Power 2, Bond/Time/Death/Rebirth 1 (user, 2026-09-13). Shipped entries start **without** hashes — the user binds a discovered sprite to a name in the library.
- **Unknown key → "discovered, unweighted"**, surfaced in the library, scored with `Runes.UnknownRuneWeight` (default 1) and visibly flagged; never silently 0.
- **Key matching is tolerant, not exact:** same `HueBucket` and Hamming(`ShapeHash`) ≤ 8 (RUNE-1 measured same rune 1–3 bits, distinct 22+).
- **No OS config dir exists in this app** — all state lives beside the exe (`AppContext.BaseDirectory/config/appsettings.json`, `…/ocr/`). The catalog's user layer goes in `…/config/rune-catalog.json`.

## Implementation plan

1. **Matcher** — `src/Runes/RuneKeyMatcher.cs`: `IsSame(RuneKey a, RuneKey b)` per the tolerance above; `FindNearest(key, entries)` for catalog lookup. Pure, unit-tested.
2. **Catalog model + persistence** — `src/Runes/RuneCatalog.cs`. Entry: `{ id, displayName?, tier?, weight?, shapeHash?, hueBucket?, spritePngBase64?, seenCount, firstSeenUtc, lastSeenUtc }`. Shipped defaults `ocr/rune-catalog.json` as an `EmbeddedResource` loaded exactly like `ItemNameParser.LoadBaseTypeKeywords` loads `unique-category-map.json` (resource-name `EndsWith`, dev-time disk fallback). User layer `config/rune-catalog.json`: discovered entries, name bindings, weight overrides, plus the `carried` id list. Merge on load: user entry wins by `id`, like `AppSettingsBootstrapper.DeepMergeDefaults` but one level. Saves debounced via `System.Text.Json`.
3. **Discovery sink** — `RuneCatalog.Observe(RuneKey)`: nearest match within tolerance → bump `seenCount`; else add an unnamed entry with the sprite (PNG-encoded from the 32×32 RGB24 bytes). Called from `LeaguePricingWorker.ExecuteAsync` right after the snapshot is read, for every key in `snapshot.RuneRows`.
4. **Carried set** — `src/Runes/CarriedRuneSet.cs`: catalog ids, persisted in the user layer so a restart mid-run keeps it. Marking: dashboard toggle per rune (decision card in the mockup offers a per-row hotkey instead). Reset: dashboard button **and** a global hotkey — new `src/App/GlobalHotkeyService.cs` using `RegisterHotKey` on a message-only `NativeWindow` (no hotkey infrastructure exists today; overlay forms are `WS_EX_NOACTIVATE` and never get focus). Hotkey string in `Runes.ResetHotkey`, default `Ctrl+Alt+R`, empty disables.
5. **Scorer** — `src/Runes/RuneRowScorer.cs`: per row → `{ NewRunes, CarriedRunes, UnknownCount, Score }`; `IsBest` for the max-score rows when max > 0.
6. **Overlay** — extend `PricingOverlayRenderer.BuildTextSegments` in `src/Overlay/ConsoleOverlayRenderer.cs` (the live renderer; `PriceRowLayout` is test-only dead code — leave it) with one rune segment per row after the price: new-rune names in green (best row) / orange (positive, not best), `↻ carried` grey when nothing new, `? new rune` amber for unknowns. Dedicated fixed colour rule, **not** `GetPriceColor` (its auto-thresholds are price-scaled). Rows with no gilded rune render no segment. Extend `LeaguePricingWorker.ComputeSnapshotHash` and `PricingOverlayRenderer.BuildContentHash` to include rune keys and a carried-set version — today both ignore `RuneRows`, so rune-only changes would never re-render.
7. **Dashboard "Rune Library"** — Dashboard is its own assembly (`src/Dashboard/Dashboard.csproj`, referenced *by* the app, cannot see `Contracts`), so define `RuneLibraryEntryView` there. Push via `DashboardService` with `Dispatcher.InvokeAsync` like `SetStatus`; edits flow back through `Action` callbacks like `SetReRunSetupTrigger`. UI: new `SectionHeader` "Rune Library" in the settings `StackPanel` of `DashboardWindow.xaml`, an `ItemsControl` bound to an `ObservableCollection` (mirror the `LogList`/`LogEntries` pattern — the only binding precedent). Per entry: sprite (`BitmapSource.Create(32,32,96,96,PixelFormats.Rgb24,null,bytes,96)`, shown 2× with `NearestNeighbor`), editable name `ComboBox` seeded with shipped names, weight box (reuse the `ScanIntervalBox` numeric pattern + `QueueAutoSave`), carried toggle, seen count, "unweighted" badge. Plus "Reset carried runes" button and the hotkey box.
8. **Options** — `src/Configuration/RunesOptions.cs` bound to a new `"Runes"` section in `Program.cs` (`AddOptions<>().Bind`, consumers take `IOptionsMonitor<>`); defaults (`ResetHotkey`, `UnknownRuneWeight`, `OverlayStyle`) added to the `AppSettingsBootstrapper` default schema so `DeepMergeDefaults` back-fills existing installs. Register `RuneCatalog`, `CarriedRuneSet`, `GlobalHotkeyService` as singletons next to the existing ones in `Program.cs`.

**Hard-to-reverse:** the catalog JSON schema and id scheme (named runes: slug `opulent`; discovered: `k-<hash hex>-<hue>` until named, id preserved on naming so the carried list and history survive), the user-layer file location, and the match tolerance.

**Open questions (non-blocking) — using defaults:** overlay text style → names (mockup decision 1); carried-set marking → dashboard toggle (decision 2); hotkey default `Ctrl+Alt+R`.

## Acceptance criteria

- [ ] Every gilded key seen appears once in the library within one scan, with its sprite and seen count; re-seeing it bumps the count instead of adding a row (matcher tolerance).
- [ ] Binding a discovered rune to a shipped name (or a new name) and editing its weight persists to `config/rune-catalog.json` and survives restart and app update (shipped file untouched).
- [ ] Rows render a rune segment naming new runes; the best row(s) are visually distinct; carried-only rows show as redundant; unknown runes show as new-unweighted and still contribute `UnknownRuneWeight`.
- [ ] Toggling carried, or a rune-only change on screen, re-renders the overlay without a name/price change.
- [ ] Reset via button and via the configured hotkey empties the carried set; hotkey disabled when empty.
- [ ] Rows with no gilded rune, and snapshots with `RuneRows == null`, render exactly as today.
- [ ] Shipped `rune-catalog.json` contains Opulent 3, Power 2, Bond/Time/Death/Rebirth 1.

## Recommended Tests

- Unit: `RuneKeyMatcher` (tolerance edges, hue mismatch), `RuneCatalog` merge (user wins by id, unknown added, save round-trip), `RuneRowScorer` (carried exclusion, unknown weight, best-row ties), segment builder colour/text rules, snapshot/content hash changing on rune-only edits.
- Fixture: feed `tests/fixtures/runeicons/2560x1440/1 Raw.png` through reader → scorer with a synthetic catalog; assert the two identical-rune rows resolve to one library entry.
- Manual: dashboard library edit → overlay updates live; hotkey reset in-game (borderless window).

## Out of scope

- Automatic carried-set capture from the in-world remnant socket bar (the game shows the inherited rune there — best future source; needs its own capture region). Automatic run-boundary detection. Both later cards.
