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
  - ocr
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
  - type: activity
    user: Agent
    date: '2026-09-12T14:51:42.235Z'
    comment: Updated description. Updated tags.
    id: a-2026-09-12t14-51-42-235z
  - type: activity
    user: Agent
    date: '2026-09-12T14:52:39.847Z'
    comment: Updated description.
    id: a-2026-09-12t14-52-39-847z
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
> **TL;DR** — The app already grabs the rune-icon strip on every scan of the Runeshape Combinations panel and throws it away to keep row detection clean. This card picks those pixels back up, works out which runes have the gold "carries forward" border, and turns each one into a stable fingerprint so a later card can tell you which combination rows give runes you don't already have. Prove it works on real screenshots first — if the fingerprints aren't stable, say so and stop.

**Card A of two.** Card B ("Score combination rows against carried succession runes") consumes this card's output and carries almost no technical risk. **All the risk lives here** — if the spike gate below fails, Card B should not be built as designed.

Carved from SCRATCH-1. The pinned DESIGN RECORD v2 + ADDENDUM there hold the full rationale including rejected alternatives — read them before changing any decision here.

## Problem / Motivation

In Path of Exile 2's Runeshape/Expedition remnant panel, gold/gilded-bordered runes are *succession runes*: they carry forward to the next remnant, and which ones are gilded is randomised per remnant. Players eyeball this today. This card produces a stable per-rune identity for every gilded rune in the Combinations panel so Card B can surface which rows grant runes the player isn't already carrying. Identification only — no scoring, no overlay, no run state.

## The opportunity

In `CaptureAndRecognize` (`src/OCR/OcrLeagueWindowReader.cs`) the scan region's left edge is shifted from the icon column to the text column:

```csharp
var textColX = (int)(preprocessed.Width * options.PanelLeftFraction);
var newX = Math.Max(crop.Value.X, textColX);
```

`PanelLeftFraction` defaults to `0.30` (`src/OCR/OcrOptions.cs`, bound via the usual `IOptionsMonitor<OcrOptions>` pattern); the existing comment says why — stop rune icons inflating row widths and confusing row detection. So for every row already segmented, `x ∈ [0, textColX)` **is** the 5-icon strip: captured, row-aligned, then dropped.

**No new capture path, no new `OcrResolutionProfiles` entry, no new `LeaguePanelDetector` work. Do not add any.**

## Decisions that are closed — do not re-open

- **Identity key is `(greyscale dHash of the normalised glyph, dominant hue bucket)`.** Shape plus tier/sub-tier colour. Whether the same glyph can roll at different tiers is an unresolved game-mechanics question; the composite key is deliberately general so it can't block implementation. If weight turns out to be pure shape, the hue component is constant per shape and harmless. **Do not simplify to shape alone.**
- **Sample the raw frame, never `preprocessed`.** `OcrImagePreprocessor` binarizes and colour-filters for text (`IsLikelyTextColor` is pure RGB distance + luminance), destroying exactly the colour information gilded detection and tier classification need.
- **Normalise to 32×32 before hashing** — correctness, not optimisation. Without it hashes differ per resolution profile and Card B's weight table would only work at the resolution it was authored at.
- **No new image-processing or ML dependency.** dHash is ~40 lines of pixel statistics; write it by hand.

## Acceptance criteria

### Spike gate — recorded evidence required before continuing past step 3

- [ ] **(a)** At each of the 5 profiles in `src/OCR/OcrResolutionProfiles.cs` (1600x900, 1920x1080, 2560x1440, 3440x1440, 3840x2160), the strip has positive width and each of the 5 cells is **≥24×24 raw px**. Actual `textColX` and per-cell width reported for all 5.
- [ ] **(b)** A gilded-vs-non-gilded border metric (proportion of border-ring pixels inside a gold hue+saturation range) over **≥30 samples per class**, spanning all 3 glyph tier colours (blue/purple/gold) and ≥2 profiles, shows **zero overlap** between the class distributions (`min(gilded) > max(non-gilded)`).
- [ ] **(c1)** Same sprite, same profile, **≥5 consecutive captures**, no game-state change: pairwise dHash Hamming **≤2** of 64 bits.
- [ ] **(c2)** Same glyph at **2 different profiles** after normalisation: Hamming **≤4**.
- [ ] **(c3)** Two visibly different glyphs: Hamming **≥10**. Without this, "stable" is trivially satisfiable by a degenerate always-equal hash.
- [ ] Pass/fail per check, measured values, and fixture filenames recorded in a ticket comment before proceeding.

