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
  - type: comment
    user: Agent
    comment: >-
      **Catalog seed data (2026-09-13).**


      **Glyph colour is the tier**, confirmed from the user's in-game tooltip
      screenshots of the socket bar: gold glyph = Opulent; purple glyphs = Oath,
      Time, Bond; blue glyphs = Ward, Rage, Volcanic, Vision, Stone. RUNE-1's
      `HueBucket` already carries this (bucket 1 gold/warm, 9 purple, ~7 blue),
      so an unknown key can default its weight from colour: **gold 3, purple 1,
      blue 0.5** — overriding the flat `UnknownRuneWeight`. User-specified:
      Power 2.


      **Full runeshape alphabet** for the shipped `rune-catalog.json` (names +
      effect text per [Game8's
      list](https://game8.co/games/Path-of-Exile-2/archives/603196), 33
      entries): Adaptive (Adaptation) · Arcane (Extra Energy Shield, stunning
      nova when ES depleted) · Bloodletting (Life Leech, cannot be leeched,
      Corrupted Blood on hit) · Bond (Rare Monsters may transfer a Mod on death)
      · Celestial (Fire/Cold/Lightning explosion on death) · Cold (Extra Cold
      Damage) · Cyclonic (Exposure, Armour Break, Wither on hit) · Death (Slain
      Monsters may merge into stronger Monsters) · Earth (Conjures Earthly
      Spires) · Electrocuting (Extra Lightning, Electrocute, Shocked Ground) ·
      Fire (Extra Fire Damage) · Life (Shared Life) · Lightning (Extra Lightning
      Damage) · Momentum (Movement Speed, cannot be slowed below base) · Moon
      (Conjures moon beams) · Oath (A Monster summons Allies) · Opulent
      (Increases Monster Rarity) · Power (Empowered) · Prismatic (Always Shock,
      all damage can Shock/Chill, all Ele Res, damage as random element) ·
      Protective (Verisium Proximity Shields) · Rage (Periodically Enrage) ·
      Rebirth (Chance to Rebirth on death) · Sky (Elemental Tornados) · Soul
      (Union of Souls) · Stone (Armoured, Stun Threshold, Earthly Prison) ·
      Tempest (cannot be Shocked/Chilled, all damage can Shock/Chill) · Tidal
      (Tidal Waves) · Time (Slain Monsters may respawn as higher Rarity) · Toxic
      (Poison, Toxic Volatiles) · Vision (Reflect Curses/Shock/Chill) · Volcanic
      (Extra Fire, Ignite, Burning Ground) · Ward (Protected by Runic Ward) ·
      Wisdom (Increased Experience).


      Sprites still have to be bound to names by the user in the library (the
      tooltip renders glyphs glowing on dark, not on the panel parchment, so
      they cannot be matched automatically). Socket-bar tooltip wording
      confirming the mechanic: "The Runic Modifier in this slot will be added to
      all Monsters unearthed after this Remnant."
    date: '2026-09-13T04:34:39.060Z'
    selfAttested: true
    summary: >-
      Full runeshape alphabet (33 names + effects, from Game8) to seed the
      shipped catalog, and glyph colour = tier confirmed from user tooltips:
      gold Opulent; purple Oath/Time/Bond; blue Ward/Rage/Volcanic/Vision/Stone.
      Default weight by tier for unknown keys: gold 3, purple 1, blue 0.5;
      user's Power 2. Socket-bar tooltip text confirms the succession slot
      semantics.
    pin: true
    id: c-2026-09-13t04-34-39-060z
  - type: activity
    user: Agent
    date: '2026-09-13T04:36:27.813Z'
    comment: Updated description.
    id: a-2026-09-13t04-36-27-813z
---
> **TL;DR** — RUNE-1 now tells us, per Combinations row, which gilded (carry-forward) runes it grants, as stable keys with sprites. This card turns that into the thing the player actually wants: a **rune library** in the dashboard listing all 33 runes with tier colour and weight (Opulent > Power > the rest), where each sprite the tool sees gets bound to its rune once; a **carried-this-run set** with a reset; and an **overlay line per row** saying which new runes that row grants and which row is the best pick.

**Card B of two.** Pure consumer of **RUNE-1** (Ready). Rationale and rejected alternatives: SCRATCH-1's pinned DESIGN RECORD v2 + ADDENDUM. User's weight input and the full rune alphabet are the pinned comments on this ticket.

## Problem / Motivation

Succession runes carry forward along a remnant chain; picking one already carried wastes the slot. The player eyeballs this today. With per-row rune keys in hand, the tool can rank rows and name the runes — but only if the opaque keys get bound to the known rune names and the player has a place to prioritise them ("a library I can prioritize against, possibly a nice UI in the app").

## Locked decisions (do not re-open)

- **Score:** `rowScore = Σ weight(rune)` over the row's gilded runes **not** in the carried set. Highest wins.
- **The catalog is the fixed list of 33 runeshapes** (user, 2026-09-13: "finite number of runes, we just need to list them out"). Shipped with name, effect text, tier colour where known (gold: Opulent; purple: Oath, Time, Bond; blue: Ward, Rage, Volcanic, Vision, Stone) and weight. Seed weights: Opulent 3, Power 2, other purple/"high value" 1, blue 0.5; user-editable. No open-ended "new rune" concept.
- **Sprites are bound, not discovered.** Shipped entries start without hashes. A gilded key with no binding shows as an **unbound sprite** in the library until the user picks which of the 33 it is; meanwhile it scores by glyph colour via `HueBucket` (gold 3, purple 1, blue 0.5) and the overlay marks it `?`. Never a silent 0.
- **Key matching is tolerant:** same `HueBucket` and Hamming(`ShapeHash`) ≤ 8 (RUNE-1: same rune 1–3 bits, distinct 22+).
- **No OS config dir exists in this app** — state lives beside the exe (`AppContext.BaseDirectory/config/appsettings.json`, `…/ocr/`). The catalog's user layer goes in `…/config/rune-catalog.json`.

## Implementation plan

1. **Matcher** — `src/Runes/RuneKeyMatcher.cs`: `IsSame(RuneKey, RuneKey)` per the tolerance; `FindBinding(key, bindings)`. Pure, unit-tested.
2. **Catalog + persistence** — `src/Runes/RuneCatalog.cs`. Shipped `ocr/rune-catalog.json` (`EmbeddedResource`, loaded like `ItemNameParser.LoadBaseTypeKeywords` loads `unique-category-map.json`: resource-name `EndsWith` + dev-time disk fallback): 33 entries `{ id (slug), displayName, effect, tier?, weight }`. User layer `config/rune-catalog.json`: `weights` overrides by id, `bindings` (`{ shapeHash, hueBucket, spritePngBase64, runeId?, seenCount, firstSeenUtc, lastSeenUtc }` — `runeId` null while unbound), `carried` id list. Merge on load, user wins by id (same idea as `AppSettingsBootstrapper.DeepMergeDefaults`, one level). Saves debounced via `System.Text.Json`.
3. **Observe** — `RuneCatalog.Observe(RuneKey)`: match an existing binding → bump `seenCount`; else add an unbound binding with the sprite (PNG from the 32×32 RGB24 bytes). Called from `LeaguePricingWorker.ExecuteAsync` right after the snapshot is read, for every key in `snapshot.RuneRows`.
4. **Carried set** — `src/Runes/CarriedRuneSet.cs`: rune ids (an unbound binding is carried by its binding id and migrates on bind), persisted in the user layer so a restart mid-run keeps it. Marking: dashboard toggle per rune (mockup decision 2 offers a per-row hotkey). Reset: dashboard button **and** a global hotkey — new `src/App/GlobalHotkeyService.cs` using `RegisterHotKey` on a message-only `NativeWindow` (no hotkey infrastructure exists; overlay forms are `WS_EX_NOACTIVATE`). `Runes.ResetHotkey`, default `Ctrl+Alt+R`, empty disables.
5. **Scorer** — `src/Runes/RuneRowScorer.cs`: per row → `{ NewRunes, CarriedRunes, UnboundCount, Score }`; `IsBest` for max-score rows when max > 0.
6. **Overlay** — extend `PricingOverlayRenderer.BuildTextSegments` in `src/Overlay/ConsoleOverlayRenderer.cs` (the live renderer; `PriceRowLayout` is test-only — leave it) with one rune segment per row after the price: new-rune names in green (best row) / orange (positive, not best), `↻ <name> carried` grey when nothing new, `? unbound` amber. Dedicated fixed colour rule, **not** `GetPriceColor` (price-scaled auto thresholds). Rows with no gilded rune render no segment. Extend `LeaguePricingWorker.ComputeSnapshotHash` and `PricingOverlayRenderer.BuildContentHash` with rune keys and a catalog/carried version — both ignore `RuneRows` today, so rune-only changes would never re-render.
7. **Dashboard "Rune Library"** — Dashboard is its own assembly (`src/Dashboard/Dashboard.csproj`, referenced *by* the app; cannot see `Contracts`), so define `RuneLibraryEntryView` / `UnboundSpriteView` there. Push via `DashboardService` with `Dispatcher.InvokeAsync` like `SetStatus`; edits return through `Action` callbacks like `SetReRunSetupTrigger`. UI: new `SectionHeader` "Rune Library" in the settings `StackPanel` of `DashboardWindow.xaml`; an unbound-sprites strip (sprite + "bind to" `ComboBox` of the 33 names) above an `ItemsControl` of all 33 runes bound to an `ObservableCollection` (mirror the `LogList`/`LogEntries` pattern). Per rune: name + effect tooltip, tier dot, weight box (reuse the `ScanIntervalBox` numeric pattern + `QueueAutoSave`), bound sprite (`BitmapSource.Create(32,32,96,96,PixelFormats.Rgb24,null,bytes,96)`, 2× `NearestNeighbor`) or "not seen yet", seen count, carried toggle. Plus "Reset carried runes" and the hotkey box.
8. **Options + DI** — `src/Configuration/RunesOptions.cs` bound to a new `"Runes"` section in `Program.cs` (`AddOptions<>().Bind`; consumers take `IOptionsMonitor<>`); defaults (`ResetHotkey`, `TierWeights`, `OverlayStyle`) added to the `AppSettingsBootstrapper` default schema so `DeepMergeDefaults` back-fills existing installs. Register `RuneCatalog`, `CarriedRuneSet`, `GlobalHotkeyService` as singletons beside the existing ones.

**Hard-to-reverse:** the catalog JSON schema and id scheme (rune ids are slugs of the 33 names; binding ids `k-<hash hex>-<hue>`), the user-layer file location, and the match tolerance.

**Open questions (non-blocking) — using defaults:** overlay segment style → names (mockup decision 1); carried marking → dashboard toggle (decision 2); tier of Death/Power/Rebirth unknown until the user's sprites show their colour → weights as seeded.

## Acceptance criteria

- [ ] The library lists all 33 runes with effect text, tier colour and editable weight; shipped weights Opulent 3, Power 2, purple 1, blue 0.5.
- [ ] A gilded key seen for the first time appears once as an unbound sprite; re-seeing it bumps its count (matcher tolerance) instead of adding another.
- [ ] Binding a sprite to a rune, and editing a weight, persists to `config/rune-catalog.json` and survives restart and app update (shipped file untouched).
- [ ] Rows render a rune segment naming new runes; best row(s) visually distinct; carried-only rows shown as redundant; unbound sprites marked `?` and scored by tier colour.
- [ ] Toggling carried, binding a sprite, or a rune-only change on screen re-renders the overlay without a name/price change.
- [ ] Reset via button and via the configured hotkey empties the carried set; empty hotkey disables it.
- [ ] Rows with no gilded rune, and snapshots with `RuneRows == null`, render exactly as today.

## Recommended Tests

- Unit: `RuneKeyMatcher` (tolerance edges, hue mismatch), `RuneCatalog` (shipped 33 load, user merge wins by id, unbound→bound migration keeps carried state, save round-trip), `RuneRowScorer` (carried exclusion, tier default for unbound, best-row ties), segment text/colour rules, snapshot/content hash changes on rune-only edits.
- Fixture: `tests/fixtures/runeicons/2560x1440/1 Raw.png` through reader → catalog; assert the two identical-rune row pairs collapse to two bindings.
- Manual: bind a sprite in the dashboard → overlay names it live; hotkey reset in-game (borderless window).

## Out of scope

- Automatic carried-set capture from the in-world remnant socket bar (the game shows the inherited rune there — best future source; needs its own capture region). Automatic run-boundary detection. Both later cards.
