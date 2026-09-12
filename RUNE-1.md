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
    id: a-2026-09-12t14-37-40-190z
  - type: activity
    user: User
    date: '2026-09-12T14:44:51.080Z'
    comment: "\U0001F3AF Launch focus: ## Dynamic Delegation\n\nUse `list_available_agents` to discover specialists, then delegate as needed. Only delegate when specialist knowledge adds clear value over doing it yourself."
    id: a-2026-09-12t14-44-51-080z
  - type: agent_session
    sessionId: d740e8c0-8e94-4a7b-82f5-488b64ab1ff8
    startedAt: '2026-09-12T14:44:51.080Z'
    status: active
    progress: []
    user: Claude Code
    date: '2026-09-12T14:44:51.080Z'
    enginePid: 9336
    groupId: 3a85ffcd-2994-412c-b0b0-02d98d3feef1
    role: planner
    pattern: supervisor
  - type: agent_session
    sessionId: 315e051d-ab05-441b-80c8-da3d41cd5e3d
    startedAt: '2026-09-12T14:45:42.898Z'
    status: completed
    progress:
      - timestamp: '2026-09-12T14:45:59.119Z'
        message: Working
        type: info
      - timestamp: '2026-09-12T14:46:14.124Z'
        message: Working
        type: info
      - timestamp: '2026-09-12T14:46:29.139Z'
        message: Reading
        type: info
      - timestamp: '2026-09-12T14:46:44.154Z'
        message: Reading
        type: info
      - timestamp: '2026-09-12T14:46:59.164Z'
        message: Reading
        type: info
      - timestamp: '2026-09-12T14:47:14.168Z'
        message: Working
        type: info
      - timestamp: '2026-09-12T14:47:59.182Z'
        message: Working
        type: info
      - timestamp: '2026-09-12T14:48:14.191Z'
        message: Working
        type: info
      - timestamp: '2026-09-12T14:48:48.016Z'
        message: >-
          Context-scout recon on RUNE-1 is posted. Summary of what I found:


          - **9 of 10 claims confirmed**, one **wrong**:
          `tests/OcrPricingSimulator` is a pure text-based pricing tool with
          zero image/Bitmap code — it cannot validate the OCR icon-strip
          approach offline. No such harness exists anywhere in the repo; the
          closest reusable pattern is
          `tests/src/OCR/OcrImagePreprocessorTests.cs`'s synthetic-bitmap idiom.

          - **Good news for the plan**: the preprocessed bitmap is *not* scaled
          relative to the raw frame — same pixel dimensions, same coordinate
          space — so no coordinate remapping is needed when slicing icon cells
          from raw pixels using preprocessed-derived row rects.

          - **Nuance on resolution risk**: capture regions are absolute-pixel
          profiles (with interpolation for unlisted resolutions), and
          `PanelLeftFraction` is applied on top of that — two stacked
          dependencies, not a single fraction.

          - Exact insertion points identified: `CaptureAndRecognize` in
          `OcrLeagueWindowReader.cs` (raw bitmap's `using`-scope), and
          `LeagueWindowSnapshot` in `src/Contracts/` for the output contract.


          This was posted as a pinned comment for the Planner to synthesize into
          the actual implementation plan — I didn't touch ticket status or the
          ticket body, per my scoped role.
    user: Claude Code
    date: '2026-09-12T14:45:42.898Z'
    enginePid: 9336
    groupId: 3a85ffcd-2994-412c-b0b0-02d98d3feef1
    role: 'assistant:context-scout'
    pattern: supervisor
    outcome: Claude Code session ended with code 0.
    endedAt: '2026-09-12T14:48:48.016Z'
    originalProgressCount: 9
    finalMessage: >-
      Context-scout recon on RUNE-1 is posted. Summary of what I found:


      - **9 of 10 claims confirmed**, one **wrong**: `tests/OcrPricingSimulator`
      is a pure text-based pricing tool with zero image/Bitmap code — it cannot
      validate the OCR icon-strip approach offline. No such harness exists
      anywhere in the repo; the closest reusable pattern is
      `tests/src/OCR/OcrImagePreprocessorTests.cs`'s synthetic-bitmap idiom.

      - **Good news for the plan**: the preprocessed bitmap is *not* scaled
      relative to the raw frame — same pixel dimensions, same coordinate space —
      so no coordinate remapping is needed when slicing icon cells from raw
      pixels using preprocessed-derived row rects.

      - **Nuance on resolution risk**: capture regions are absolute-pixel
      profiles (with interpolation for unlisted resolutions), and
      `PanelLeftFraction` is applied on top of that — two stacked dependencies,
      not a single fraction.

      - Exact insertion points identified: `CaptureAndRecognize` in
      `OcrLeagueWindowReader.cs` (raw bitmap's `using`-scope), and
      `LeagueWindowSnapshot` in `src/Contracts/` for the output contract.


      This was posted as a pinned comment for the Planner to synthesize into the
      actual implementation plan — I didn't touch ticket status or the ticket
      body, per my scoped role.
  - type: agent_session
    sessionId: 870a7925-e5a8-4c50-9372-baf41c639928
    startedAt: '2026-09-12T14:45:43.471Z'
    status: completed
    progress:
      - timestamp: '2026-09-12T14:45:59.134Z'
        message: Working
        type: info
      - timestamp: '2026-09-12T14:46:44.169Z'
        message: Working
        type: info
      - timestamp: '2026-09-12T14:47:09.684Z'
        message: >-
          Posted the requirements analysis as a pinned comment on RUNE-1.
          Summary of the outcome:


          - **Spike gate**: concrete numeric thresholds for all three validation
          checks — min 24×24px cells across all 5 resolution profiles,
          zero-overlap gilded/non-gilded border metric over ≥30 samples per
          class across 3 tiers, and dHash Hamming-distance bounds for
          frame-stability (≤2), cross-profile stability (≤4), and cross-shape
          discriminability (≥10).

          - **Implementation criteria**: fail-soft defaults for malformed rows
          (skip, don't crash) and ambiguous gilded calls (drop, don't guess) —
          reasoning is that a wrong identity key corrupts Card B's "already
          carrying" set, while a missing one just under-reports.

          - **Recommended tests**: unit tests for dHash/hue-bucket/cell-slicing
          math, plus extending `tests/OcrPricingSimulator/` as the primary
          fixture-based regression guard.

          - **No blocking questions** — every open seam had a safe, stated
          default rather than needing to pause on Require Input.


          I didn't touch ticket status or rewrite the body — that's the
          Planner's job next, per the delegation boundaries I was given.
    user: Claude Code
    date: '2026-09-12T14:45:43.471Z'
    enginePid: 9336
    groupId: 3a85ffcd-2994-412c-b0b0-02d98d3feef1
    role: 'assistant:requirements-interrogator'
    pattern: supervisor
    outcome: Claude Code session ended with code 0.
    endedAt: '2026-09-12T14:47:09.684Z'
    originalProgressCount: 3
    finalMessage: >-
      Posted the requirements analysis as a pinned comment on RUNE-1. Summary of
      the outcome:


      - **Spike gate**: concrete numeric thresholds for all three validation
      checks — min 24×24px cells across all 5 resolution profiles, zero-overlap
      gilded/non-gilded border metric over ≥30 samples per class across 3 tiers,
      and dHash Hamming-distance bounds for frame-stability (≤2), cross-profile
      stability (≤4), and cross-shape discriminability (≥10).

      - **Implementation criteria**: fail-soft defaults for malformed rows
      (skip, don't crash) and ambiguous gilded calls (drop, don't guess) —
      reasoning is that a wrong identity key corrupts Card B's "already
      carrying" set, while a missing one just under-reports.

      - **Recommended tests**: unit tests for dHash/hue-bucket/cell-slicing
      math, plus extending `tests/OcrPricingSimulator/` as the primary
      fixture-based regression guard.

      - **No blocking questions** — every open seam had a safe, stated default
      rather than needing to pause on Require Input.


      I didn't touch ticket status or rewrite the body — that's the Planner's
      job next, per the delegation boundaries I was given.
  - type: comment
    user: Agent
    comment: >-
      **REQUIREMENTS**


      ### Acceptance criteria


      **Spike gate** (must be demonstrated, with recorded evidence, before
      implementation continues past the validation spike)


      - [ ] For each of the 5 profiles in `src/OCR/OcrResolutionProfiles.cs`, a
      `SaveDebugImages` capture of the Combinations panel with at least one
      visibly gilded rune shows the icon strip (`x ∈ [0, textColX)`) has
      positive width and each of the 5 derived icon cells is **≥24×24 raw
      pixels**. Report actual `textColX` and per-cell width for all 5 profiles.

      - [ ] A gilded-vs-non-gilded border metric (e.g. % of border-ring pixels
      within a gold hue+saturation range) is computed over **≥30 gilded-labeled
      and ≥30 non-gilded-labeled** icon cells, sampled across all 3 known glyph
      tier colors (blue/purple/gold) and at least 2 resolution profiles, with
      **zero overlap** between the two classes' metric distributions
      (`min(gilded metric) > max(non-gilded metric)` or vice versa).

      - [ ] dHash of the same sprite, same resolution profile, across **≥5
      consecutive raw-frame captures** with no game-state change: pairwise
      Hamming distance **≤2** (of 64 bits, from an 8×8 diff hash).

      - [ ] dHash of the same glyph shape captured at **2 different resolution
      profiles**, after 32×32 normalization: Hamming distance **≤4**.

      - [ ] dHash of **two visibly different glyph shapes**: Hamming distance
      **≥10** — confirms the hash still discriminates shapes after the
      normalization forced by stability (not in the ticket body verbatim, but
      needed or "stable" becomes trivially satisfiable by a degenerate
      always-equal hash).

      - [ ] Spike results (pass/fail per check above, the actual measured
      values, and the debug images used) are recorded in a ticket comment before
      any implementation work proceeds past this gate.


      **Implementation**


      - [ ] For every row rect from the existing row-detection pass, `x ∈ [0,
      textColX)` is sliced into 5 equal-width cells read from `capturedBitmap`
      (raw), never from `preprocessed`.

      - [ ] Each cell is classified gilded/non-gilded via the border-hue metric
      validated in the spike gate; non-gilded cells are dropped before hashing.

      - [ ] Surviving cells are rescaled to a fixed size (32×32, or whatever
      size the spike validated) before dHash computation.

      - [ ] Each surviving cell emits a `(shapeHash, hueBucket)` key; keys are
      emitted in row order on a documented contract Card B can consume (e.g.
      ordered list per row, or per-panel list carrying row index).

      - [ ] A row yielding a cell count ≠ 5 (mis-segmented row) is
      **fail-soft**: skipped and logged, never thrown, never crashes the OCR
      pass.

      - [ ] A cell whose border metric falls in the ambiguous middle band (per
      spike-derived thresholds) defaults to **treated as non-gilded** (dropped),
      with a debug log entry — see rationale below.

      - [ ] `OcrOptions.SaveDebugImages` output is extended to include the
      sliced per-cell images and their computed `(shapeHash, hueBucket)` keys,
      without altering existing debug-image behavior for the price-OCR path.

      - [ ] No new capture path, no new `OcrResolutionProfiles` entry, no new
      `LeaguePanelDetector` code, no new image-processing/ML dependency.


      ### Recommended tests


      - **Unit tests** (co-located with existing OCR tests): dHash correctness
      on synthetic bitmaps with known perturbations/expected Hamming distances;
      hue-bucket classification on synthetic color swatches; cell-slicing math
      for each of the 5 `OcrResolutionProfiles` entries (verifies cell bounds
      never go negative or out-of-range).

      - **`tests/OcrPricingSimulator/`** (primary regression guard once the
      spike passes): extend with a scenario feeding captured raw+preprocessed
      bitmap pairs for a Combinations panel with mixed gilded/non-gilded runes
      through the new pipeline, asserting the emitted `(shapeHash, hueBucket)`
      sequence matches a checked-in expected fixture.

      - **Offline cross-profile validation**: run the simulator against captures
      from all 5 resolution profiles and confirm identical shape hashes for the
      same glyph across profiles — turns the spike's cross-profile stability
      check into a repeatable fixture-based test rather than a one-time manual
      check.


      ### Defaults I am assuming


      - **Numeric spike thresholds** (24px min cell width; Hamming ≤2
      same-frame, ≤4 cross-profile, ≥10 cross-shape; zero-overlap gilded metric)
      are proposed, not in the original ticket. *Why:* "reliably separable" and
      "stable" are unfalsifiable without numbers; these are conservative,
      checkable values consistent with standard 64-bit perceptual-hash practice.

      - **Ambiguous/marginal detections default to drop-silently-and-log**,
      never a guessed key. *Why:* Card B uses these keys to tell the player
      which rows grant runes not already carried — a wrong key (false identity)
      corrupts that "already carrying" set, while a missing key just
      under-reports one row. Fail-soft toward omission, not toward guessing.

      - **Row cell-count mismatches (≠5) are skipped, not error-raised.** *Why:*
      one malformed row shouldn't take down the whole OCR pass; this matches the
      ticket's own framing of gilded-detection as an early-exit filter to reduce
      work, i.e. tolerance for partial/noisy visual data is already the design's
      spirit.

      - **Spike evidence is recorded as a ticket comment referencing debug-image
      filenames/paths plus the measured metric values** (not just "looks fine"),
      so a reviewer can audit the spike later without re-running it.

      - **Assumed real captures covering gilded runes at blue/purple/gold tiers
      across ≥2 resolution profiles are obtainable** (via `SaveDebugImages`
      during actual play, or existing fixtures) before the spike starts. *Why:*
      if none exist yet, producing them is a prerequisite implementation step,
      not a scope or direction change — doesn't warrant blocking grooming over.


      ### Blocking question


      None — all ambiguity resolvable by stated default.
    date: '2026-09-12T14:47:02.381Z'
    selfAttested: true
    summary: >-
      Requirements analysis: concrete pass/fail thresholds for spike checks
      (a)(b)(c), fail-soft runtime defaults for marginal detections, spike
      evidence format, and full acceptance criteria checklist. No blocking
      questions — all ambiguity resolved by stated defaults.
    pin: true
    id: c-2026-09-12t14-47-02-381z
  - type: comment
    user: Agent
    comment: >
      **CONTEXT SCOUT**


      ### Land here first


      1. **`src/OCR/OcrLeagueWindowReader.cs`, method `private string
      CaptureAndRecognize(out bool attemptedRecognition, out bool fromCache)`**
      (declared line 418). The icon-strip slice must be inserted inside this
      method, between where `crop`/`rowYs`/`rowHeights` become known (line 683)
      and where the method returns — `capturedBitmap` (the raw frame) is a
      `using var` declared at line 570 and is disposed at the end of this
      method's scope, so all raw-pixel sampling must happen synchronously inside
      this method body, not deferred.

      2. **`src/Contracts/LeagueWindowSnapshot.cs`** — the output contract
      record. Currently `ItemNames: IReadOnlyList<string>` + `CapturedAtUtc`,
      `RowYPositions`, `InterfaceDetected`, `CaptureMethod`, `CropBounds`,
      `RetryRegions`, `RejectedRegions`. No per-row rune-key field exists — this
      card must add one (e.g. `RuneKeys: IReadOnlyList<...>? = null`), threaded
      through from `OcrLeagueWindowReader.cs:389` where the real snapshot is
      constructed (`_lastSnapshot = new LeagueWindowSnapshot(lines, capturedAt,
      matchedYPositions, ...)`).

      3. A new file (e.g. `src/OCR/RuneIconFingerprinter.cs`) for the
      gilded-border detection + dHash + hue-bucket logic — no existing home for
      this.


      ### Verified claims


      1. **CONFIRMED.** Lines 660-664 of `OcrLeagueWindowReader.cs` are exactly
      as cited: `var textColX = (int)(preprocessed.Width *
      options.PanelLeftFraction); var newX = Math.Max(crop.Value.X, textColX);`.
      Enclosing method: `private string CaptureAndRecognize(out bool
      attemptedRecognition, out bool fromCache)`, line 418.

      2. **CONFIRMED.** `PanelLeftFraction` at `src/OCR/OcrOptions.cs:48`,
      `[Range(0.0, 1.0)]`, default `0.30`. Bound via the standard
      `IOptionsMonitor<OcrOptions>` pattern used for every other field in that
      class — no special-case construction.

      3. **CONFIRMED — load-bearing, verified precisely.** `using var
      capturedBitmap = captureResult.Bitmap;` at `OcrLeagueWindowReader.cs:570`,
      inside `CaptureAndRecognize`. `rowYs`/`rowHeights` are computed at line
      683 (`(rowYs, rowHeights) = OcrPipeline.DetectRowPositions(...)`) inside
      the same method body/try block, so `capturedBitmap` is still in scope and
      undisposed at that point. Exact variable names: `capturedBitmap` (raw),
      `preprocessed` (Bitmap, non-null after line 621), `crop` (`Rectangle?`),
      `rowYs`/`rowHeights` (`int[]`).

      4. **CONFIRMED, plus an important correction to the ticket's framing.**
      `IsLikelyTextColor` exists at `OcrImagePreprocessor.cs:604-633` and is
      pure RGB target-distance + luminance/spread — it does not use HSV/hue at
      all, and no hue extraction exists anywhere in the file or codebase.
      However: **`preprocessed` is NOT scaled relative to `capturedBitmap`.**
      Both `KeepBlackAndNeighbors` (`OcrImagePreprocessor.cs:27`) and
      `PreprocessForOcr` (`OcrImagePreprocessor.cs:148`) allocate their output
      as `new Bitmap(width, height, ...)` using `source.Width`/`source.Height`
      directly — same pixel dimensions, same coordinate space as
      `capturedBitmap`. The only upscaling in the whole pipeline is
      `OcrImagePreprocessor.UpscaleForOcr(rowBitmap, 3)`, called from
      `OcrPipeline.cs:25`, which runs *after* row rects are known and only on
      individual per-row bitmaps for Tesseract recognition — it never touches
      the row-detection coordinate space. **No coordinate remapping is needed**
      between raw and preprocessed row rects; this simplifies the implementation
      versus what the ticket assumes.

      5. **CONFIRMED**, with a nuance the ticket doesn't mention.
      `src/OCR/OcrResolutionProfiles.cs` lists exactly 5 entries — `1600x900`,
      `1920x1080`, `2560x1440`, `3440x1440`, `3840x2160` — each an
      `OcrResolutionProfile(CaptureOffsetX, CaptureOffsetY, CaptureWidth,
      CaptureHeight)` in **absolute pixels**, keyed by exact resolution string
      via `TryGet`, with `Interpolate(width, height)` doing linear interpolation
      between the two nearest-by-pixel-count profiles for resolutions not in the
      table. `PanelLeftFraction` is then applied as a fraction *of the resulting
      captured/preprocessed bitmap width*, not of the screen. So there are two
      stacked resolution-dependencies: absolute-pixel region selection (exact or
      interpolated), then a fractional sub-crop within that region. Validation
      spike (a) should be checked at all 5 listed profiles; interpolated
      (unlisted) resolutions are a secondary, lower-priority risk.

      6. **CONFIRMED.** Output type is `LeagueWindowSnapshot`
      (`src/Contracts/LeagueWindowSnapshot.cs`), a sealed record — see "Land
      here first" #2 above for fields and attachment point.

      7. **WRONG.** `tests/OcrPricingSimulator/Program.cs` (fully read) has zero
      image/Bitmap/OCR code. It's a pure text-pricing harness: parses
      `--item`/`--input-file` item-name strings via `ItemNameParser`, prices
      them through `InMemoryPricingCache` against poe2scout/ninja/mock sources
      (`CreatePricingSourceAsync`), and prints price matches (invoked as `dotnet
      run --project tests/OcrPricingSimulator -- --item "..."`). **It cannot
      feed a PNG through the league-window reader path — no such offline harness
      exists anywhere in the repo.** No sample PNGs are checked into `tests/` at
      all; the only PNGs repo-wide are `img/example.png` (a setup-overlay
      illustration referenced by `src/Overlay/SetupOverlayForm.cs:157`) and
      `src/Dashboard/rune-43.png`, neither an OCR fixture. The closest existing
      convention is `tests/src/OCR/OcrImagePreprocessorTests.cs`, which builds
      synthetic in-memory bitmaps directly (`new Bitmap(10, 10,
      PixelFormat.Format24bppRgb)`, etc.) and calls preprocessor methods in
      xunit tests — that idiom should be reused for unit-level validation, but
      it cannot validate real gilded-border/hue separability against actual game
      art (spike (b)) since there's no real game screenshot in the repo.

      8. **CONFIRMED negative.** No HSV/hue/GetHue/dHash/perceptual-hash code
      anywhere in `src/`. `IsLikelyTextColor` is the only color-classification
      logic and it's pure RGB-distance as described in #4.

      9. **CONFIRMED negative.** No `Client.txt` or zone-parsing anywhere in
      `src/` (0 matches). No session/run-state concept — a grep for
      session/RunState/run-boundary only matched Sentry crash-session plumbing
      (`src/Startup/CrashLogger.cs`, `SentryNativeMethods.cs`) and unrelated
      app-lifecycle files, nothing game-run-related.

      10. **CONFIRMED.** `TargetFramework net8.0-windows10.0.17763.0`,
      `UseWindowsForms=true` in `RuneshapePriceChecker.csproj` — Windows-only.
      `System.Drawing`/`Bitmap`/`LockBits` is already the core dependency used
      pervasively throughout `OcrImagePreprocessor.cs` and
      `OcrLeagueWindowReader.cs` — fast pixel access via `LockBits` +
      `Marshal.Copy` is the established, safe idiom here.


      ### Reuse


      - **Pixel access idiom**: `LockBits` + `Marshal.Copy` into a flat `byte[]`
      with manual stride handling, exactly as done at
      `OcrLeagueWindowReader.cs:633-647` and throughout
      `OcrImagePreprocessor.cs`. Do not use `GetPixel`/`SetPixel` (far too slow,
      and inconsistent with every other pixel-processing path in this file).

      - **Row geometry**: `rowYs`/`rowHeights` and the `crop` rectangle are
      already computed via `OcrPipeline.DetectRowPositions` at
      `OcrLeagueWindowReader.cs:683` — reuse these directly rather than
      recomputing row boundaries.

      - **Debug image output**: `OcrImagePreprocessor.SavePng` (used at lines
      669-676 for `"1 Raw.png"`, `"2 Mask.png"`, `"3 Preprocessed.png"`, `"4
      Cropped.png"`) is the exact mechanism to extend for the ticket's
      requirement to dump sliced icon cells and computed keys — continue the
      sequential-numbering convention (e.g. `"5 IconCells.png"` or per-row
      files).

      - **Test idiom**: `tests/src/OCR/OcrImagePreprocessorTests.cs`'s
      synthetic-Bitmap pattern (`new Bitmap(W, H, PixelFormat.Format24bppRgb)`)
      for unit-testing new fingerprinting logic without needing game capture.


      ### Risks


      - **No offline PNG-driven validation harness exists** (see claim 7). The
      validation spike as scoped in the ticket body cannot be run purely offline
      against `tests/OcrPricingSimulator` as written — that tool is unrelated.
      Realistic options are: (i) new xunit tests with synthetic bitmaps
      (verifies logic correctness, not real gilded-border/hue separability), or
      (ii) iterating against `OcrOptions.SaveDebugImages` output from live game
      captures or manually-saved screenshots. This raises the cost of spike (b)
      specifically (real-game color separability) — it likely needs at least a
      few real screenshots at different resolutions, which aren't currently
      checked into the repo and would need to be captured or requested from the
      user.

      - **`capturedBitmap`'s lifetime is strictly scoped to
      `CaptureAndRecognize`** via `using var` (line 570) — any
      icon-fingerprinting code must run synchronously inside that method, before
      it returns; it cannot be deferred to `ReadSnapshotCore` or later without
      cloning the bitmap first.

      - **`LeagueWindowSnapshot` is a shared public contract record** consumed
      by `src/App/LeaguePricingWorker.cs` and
      `tests/src/Overlay/PriceRowLayoutTests.cs` among others. All current
      construction sites (`OcrLeagueWindowReader.cs:204,389,399,407,414`;
      `LeaguePricingWorker.cs:90,221,246`) use named/mixed positional args, and
      every existing optional field has a default — adding a new trailing
      optional field (e.g. `RuneKeys`) is additive and non-breaking if it
      follows the same pattern, but every construction site should be checked,
      not just the "real" one at line 389.

      - **Resolution dependency is two-layered, not purely fractional** (see
      claim 5) — implementers should not assume `PanelLeftFraction` alone
      determines where the icon strip falls; the absolute-pixel capture region
      selection (exact match or interpolated) happens first and could shift the
      icon strip out of the captured region entirely for extreme/unlisted aspect
      ratios, which is exactly what validation spike (a) needs to check
      per-profile.
    date: '2026-09-12T14:48:41.283Z'
    selfAttested: true
    summary: >-
      Context scout verified all 10 claims against current code: 9 confirmed, 1
      wrong (no offline PNG harness exists — tests/OcrPricingSimulator is a
      text-only pricing tool). Preprocessed bitmap is same-scale as raw (no
      coord remapping needed). Capture region uses absolute-pixel profiles with
      interpolation, not pure fractions.
    pin: true
    id: c-2026-09-12t14-48-41-283z
  - type: activity
    user: Agent
    comment: >-
      Published artifact revision 1 — RUNE-1 — icon-strip fingerprinting: data
      flow, geometry, spike gate (11,781 bytes).
    date: '2026-09-12T14:50:25.682Z'
    id: a-2026-09-12t14-50-25-682z
baselineCommit: 4930b6708b97d44d404a035dad8fd0a19ff6085e
tokenMetadata:
  inputTokens: 1251371
  outputTokens: 24639
  costUSD: 1.092922
  costIsEstimated: false
  cacheReadTokens: 1096054
  cacheCreationTokens: 155281
artifacts:
  latest: 1
  revisions:
    - rev: 1
      createdAt: '2026-09-12T14:50:25.677Z'
      bytes: 11781
      title: 'RUNE-1 — icon-strip fingerprinting: data flow, geometry, spike gate'
      note: >-
        Plan proposal. Shows where the icon strip is discarded today, exactly
        where the new code hooks in (inside CaptureAndRecognize, while
        capturedBitmap is alive), the raw-vs-preprocessed sampling rule, and the
        spike gate that must pass before implementation continues. Note the
        corrected harness story: tests/OcrPricingSimulator is a text-only tool
        and cannot run this spike.
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