### Implementation

- [ ] Each row rect's `x ∈ [0, textColX)` is sliced into 5 equal cells read from `capturedBitmap`; `preprocessed` is never sampled for colour.
- [ ] Cells are classified via the spike-validated border metric, and non-gilded cells are dropped **before** normalisation or hashing — this early exit is what keeps added cost at ~1–2 cells per row instead of 5.
- [ ] Surviving cells are rescaled to 32×32 and emit a `(shapeHash, hueBucket)` key.
- [ ] Keys reach Card B in row order via a new trailing optional field on `LeagueWindowSnapshot`; every existing construction site still compiles unchanged.
- [ ] A row yielding a cell count ≠ 5 is skipped and logged — never thrown, never crashes the OCR pass.
- [ ] A cell whose border metric lands in the ambiguous band is treated as non-gilded and dropped, with a debug log entry. *Rationale: a wrong key corrupts Card B's "already carrying" set and produces actively wrong advice; a missing key only under-reports one row.*
- [ ] With `SaveDebugImages` on, sliced cells and their computed keys are written alongside the existing `1 Raw.png` … `4 Cropped.png`; existing price-OCR debug behaviour unchanged. With it off, no new file I/O.
- [ ] No new NuGet dependency.

## Implementation plan

**Land here first:** `CaptureAndRecognize` in `src/OCR/OcrLeagueWindowReader.cs` (owns both the raw bitmap and the row rects), then `src/Contracts/LeagueWindowSnapshot.cs`.

1. **Check (a) analytically — no screenshots needed.** Unit test over every `OcrResolutionProfiles` entry: compute capture width, derive `textColX = width × PanelLeftFraction`, assert `textColX / 5 ≥ 24`. Do this first — a failure reshapes the whole card. **If it fails, report which profiles and by how much; do not propose widening the capture region** (that affects the existing price OCR path and is out of scope).

2. **Build the offline harness — it does not exist yet.** The original filing named `tests/OcrPricingSimulator/`, but that is a text-only pricing tool (`Program.cs` parses `--item`/`--input-file` strings through `ItemNameParser` and hits poe2scout/ninja/mock sources); it has **zero image or `Bitmap` code**, and no PNG-driven OCR harness or OCR fixture exists anywhere in the repo. Add a console project `tests/RuneIconProbe/` that loads a PNG, runs it through `OcrImagePreprocessor` and `OcrPipeline.DetectRowPositions` as the live path does, and dumps sliced cells, per-cell border metric, and keys. *A new project rather than extending the pricing simulator: that tool is single-purpose text/pricing, and an image mode would entangle two unrelated concerns.* If `DetectRowPositions` is entangled with live capture, extract the pure geometry portion rather than duplicating it. Fixtures: `tests/fixtures/runeicons/<resolution>/*.png`, git-tracked.

3. **Get real fixtures, then run (b) and (c).** These are questions about real game art and cannot be answered with synthetic bitmaps. Captures come from enabling `OcrOptions.SaveDebugImages` during play and keeping the `1 Raw.png` frames — needed: gilded runes across blue/purple/gold tiers at ≥2 profiles, plus ≥5 consecutive frames of one unchanged panel for (c1). **If no fixtures are available when you reach this step, `change_status` to Require Input and ask the user for them rather than faking the gate with synthetic art.** Record results and stop here if any check fails.

4. **Write the fingerprinter** — new file `src/OCR/RuneIconFingerprinter.cs`, no existing home. Owns: cell slicing from a row rect + `textColX`; the border-ring gilded metric; RGB→hue conversion and bucketing (nothing reusable exists — `IsLikelyTextColor` is pure RGB distance); 32×32 box-filter downscale; greyscale 8×8 dHash → 64-bit key. Use `LockBits` + `Marshal.Copy` into a flat `byte[]` with manual stride handling, the established idiom in `OcrImagePreprocessor.cs` and `OcrLeagueWindowReader.cs` — **not `GetPixel`/`SetPixel`**. Keep the class pure (bitmap in, keys out) so the harness and unit tests drive it directly.

