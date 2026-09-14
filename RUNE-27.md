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
  - type: activity
    user: Agent
    date: '2026-09-14T06:45:19.892Z'
    comment: >-
      ## The overlay exists now


      `Alt+C` (configurable, `Rumours:ReadHotkey`) captures the client, OCRs it,
      and marks whatever it finds. Badges sit in one column beside the panel —
      tier letter in its tier colour, map and mods beside it, ★ on the best of
      the list — and take themselves down after `HoldSeconds` (12), since
      nothing tells the tool when the panel closes.


      - `RumourReadService` — capture + read, own OCR engine, single-flight,
      logs every line with its match distance and unmatched ones at warning.

      - `RumourOverlay` / `RumourBadgePainter` — click-through layered form,
      pure painter beside it.

      - `RumoursOptions`, DI, hotkey binding, appsettings defaults.


      ## Map tooltips too


      A charted node has no rumour wording left, but the tier list pairs each
      rumour with exactly one map, so the node's own tooltip identifies it.
      `RumourMapTooltipReader` finds the title as the line above the `Biome:`
      anchor and `RumourTable.MatchMap` names it. Verified on a real capture:
      `SLOUGHED GULLY` → It's Dry At Least, D, Monster effectiveness.


      Map names get a tighter bar than rumour lines (`MaxMapDistance = 3` vs
      `MaxDistance = 9`) — they are block small caps and read all but perfectly,
      so a loose match there would be a wrong answer rather than a rescued one.


      ## Caught by rendering it


      Added `--render <dir>` to the simulator, which draws the marks onto the
      captures. First render showed the badges alternating sides down the list:
      a panel is only as wide as its longest rumour, so near the screen edge the
      short lines had room on the right and the long ones did not. Replaced
      per-line placement with `LayoutColumn`, which picks one side for the whole
      panel and shares an edge. Would not have been visible from tests alone.


      ## State


      1113 tests green (28 new). Published to `obj/Release/publish` at 16:44 —
      the Release gate ran the suite again before packaging. Still uncommitted
      on master.


      Left: settings UI, a rumour library view, and the 12 rumours still never
      seen on screen.
    summary: >-
      In-game path built and published: Alt+C reads the screen and marks either
      the rumour list or a charted node's map tooltip with tier badges. Map
      identification added via RumourMapTooltipReader + RumourTable.MatchMap.
      Column layout fixed after a render check showed alternating sides. 1113
      tests green, published to obj/Release/publish.
    pin: true
    id: a-2026-09-14t06-45-19-892z
  - type: activity
    user: Agent
    date: '2026-09-14T07:01:58.821Z'
    comment: >-
      ## No hotkey needed any more


      Measured first, three passes each: full client 2560x1440 is **~230 ms**, a
      cursor-anchored region 1280x648 is **~50 ms**, a tight 700x560 is **~32
      ms**. Scanning the full client on a timer was never viable — 1 Hz would be
      a fifth of a core burned forever, on top of the pricing loop already at
      100 ms.


      `RumourWatchService` instead watches the cursor, which costs nothing to
      ask:


      - poll the position (no capture, no OCR)

      - read only when it has come to rest (`SettleMs`, 200) somewhere it has
      not already been read

      - read the region around the cursor, not the client


      Sweeping the mouse triggers nothing, because it never settles. Leaving it
      parked triggers nothing, because it has not moved. Both panels are drawn
      against the node under the pointer, so nothing is lost by not looking
      elsewhere — `CursorRegion` takes three fifths of the client each way,
      which covers the rumour list drawn *above* the node and a charted tooltip
      drawn *below* it.


      Two guards worth noting: a `Busy` result (the hotkey mid-read) does not
      mark the resting place as done, so the next tick retries; and a sighting
      identical to the last is not redrawn, or every mouse twitch would restart
      the hold timer.


      ## The coordinate change this forced


      Reading a region rather than the client means the reader's boxes are
      relative to that region, not the screen. `ReadCore` now shifts every box —
      readings and the panel's own bounds — back into client space before
      returning, since the overlay covers the client. Pinned by a test that
      asserts the shift happens for a cursor read and does not for a full-client
      read.


      New settings: `AutoScan` (true), `PollMs` (100), `SettleMs` (200). The
      hotkey stays as the manual re-read.


      1122 tests green (9 new). Republished to `obj/Release/publish` at 17:01.
      Still uncommitted on master.
    summary: >-
      Auto-scan added: RumourWatchService triggers on the cursor settling rather
      than a timer, reading a cursor-anchored region (50 ms) instead of the
      whole client (230 ms). Hotkey kept as manual re-read. Boxes now shift from
      region to client coordinates. 1122 tests green, republished 17:01.
    pin: true
    id: a-2026-09-14t07-01-58-821z
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
