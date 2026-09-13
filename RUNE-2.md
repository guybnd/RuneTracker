---
id: RUNE-2
title: Score combination rows against carried succession runes
status: Grooming
priority: Medium
effort: M
assignee: unassigned
tags:
  - feature
  - ui-ux
createdBy: Agent
updatedBy: Guy
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
---
Carved from the SCRATCH-1 discussion. The pinned DESIGN RECORD v2 and its ADDENDUM on SCRATCH-1 are the full rationale, including rejected alternatives — read them before changing any decision here.

This is **Card B of two**. It is a pure consumer of **RUNE-1** ("Extract and fingerprint succession runes from the discarded icon strip"), which supplies a per-rune identity key `(shape dHash, hue bucket)` for every gilded rune in each row of the Runeshape Combinations panel.

**Blocked on RUNE-1.** RUNE-1 carries a validation spike that can invalidate the whole approach; do not start this card until that spike passes.

## Goal

Tell the player which combination row is the best pick right now, by scoring each row on the succession runes it grants that they are **not already carrying** this run.

## Scoring model

```
rowScore = Σ weight(rune) for each gilded rune in the row NOT already in the carried set
```

Highest-scoring row wins. Because `rowScore` is a single scalar, feed it straight into `src/Overlay/PriceColorCalculator.cs` → `GetPriceColor`, which is a threshold-driven red → orange → green lerp over one `decimal`. This inherits the app's existing visual language for free. `src/Overlay/PriceRowLayout.cs` already handles per-row positioning, and `src/Overlay/DebugOverlayService.cs` already draws per-row rectangles, so rendering is GDI+ `Graphics` work on established machinery.

## Weight catalog

Weights come from a **hand-authored default table shipped with the app, user-editable**. This was the user's explicit choice over: dashboard-only manual assignment, deriving from poe2scout/poe.ninja pricing, and colour-tier-only.

### The catalog needs a naming layer

RUNE-1's identity key is opaque — nobody hand-authors weights against a hash string. Entry schema:

```
{ id, displayName, spriteRef, dHash, hueBucket, weight }
```

Auto-discovery (from RUNE-1's output) populates `dHash`, `hueBucket`, `spriteRef`. A human supplies `displayName` and `weight` once.

- Ship the defaults JSON alongside `ocr/unique-category-map.json`.
- Write user overrides to the **config directory**, so an app update cannot clobber user edits.

### Unknown keys must degrade gracefully

A new league will introduce runes absent from the shipped table. An unknown key must render as **"discovered but unweighted"** and be surfaced in the dashboard for the user to weight.

It must **not** silently score 0 — that would make a new high-value rune look worthless, which is strictly worse than showing nothing. This makes auto-discovery a permanent fallback, not bootstrap scaffolding.

### Open, non-blocking: no authoritative weight data

No authoritative weight data is currently available. Plan: seed the shipped table from hue/tier as placeholders so the feature works end to end, and flag the values as needing user input or a community data source. **Schema and code are identical either way**, so this does not block implementation — it only affects the quality of the first release's numbers. Resolve during grooming; ask the user directly if they can supply rankings.

## Scope

1. Catalog schema, shipped defaults file, user-override file in the config dir, merge/precedence between them.
2. Auto-discovery sink: unseen keys from RUNE-1 banked with their cropped sprite, marked unweighted.
3. Dashboard surface listing discovered runes with their sprite and an editable weight field (see `src/Dashboard/DashboardWindow.xaml` / `.xaml.cs` and `DashboardViewModel.cs` for existing settings patterns).
4. Per-run carried set, with a **manual reset hotkey**.
5. `rowScore` computation and per-row overlay rendering via `PriceColorCalculator` + `PriceRowLayout`.

## Out of scope

- Automatic run-boundary detection. No session concept and no `Client.txt` / zone parsing exists in `src/` today; it would be a new subsystem. Deferred to a later card — the manual reset hotkey is the MVP.
- Overlay on the in-world remnant socket bar (the horizontal 5-socket bar, distinct from the Combinations panel). Needs its own region resolution and detection. Deferred.
- Anything touching icon extraction, gilded detection, or hashing — that is RUNE-1.

## Context

Repo is already a fork: `origin` = `guybnd/RuneshapePriceChecker`, `upstream` = `Barragek0/RuneshapePriceChecker`.