5. **Hook into the reader.** Call it from `CaptureAndRecognize` after `OcrPipeline.DetectRowPositions` populates `rowYs`/`rowHeights`, before the method returns. **Mandatory placement, not stylistic:** `capturedBitmap` is a `using var` scoped to this method, so deferring to `ReadSnapshotCore` or later would require cloning the frame. Reuse the computed `rowYs`/`rowHeights`/`crop` — do not recompute row boundaries. No coordinate remapping is needed: `KeepBlackAndNeighbors` and `PreprocessForOcr` both allocate via `new Bitmap(source.Width, source.Height, …)`, so `preprocessed` shares the raw frame's coordinate space. (The pipeline's only upscale, `UpscaleForOcr(rowBitmap, 3)` from `src/OCR/OcrPipeline.cs`, runs after row detection, per-row, for Tesseract only.)

6. **Extend the output contract.** Add a trailing optional field (e.g. `RuneKeys`) to the `LeagueWindowSnapshot` record, defaulted so the change is additive, and thread it through the real construction site in `OcrLeagueWindowReader.cs` (the one built from `lines`, `capturedAt`, `matchedYPositions`). **Check every construction site** — ~5 in `OcrLeagueWindowReader.cs`, ~3 in `src/App/LeaguePricingWorker.cs`, plus consumers including `tests/src/Overlay/PriceRowLayoutTests.cs`. They mix positional and named arguments.

7. **Extend debug output** following the existing `OcrImagePreprocessor.SavePng` numbering convention (e.g. `5 IconCells.png`), gated on the same `SaveDebugImages` flag.

8. **Unit tests** per below.

## Recommended tests

- **Cell geometry** — the step-1 test across all 5 profiles; assert bounds never negative or out of range.
- **dHash correctness** — synthetic bitmaps with known perturbations and expected Hamming distances, using the `new Bitmap(W, H, PixelFormat.Format24bppRgb)` idiom already in `tests/src/OCR/OcrImagePreprocessorTests.cs`.
- **Hue bucketing** — synthetic swatches at known hues, including bucket-boundary values.
- **Fail-soft paths** — a row rect producing ≠5 cells, and a cell in the ambiguous band; assert skip-and-log, no throw.
- **Fixture regression via `tests/RuneIconProbe/`** — once the gate passes, assert the emitted key sequence for a checked-in mixed panel matches an expected fixture, and that one glyph yields the same shape hash at two profiles. This turns the one-off cross-profile check into a repeatable guard.

## Risks / caveats

- **The spike's real cost is fixture acquisition, not code.** Checks (b) and (c) are blocked on screenshots only the user can produce. Sequence steps 1, 2, 4 and the synthetic unit tests to proceed while fixtures are gathered.
- **Resolution dependency is two-layered.** `OcrResolutionProfiles` picks a capture region in **absolute pixels** per profile (`OcrResolutionProfile(CaptureOffsetX, CaptureOffsetY, CaptureWidth, CaptureHeight)`, exact-string `TryGet`, `Interpolate(width, height)` for anything unlisted), and only then does `PanelLeftFraction` cut a fraction of that. Check (a) must be evaluated per profile; interpolated resolutions are an untested tail — out of scope here, worth a follow-up card if (a) is tight at the extremes.
- **This runs inside the live price-OCR hot path.** A regression that hashes all 5 cells per row multiplies per-frame work; keep the early-exit ordering intact.

## Out of scope

- Scoring, weighting, catalog, overlay — Card B.
- Run-boundary detection. No session concept and no `Client.txt`/zone parsing exists in `src/` today (verified). Later card.
- Overlay on the in-world remnant socket bar (distinct from the Combinations panel) — needs its own region resolution and detection. Deferred.

## Context

`net8.0-windows10.0.17763.0`, `UseWindowsForms=true` — Windows-only; `System.Drawing`/`Bitmap`/`LockBits` is already a core dependency throughout the OCR path.

Fork: `origin` = `guybnd/RuneshapePriceChecker`, `upstream` = `Barragek0/RuneshapePriceChecker`.

**Plan artifact:** revision 1 renders the data flow, strip geometry, spike gate, and fail-soft contract.
