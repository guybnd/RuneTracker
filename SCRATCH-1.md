---
id: SCRATCH-1
title: Scratch 1
status: Todo
priority: None
effort: None
assignee: unassigned
tags: []
createdBy: Guy
updatedBy: Agent
kind: scratch
history:
  - type: activity
    user: Guy
    date: '2026-09-12T14:14:19.328Z'
    comment: Created ticket.
    id: a-2026-09-12t14-14-19-328z
  - type: agent_session
    sessionId: bc1b49fd-f934-462a-810d-056248483df0
    startedAt: '2026-09-12T14:14:52.799Z'
    status: waiting-input
    progress:
      - timestamp: '2026-09-12T14:15:08.714Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T14:15:23.722Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T14:15:38.735Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T14:15:53.745Z'
        message: Working
        type: info
      - timestamp: '2026-09-12T14:17:31.382Z'
        message: >-
          Go ahead — what are you looking to add?


          I'll hold off on sketching anything until I hear it. For context when
          you describe it, the pieces available to build on are:


          | Layer | Files |

          |---|---|

          | Capture | `src/OCR/Capture/OcrCaptureStrategy.cs` (PrintWindow / WGC
          / DXGI) |

          | Region resolution | `OcrCaptureRegionResolver.cs`,
          `Poe2WindowResolutionService.cs`, `OcrResolutionProfiles.cs` |

          | Panel detection | `LeaguePanelDetector.cs` |

          | Preprocessing | `OcrImagePreprocessor.cs` (682 lines),
          `OcrPipeline.cs` (row segmentation) |

          | Recognition | `WindowsOcrEngine.cs`, `NativeTesseractEngine.cs` |

          | Post-processing | `OcrTextPostProcessor.cs`,
          `Pricing/ItemNameParser.cs`, `ItemNameTranslator.cs` |

          | Orchestration | `OcrLeagueWindowReader.cs` (1238 lines — the god
          object) |


          There's also a test harness already in place at
          `tests/OcrPricingSimulator/`, which is usually the cheapest place to
          iterate on OCR changes without running the game.
    user: Claude Code
    date: '2026-09-12T14:14:52.799Z'
    enginePid: 9336
    outcome: Claude Code paused — waiting for user input.
    originalProgressCount: 5
    finalMessage: >-
      Go ahead — what are you looking to add?


      I'll hold off on sketching anything until I hear it. For context when you
      describe it, the pieces available to build on are:


      | Layer | Files |

      |---|---|

      | Capture | `src/OCR/Capture/OcrCaptureStrategy.cs` (PrintWindow / WGC /
      DXGI) |

      | Region resolution | `OcrCaptureRegionResolver.cs`,
      `Poe2WindowResolutionService.cs`, `OcrResolutionProfiles.cs` |

      | Panel detection | `LeaguePanelDetector.cs` |

      | Preprocessing | `OcrImagePreprocessor.cs` (682 lines), `OcrPipeline.cs`
      (row segmentation) |

      | Recognition | `WindowsOcrEngine.cs`, `NativeTesseractEngine.cs` |

      | Post-processing | `OcrTextPostProcessor.cs`,
      `Pricing/ItemNameParser.cs`, `ItemNameTranslator.cs` |

      | Orchestration | `OcrLeagueWindowReader.cs` (1238 lines — the god object)
      |


      There's also a test harness already in place at
      `tests/OcrPricingSimulator/`, which is usually the cheapest place to
      iterate on OCR changes without running the game.
  - type: comment
    user: User
    date: '2026-09-12T14:21:50.059Z'
    comment: "i will explain. the tool is for the game path of exile 2. in it there is a 'expedition' game mode and this mode contains various remnants scattered about. \n\nthis tool what it already achieves, is to let you pick the best reward for remnants, but there is a second mechanic to the remnants. they each add their selected mods (only the specially highlighted ones) to the next remnant after it in order. which makes the next one's monsters have the selected mod added to it\n\nto optimize the gameplay,. a player will want to have the most amount of runes added from the remnant, by the end of his run. since they can repeat, and theres less and more optimal ones, i want to add in this tool a way to \n1. show which remnants were already selected so we dont try to double dip\n2. show which ones are more valuable\n3. show on screen, in the selector menu, which ones we may have already picked so are now redundant, which ones have more value, estuff like that.\nit looks like such :\n\npic 1\n\npic 2 when the screen is opened, it shows you the selection of runes. then we can try to optimzie there to pick the correct ones. or show if they are not so valuable\nnote the runes with gilded border are the ones that can carry on. the runes themselves have a color where blue one is x value and puple is y value and gold is most value. between each one theres also more preferable ones ofc\n\n\U0001F4CE image.png, image-2.png"
    id: c-2026-09-12t14-21-50-059z
  - type: comment
    user: Agent
    comment: >-
      DESIGN RECORD — Succession rune tracking (PoE2 Runeshape/Expedition
      remnants). Not yet a ticket; awaiting user approval to extract.


      GOAL: In the Runeshape Combinations panel, help the player pick
      combinations that maximise succession runes accumulated across a remnant
      chain. Gold/gilded-bordered runes carry forward to the next remnant; which
      runes are gilded is RANDOMISED per remnant. Picking a gilded rune already
      carried is a wasted slot ("double dipping").


      KEY CODEBASE FINDING: the rune icon strip is already captured and then
      deliberately discarded.

      - src/OCR/OcrLeagueWindowReader.cs:655-664 shifts the scan region's left
      edge from the icon column to the text column: `var textColX =
      (int)(preprocessed.Width * options.PanelLeftFraction);` (PanelLeftFraction
      defaults 0.30, src/OCR/OcrOptions.cs:48). Comment there states the intent
      is to stop rune icons inflating row widths.

      - Consequence: x in [0, textColX) of each already-segmented row rect IS
      the 5-icon strip. No new capture path, no new OcrResolutionProfiles entry,
      no new LeaguePanelDetector work is required.

      - CONSTRAINT: sample that strip from `capturedBitmap` (the raw frame,
      saved as "1 Raw.png"), NOT from `preprocessed` — OcrImagePreprocessor
      binarizes and colour-filters for text, destroying the hue needed for
      gilded-border and tier detection.


      DECISION: no labelled rune catalog. Identity is only ever needed RELATIVE
      TO THE PLAYER'S OWN RUN HISTORY ("have I already taken this one?"), never
      as a name. So fingerprint each gilded cell with a perceptual hash
      (dHash/aHash, no new dependency) and compare against the set banked this
      run.

      RATIONALE: user confirmed the recipe space is large enough that
      maintaining a recipe/sprite catalog is impractical, and that the gilded
      subset is randomised per remnant so a static recipe table could not supply
      it anyway.

      REJECTED — static recipe table keyed by the already-OCR'd output name
      (e.g. "1x Farrul's Rune of Grace"): cheapest option but cannot work,
      because the gilded subset is rolled per remnant, not fixed per named
      output.

      REJECTED — full 5-icon sprite recognition with a labelled catalog:
      unnecessary; only gilded cells matter (1-2 per row, not 5), and names are
      never needed.


      PROPOSED MVP SCOPE:

      1. Slice x in [0, textColX) of each row rect into 5 cells, sampled from
      the raw bitmap.

      2. Gilded-border detection per cell via border hue/luminance sampling;
      drop non-gilded cells.

      3. Tier classification (blue/purple/gold) from glyph hue.

      4. dHash fingerprint per gilded cell; per-run banked set; manual reset
      hotkey.

      5. Overlay badge per row: redundant / new / tier. Rendering is near-free —
      src/Overlay/ConsoleOverlayRenderer.cs uses GDI+ Graphics and
      src/Overlay/DebugOverlayService.cs already draws per-row rectangles.


      VALIDATE FIRST (spike; either result invalidates the approach): (a) does
      the icon strip fall inside the capture region at ALL five profiles in
      src/OCR/OcrResolutionProfiles.cs; (b) are gilded borders reliably
      separable in the raw frame. Cheapest harness: OcrOptions.SaveDebugImages
      plus tests/OcrPricingSimulator/.


      EXPLICITLY DEFERRED to follow-ups: automatic run-boundary detection (no
      session concept and no Client.txt/zone parsing exists anywhere in src/
      today — would be a new subsystem); intra-tier value ranking (needs labels;
      suggested approach is user-taught labels attached to fingerprints already
      seen, built organically rather than pre-built); overlay on the in-world
      socket bar (needs its own region resolution + detection, unlike the
      combinations panel which is already segmented).


      CONTEXT: repo is already a fork — origin guybnd/RuneshapePriceChecker,
      upstream Barragek0/RuneshapePriceChecker.
    date: '2026-09-12T14:29:05.093Z'
    selfAttested: true
    summary: >-
      Design for succession-rune tracking: fingerprint gilded runes (no labelled
      catalog) from the icon strip the OCR pipeline currently discards; overlay
      redundant-pick warnings. Awaiting user go-ahead to extract a real ticket.
    pin: true
    id: c-2026-09-12t14-29-05-093z
  - type: comment
    user: User
    date: '2026-09-12T14:29:10.864Z'
    comment: >-
      the tier has sub tiers. specific rune shapes also carry weight. we need to
      differentiate between them
    id: c-2026-09-12t14-29-10-864z
  - type: comment
    user: Agent
    comment: >-
      DESIGN RECORD v2 — Succession rune tracking (PoE2 Runeshape/Expedition
      remnants). Supersedes the v1 record (c-2026-09-12t14-29-05-093z); v1's "no
      labelled catalog" decision is CORRECTED below. Not yet a ticket; awaiting
      user approval to extract.


      GOAL: In the Runeshape Combinations panel, help the player pick the
      combination row that maximises succession runes accumulated across a
      remnant chain. Gold/gilded-bordered runes carry forward to the next
      remnant; which runes are gilded is RANDOMISED per remnant. Picking a
      gilded rune already carried is a wasted slot.


      KEY CODEBASE FINDING (unchanged from v1): the rune icon strip is already
      captured and then deliberately discarded.

      - src/OCR/OcrLeagueWindowReader.cs:655-664 shifts the scan region's left
      edge from the icon column to the text column: `var textColX =
      (int)(preprocessed.Width * options.PanelLeftFraction);` (PanelLeftFraction
      defaults 0.30, src/OCR/OcrOptions.cs:48). The comment there states the
      intent is to stop rune icons inflating row widths.

      - Consequence: x in [0, textColX) of each already-segmented row rect IS
      the 5-icon strip. No new capture path, no new OcrResolutionProfiles entry,
      no new LeaguePanelDetector work required.

      - CONSTRAINT: sample that strip from `capturedBitmap` (raw frame, saved as
      "1 Raw.png"), NOT from `preprocessed` — OcrImagePreprocessor binarizes and
      colour-filters for text, destroying the hue needed for gilded-border and
      tier detection.


      CORRECTION TO v1: user states tiers have SUB-TIERS and specific rune
      SHAPES carry weight, so runes must be differentiated individually. v1
      concluded no labelled catalog was needed (identity only relative to run
      history). That is now WRONG: a per-shape weight requires labelling.
      However v1's underlying objection still resolves — catalogue the GLYPH
      ALPHABET, not the recipes. The user's concern was recipe count; the glyph
      alphabet is small, bounded and reused (in the supplied screenshot the same
      glyphs repeat at positions 2 and 4 across both rows), while it is the
      combinations of that alphabet that blow up. The perceptual-hash design
      therefore survives: a dHash already discriminates shape, which is exactly
      the differentiation required. What is ADDED is a weight attached to each
      distinct fingerprint.


      DECISION — identity key is composite: (greyscale dHash of the glyph,
      dominant hue bucket).

      RATIONALE: it is not known whether the same glyph shape can roll at
      different tiers. The composite key is strictly more general and costs
      nothing: dHash captures shape, hue captures tier/sub-tier. If weight is a
      pure function of shape, the hue component is constant per shape and
      harmless; if the same shape rolls at multiple tiers with different values,
      the key already separates them. This deliberately avoids blocking the
      build on an unresolved game-mechanics question — do NOT re-litigate this
      at grooming.


      DECISION — catalog is auto-discovered, not hand-captured: the tool banks
      every unseen (shape, hue) key it encounters together with the cropped
      sprite image, so no sprite has to be captured or cropped by hand. Only the
      WEIGHT per key is human-supplied. Source of weights is the one OPEN
      QUESTION put to the user (see the following comment/answer on this
      ticket).


      DECISION — the overlay is a per-row SCORE, not a binary redundant/not
      badge.
        rowScore = sum of weight(rune) over gilded runes in that row NOT already carried this run.
      Highest-scoring row wins. Because the score is a scalar it feeds directly
      into src/Overlay/PriceColorCalculator.cs GetPriceColor (a threshold-driven
      red->orange->green lerp over one decimal), inheriting the app's existing
      visual language; src/Overlay/PriceRowLayout.cs already handles per-row
      positioning. Rendering is GDI+ Graphics
      (src/Overlay/ConsoleOverlayRenderer.cs) and
      src/Overlay/DebugOverlayService.cs already draws per-row rectangles, so
      the overlay work stays cheap.


      REJECTED — static recipe table keyed by the OCR'd output name (e.g. "1x
      Farrul's Rune of Grace"): cannot work, the gilded subset is rolled per
      remnant, not fixed per named output.

      REJECTED — pre-built hand-captured sprite catalog: unnecessary manual
      work; auto-discovery supplies the sprites, leaving only weights to humans.

      REJECTED — detecting tier/sub-tier as a signal separate from shape:
      subsumed by the composite key above.


      PROPOSED MVP SCOPE:

      1. Slice x in [0, textColX) of each row rect into 5 cells, sampled from
      the raw bitmap.

      2. Gilded-border detection per cell (border hue/luminance sampling); drop
      non-gilded cells, so only ~1-2 cells per row are hashed.

      3. Composite key (greyscale dHash, hue bucket) per surviving cell.

      4. Auto-discovery store: unseen keys banked with their cropped sprite;
      weight assignable per key.

      5. Per-run carried set + manual reset hotkey.

      6. Per-row score overlay via PriceColorCalculator.


      VALIDATE FIRST (spike; either result invalidates the approach): (a) does
      the icon strip fall inside the capture region at ALL five profiles in
      src/OCR/OcrResolutionProfiles.cs; (b) are gilded borders reliably
      separable in the raw frame; (c) is the dHash stable frame-to-frame for the
      same sprite at a fixed resolution profile. Cheapest harness:
      OcrOptions.SaveDebugImages plus tests/OcrPricingSimulator/.


      EXPLICITLY DEFERRED to follow-ups: automatic run-boundary detection (no
      session concept and no Client.txt/zone parsing exists anywhere in src/
      today — a new subsystem); overlay on the in-world socket bar (needs its
      own region resolution + detection, unlike the combinations panel which is
      already segmented); cross-resolution fingerprint normalisation if hashes
      prove unstable between profiles.


      CONTEXT: repo is already a fork — origin guybnd/RuneshapePriceChecker,
      upstream Barragek0/RuneshapePriceChecker.
    date: '2026-09-12T14:31:05.074Z'
    selfAttested: true
    summary: >-
      v2 design for succession-rune tracking. Supersedes v2026-09-12T14:29:05Z:
      a glyph catalog IS needed after all (weights are per-shape), but
      catalogued by glyph alphabet not recipes. Identity key = (greyscale dHash,
      hue bucket). Overlay becomes a per-row score. Awaiting user decision on
      weight source.
    pin: true
    supersedes:
      - c-2026-09-12t14-29-05-093z
    id: c-2026-09-12t14-31-05-074z
  - type: comment
    user: Agent
    comment: >-
      DESIGN RECORD v2 ADDENDUM — weight source decided. Read alongside the v2
      record on this ticket. Still not a ticket; user has been asked to approve
      a two-card extraction.


      DECISION — weights come from a hand-authored default table shipped with
      the app, user-editable (user's explicit choice over: dashboard-only manual
      assignment; deriving from poe2scout/poe.ninja pricing; or
      colour-tier-only).


      CONSEQUENCE 1 — the catalog needs a NAMING LAYER. The identity key from
      the v2 record is (dHash, hueBucket), which is opaque; a human cannot
      author weights against a hash string. Catalog entry schema must therefore
      be: { id, displayName, spriteRef, dHash, hueBucket, weight }.
      Auto-discovery populates dHash/hueBucket/spriteRef; a human supplies
      displayName and weight once. Ship the JSON alongside
      ocr/unique-category-map.json; write user overrides to the config directory
      so an app update cannot clobber user edits.


      CONSEQUENCE 2 — NORMALISE BEFORE HASHING. Rescale each icon cell to a
      fixed size (32x32 suggested) before computing the dHash. Without this the
      hash varies per entry in src/OCR/OcrResolutionProfiles.cs and a shipped
      table would only work for users at the resolution it was authored at. This
      is a correctness requirement for the shipped-table decision, not an
      optimisation.


      CONSEQUENCE 3 — GRACEFUL DEGRADATION ON UNKNOWN KEYS. A new league will
      introduce runes absent from the shipped table. An unknown key must render
      as "discovered but unweighted" and be surfaced in the dashboard for the
      user to weight — it must NOT break the row or silently score 0 (silently
      scoring 0 would make a new high-value rune look worthless, which is worse
      than showing nothing). This makes the auto-discovery path a permanent
      fallback, not bootstrap scaffolding.


      OPEN / NON-BLOCKING — no authoritative weight data is available to the
      agent. Plan: seed the shipped table from hue/tier as placeholders so the
      feature works end to end, and flag the values as requiring user input or a
      community data source. Schema and code are identical either way, so this
      does not block implementation; it only affects the quality of
      first-release numbers. Resolve at grooming.


      PROPOSED EXTRACTION — two cards, split at the point where technical risk
      ends:

      - CARD A "Extract and fingerprint succession runes from the discarded icon
      strip": slice x in [0, textColX) from capturedBitmap, gilded-border
      detection, fixed-size normalisation, dHash, emit (shape, hue) keys.
      Includes the validation spike from the v2 record (strip inside capture
      region at all five profiles; gilded borders separable in the raw frame;
      hash stable frame-to-frame). Harness: tests/OcrPricingSimulator/ plus
      OcrOptions.SaveDebugImages. CARRIES ESSENTIALLY ALL THE RISK — if the
      spike fails, Card B is not built.

      - CARD B "Score combination rows against carried succession runes":
      catalog schema + shipped defaults + user overrides, per-run carried set
      with manual reset hotkey, rowScore computation, overlay rendering through
      src/Overlay/PriceColorCalculator.cs GetPriceColor and
      src/Overlay/PriceRowLayout.cs. Pure consumer of Card A's output; low
      technical risk.

      Deferred to later cards regardless: automatic run-boundary detection
      (needs Client.txt/zone parsing, no session concept exists in src/ today);
      in-world socket-bar overlay (needs its own region resolution + detection).
    date: '2026-09-12T14:34:39.492Z'
    selfAttested: true
    summary: >-
      Weight-source decided: ship a hand-authored user-editable weight table.
      Requires a naming layer over the opaque hash key, fixed-size normalisation
      before hashing for cross-resolution portability, and graceful unknown-rune
      degradation. Proposed two-card split (A: fingerprinting + spike, B:
      scoring/overlay) pending user go-ahead.
    pin: true
    id: c-2026-09-12t14-34-39-492z
  - type: comment
    user: User
    date: '2026-09-12T14:36:49.108Z'
    comment: go for it
    id: c-2026-09-12t14-36-49-108z
  - type: comment
    user: Agent
    comment: >-
      EXTRACTED — this discussion has been carved into two real cards, both
      created in Grooming:


      - **RUNE-1** "Extract and fingerprint succession runes from the discarded
      icon strip" (effort L). Icon-strip slicing from the raw frame,
      gilded-border detection, fixed-size normalisation, dHash + hue-bucket
      identity key. Includes the validation spike. CARRIES ALL THE TECHNICAL
      RISK.

      - **RUNE-2** "Score combination rows against carried succession runes"
      (effort M). Weight catalog with naming layer, shipped defaults + user
      overrides, per-run carried set with manual reset hotkey, rowScore, overlay
      via PriceColorCalculator. BLOCKED ON RUNE-1 — do not start until RUNE-1's
      spike passes.


      Both card bodies are self-contained and restate the load-bearing findings,
      constraints and decisions. They point back here for the full rationale and
      the rejected alternatives.


      Used create_ticket rather than extract_ticket deliberately: extract_ticket
      carves a contiguous seq range and consumes the scratch source, but this
      design is spread across several notes plus two user-supplied screenshots
      and needed splitting into two cards with hand-written bodies. SCRATCH-1 is
      therefore intentionally left intact as the rationale record that RUNE-1
      and RUNE-2 both reference.


      Deferred to future cards, recorded here so they are not lost: automatic
      run-boundary detection (needs Client.txt/zone parsing; no session concept
      exists in src/ today) and the in-world remnant socket-bar overlay (needs
      its own region resolution + detection, unlike the Combinations panel which
      is already segmented).
    date: '2026-09-12T14:38:10.991Z'
    selfAttested: true
    summary: >-
      Extracted to RUNE-1 (fingerprinting + spike, carries all risk) and RUNE-2
      (catalog/scoring/overlay, blocked on RUNE-1). Both in Grooming. SCRATCH-1
      retained as the rationale record.
    pin: true
    id: c-2026-09-12t14-38-10-991z
baselineCommit: 79e13186bd635da01a8d14958a13c8c2d8260bd0
tokenMetadata:
  inputTokens: 2427947
  outputTokens: 38550
  costUSD: 3.312439
  costIsEstimated: false
  cacheReadTokens: 2308712
  cacheCreationTokens: 119175
needsAction: >-
  Agent left a comment on this "Todo" ticket without raising a structured prompt
  or taking a board action — it may contain a decision/question that needs your
  attention.
---

