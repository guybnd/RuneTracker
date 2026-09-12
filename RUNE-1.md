---
id: RUNE-1
title: Extract and fingerprint succession runes from the discarded icon strip
status: Grooming
priority: Medium
effort: L
assignee: unassigned
tags:
  - feature
  - spike
createdBy: Agent
updatedBy: Agent
history:
  - type: activity
    user: Agent
    date: '2026-09-12T14:37:40.190Z'
    comment: Created ticket.
---
Carved from the SCRATCH-1 discussion. The pinned DESIGN RECORD v2 and its ADDENDUM on SCRATCH-1 are the full rationale, including rejected alternatives — read them before changing any decision here.

This is **Card A of two**. Card B ("Score combination rows against carried succession runes") consumes this card's output and carries almost no technical risk. **All the risk lives here** — if the validation spike below fails, Card B should not be built as designed.

## Goal

In Path of Exile 2's Runeshape/Expedition remnant panel, gold/gilded-bordered runes are *succession runes*: they carry forward to the next remnant in the chain. Which runes are gilded is randomised per remnant. This card produces a stable per-rune identity for every gilded rune visible in the Runeshape Combinations panel, so Card B can tell the player which rows grant runes they are not already carrying.

This card delivers identification only. No scoring, no overlay, no run state.

## Key finding — the icons are already captured and deliberately thrown away

`src/OCR/OcrLeagueWindowReader.cs:655-664` shifts the scan region's left edge from the icon column to the text column:

```csharp
var textColX = (int)(preprocessed.Width * options.PanelLeftFraction);
var newX = Math.Max(crop.Value.X, textColX);
```

`PanelLeftFraction` defaults to `0.30` (`src/OCR/OcrOptions.cs:48`). The existing comment states the intent: stop rune icons inflating row widths and confusing row detection.

Consequence: for every row the pipeline already segments, `x ∈ [0, textColX)` **is** the 5-icon strip. It is captured, preprocessed and row-aligned today, then discarded.

**No new capture path, no new `OcrResolutionProfiles` entry, and no new `LeaguePanelDetector` work is required.** Do not add any.

## Hard constraint — sample from the raw frame, not the preprocessed one

Sample the icon strip from `capturedBitmap` (the raw frame, the one written as `1 Raw.png` when `OcrOptions.SaveDebugImages` is on), **not** from `preprocessed`.

`OcrImagePreprocessor` binarizes and colour-filters for text (`IsLikelyTextColor`, `src/OCR/OcrImagePreprocessor.cs:604`). That destroys exactly the hue information this card depends on for both gilded-border detection and tier classification. Use the row rectangles (`rowYs` / `rowHeights`) derived from the preprocessed pass, but read pixels from the raw frame.

## Identity key

```
key = (greyscale dHash of the normalised glyph, dominant hue bucket)
```

The dHash captures **shape**; the hue bucket captures **tier / sub-tier colour**. Both are required: the user has confirmed that tiers have sub-tiers and that specific rune shapes carry weight independently.

It is **not known** whether the same glyph shape can roll at different tiers. The composite key is deliberately more general than may be necessary so that this unresolved game-mechanics question does not block implementation — if weight turns out to be a pure function of shape, the hue component is simply constant per shape and harmless. **Do not re-litigate this at grooming and do not "simplify" the key down to shape alone.**

### Normalise before hashing — correctness requirement, not an optimisation

Rescale each icon cell to a fixed size (32×32 suggested) **before** computing the dHash. Without this, hashes differ across the five entries in `src/OCR/OcrResolutionProfiles.cs` (1600x900, 1920x1080, 2560x1440, 3440x1440, 3840x2160), and the weight table Card B ships would only work for users playing at the resolution it was authored at.

## Scope

1. Slice `x ∈ [0, textColX)` of each detected row rect into 5 equal icon cells, sampled from the raw bitmap.
2. Gilded-border detection per cell via border hue/luminance sampling. Non-gilded cells are dropped immediately — this reduces work to ~1-2 cells per row instead of 5.
3. Normalise each surviving cell to fixed size; compute greyscale dHash; compute dominant hue bucket.
4. Emit `(shape, hue)` keys per row, in row order, on a contract Card B can consume.
5. Debug support: extend the existing `SaveDebugImages` output with the sliced cells and their computed keys, so failures are diagnosable without attaching a debugger.

Implement dHash directly (~40 lines). **Do not add an image-processing or ML dependency** — everything here is pixel statistics.

## Validation spike — do this FIRST

Any of these failing invalidates the approach and must be reported back before further implementation:

- **(a)** Does the icon strip fall inside the capture region at **all five** profiles in `src/OCR/OcrResolutionProfiles.cs`? `PanelLeftFraction` is a fraction of the capture width, so this is an assumption, not a guarantee.
- **(b)** Are gilded borders reliably separable from non-gilded ones in the raw frame, across the tier colours (blue / purple / gold glyphs)?
- **(c)** Is the dHash stable frame-to-frame for the same sprite at a fixed resolution profile, and after normalisation, stable *across* profiles?

Cheapest harness: `OcrOptions.SaveDebugImages` plus `tests/OcrPricingSimulator/`. Iterate there rather than by running the game.

If (a) fails: report which profiles fail and by how much before proposing a capture-region change — widening capture affects the existing price OCR path and is out of scope for this card.

## Out of scope

- Any scoring, weighting, catalog, or overlay work — that is Card B.
- Run-boundary detection. No session concept and no `Client.txt` / zone parsing exists anywhere in `src/` today. Deferred to a later card.
- Overlay on the in-world remnant socket bar (the horizontal 5-socket bar, distinct from the Combinations panel). It needs its own region resolution and detection. Deferred.

## Context

Repo is already a fork: `origin` = `guybnd/RuneshapePriceChecker`, `upstream` = `Barragek0/RuneshapePriceChecker`.
