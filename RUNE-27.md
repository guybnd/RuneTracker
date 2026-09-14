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
