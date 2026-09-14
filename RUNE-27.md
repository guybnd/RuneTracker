---
id: RUNE-27
title: 'Island Rumours checker: tier the Uncharted Waters rumour list on screen'
status: In Progress
priority: Medium
effort: L
assignee: unassigned
tags:
  - ocr
  - overlay
createdBy: Agent
updatedBy: Agent
history:
  - type: activity
    user: Agent
    date: '2026-09-14T06:08:55.543Z'
    comment: Created ticket.
    id: a-2026-09-14t06-08-55-543z
  - type: activity
    user: Agent
    date: '2026-09-14T06:17:49.760Z'
    comment: >-
      ## Spike result: the OCR risk is closed, and not the way I expected


      Windows OCR reads the block-font parts of the panel perfectly every time —
      `USE A LOGBOOK TO CHART THE AREA`, `ISLAND RUMOURS`,
      `CONSUMES:`/`REQUIRES:`, `Expedition Logbook`. It makes a mess of the
      handwritten rumour lines:


      ```

      Nothin' to drink...  ->  "Lodoiw' to c(riwk.."   /  "no&viw' to drink.." 
      /  "now' to drink.."

      Somethin' fishy...   ->  "Sovuedoiw' fisbv..."   /  "Some&viw' fishy..."

      Warm but risky...    ->  "Waru but risklå..."    /  "Wanw but riskb..."

      Stardrinker...       ->  "Stardriwker..."        /  "Starc(riwker..."

      It's dry at least... ->  "Lt's at least..."       (the word "dry" is
      dropped entirely)

      Cold as ice... / Sulphite!  ->  exact

      ```


      That looked fatal, but it is not, because the job is not to read the text
      — it is to pick which of nineteen known strings it is. Only the argmin has
      to be right, not the distance. Ran every line the engine returned for the
      four captures (1x and 2x) against the table: **20/20 correct, minimum
      margin 3 edits**. Upscaling does not help and sometimes hurts, so the
      pipeline reads at 1x.


      No Tesseract, no preprocessing, no character-confusion map needed. The
      closest two rows in the table are 8 edits apart and the worst correct
      match measured 8, so the guards are `MaxDistance = 9` and `MinMargin = 2`:
      anything outside them is reported as unrecognised rather than guessed at,
      which is how a rumour whose printed string is an unrecorded alias will
      surface.


      ## Built


      - `ocr/rumour-tiers.json` — 19 rows, embedded resource. Aliases carry the
      strings the game actually prints: `Warm but risky` (It's Warm), `Nothin'
      to drink`, `Somethin' fishy`.

      - `RumourTable` — load + fuzzy match with the two guards.

      - `RumourPanelReader` — pure. Finds the panel by anchor (subtitle / header
      / footer), takes the rumour lines by geometry rather than by the engine's
      reading order, which is screen-wide and unordered. The subtitle doubles as
      the panel's width because the header sometimes reads clipped (`'ISLA N D
      RU`).

      - `OcrLine` + `WindowsOcrEngine.RecognizeLines` — full per-line boxes,
      which `Recognize` was discarding down to an averaged Y. Needed both to
      group the panel's lines and to place badges later.

      - `tests/fixtures/rumours/` — the four captures cropped to the top-left
      1400x900, keeping the act bar, search box and map as the noise the panel
      is picked out of.


      End to end on all four: panel found, every rumour named, boxes returned,
      ~80 ms per pass.


      ```

      "Cold as ice..."      -> Cold as ice (d=0)      at
      {X=1021,Y=358,W=144,H=30}

      "no&viw' to drink.."  -> Nothing to drink (d=3) at
      {X=992,Y=410,W=202,H=31}

      "Stardriwker..."      -> Stardrinker (d=1)      at
      {X=1019,Y=467,W=148,H=31}

      ```


      34 new tests; full suite 1085 green. Uncommitted in the working tree on
      master.


      ## Left


      Capture service + trigger (hotkey first), the badge overlay, settings, and
      a rumour library view. Still only 7 of 19 rumours seen on screen — the
      other 12 could each hide an alias like `Warm but risky`, and will surface
      as unrecognised until captured.
    summary: >-
      Spike resolved the OCR risk (argmin over a closed 19-row set beats bad
      OCR, 20/20). Built the table, matcher, panel reader,
      OcrLine/RecognizeLines and fixtures; 34 new tests, 1085 total green.
      Remaining: capture service, hotkey/trigger, overlay, settings.
    pin: true
    id: a-2026-09-14t06-17-49-760z
  - type: activity
    user: Agent
    date: '2026-09-14T06:28:09.739Z'
    comment: >-
      Added `tests/RumourSimulator` so the reader can be exercised on any
      screenshot without a game window — the same path the overlay will use,
      printing tier, map and mods per line plus the raw OCR string.


      Run over the whole capture drop folder: 4/78 captures held a panel, the
      four rumour ones, every rumour named correctly. The other 74 (rune rows,
      price panels, cropped strips) all reported no panel — no false positives.
      Full-frame 2560x1440 costs ~150 ms, the 1400x900 fixtures ~60-115 ms.


      Needed `<Compile Remove="RumourSimulator\**\*.cs" />` in both test csproj
      files: `tests/` is globbed by the test project, so a new exe folder under
      it collides on generated AssemblyInfo.


      Suite still 1085 green.
    id: a-2026-09-14t06-28-09-739z
---
## What

When the Uncharted Waters panel is open on the world map, read the `ISLAND RUMOURS` list and mark each rumour with its tier (S+ / A+ / A / B / C / D), plus the map type and mods it implies.

## Why

Rumours vary wildly in value (Fallen Stars → Runestones is S+; Wild, Roaming Free → Azmeri Spirits is D) and the panel gives you nothing but flavour text. The tool already reads the game screen and marks value in place for runes; this is the same idea for logbook charting.

## Shape

Reuses the established path from `RuneTooltipMarkService`: capture an arbitrary screen region → Windows OCR → fuzzy-match against a catalog → draw a click-through overlay in place.

1. **Locate** — anchor on the `ISLAND RUMOURS` header; read every line between it and the `CONSUMES:` / `REQUIRES:` footer. Panel position varies (centred above the clicked node, or jammed into a screen corner) and the rumour count varies (2 or 3 observed), so anchor + delimiter beats any fixed geometry or fixed line count.
2. **Match** — normalised edit distance against the tier table, with a small alias list for the strings that don't resemble their canonical name (`Warm but risky…` → `It's Warm` is the only one found so far; `Nothin' to drink…` → `Nothing to drink` and `Somethin' fishy…` → `Something Fishy` are distance 1 and need no alias).
3. **Data** — `ocr/rumor-tiers.json`: canonical name, aliases, map type, mods, rating. Same shape as `ocr/rune-combinations.json`.
4. **Display** — tier badge at the end of each line, coloured by rating, ★ on the best of the list; map type and mods in a detail view. Reuses `RuneMarkerOverlay`'s click-through layered window.

Windows OCR returns a `BoundingRect` per word, but `WindowsOcrEngine.Recognize` currently keeps only an averaged Y. Needs an overload returning full line rects so badges can be placed exactly.

## Risk to settle first

The rumour lines are in the game's handwritten italic parchment font — nothing like the block text the current pipeline reads. If Windows OCR can't read it, this needs the Tesseract path with preprocessing instead, which changes the cost of the whole feature. **Spike this before building anything else.**

## Fixtures

Four real captures in `E:\Git\RuneshapeCaptures\incoming\2026-09-14-island-rumours\`: two counts (2 and 3 rumours), three panel positions (centred, left, top-left corner), both footer wordings, one non-standard resolution (2292x1392), seven of the ~19 rumours.

## Tier table (from the user)

| Rumour | Map | Mods | Rating |
|---|---|---|---|
| Fallen Stars | Moor | Runestones | S+ |
| Cold as ice | Frigid Bluffs | Old Expedition | A+ |
| Nothing to drink | Stagnant Basin | Oil | A |
| Endless Cliffs | Craggy Peninsula | Rarity/Rogue Exiles | A |
| Sulphite! | Scorched Cay | Increased Rarity | A |
| Unknown Ruins | Exhumed Ruins | Precursor Leylines | B |
| Something Fishy | Bleached Shoals | Gold | B |
| It's Warm | Lush Island | Exp/Beyond/Hoards | B |
| Bleak and Awful | Barren Atoll | Strongbox | B |
| It's Dry At Least | Sloughed Gully | Monster effectiveness | D |
| Wild, Roaming Free | Grazed Prairie | Azmeri Spirits | D |
| Reflective Waters | Lake of Kalandra | Ring Bases | A (unique) |
| All that Glitters | Castaway | Gold | A (unique) |
| Almost paradise | Untainted Paradise | Exp | C (unique) |
| A good fellow | Moment of Zen | Seer | C (unique) |
| Origin of the Fall | Obscure Island | Olroth | A (boss) |
| Stardrinker | Secluded Temple | Uhtred | A (boss) |
| Last To Fall | Mournful Cliffside | Vorana | B (boss) |
| End of the Circle | Sprawling Jungle | Medved | B (boss) |
