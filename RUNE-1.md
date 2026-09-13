---
id: RUNE-1
title: Extract and fingerprint succession runes from the discarded icon strip
status: In Progress
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
    status: completed
    progress:
      - timestamp: '2026-09-12T14:45:06.831Z'
        message: Working
        type: info
      - timestamp: '2026-09-12T14:48:51.990Z'
        message: Thinking
        type: info
      - timestamp: '2026-09-12T14:49:06.990Z'
        message: Working
        type: info
      - timestamp: '2026-09-12T14:49:37.005Z'
        message: Working
        type: info
      - timestamp: '2026-09-12T14:50:37.029Z'
        message: Working
        type: info
      - timestamp: '2026-09-12T14:51:52.068Z'
        message: Working
        type: info
      - timestamp: '2026-09-12T14:52:49.671Z'
        message: >-
          ⚠️ Tool failed: mcp__event-horizon__change_status — RUNE-1 stays in
          Grooming — deterministic plan lint bounced this move (free check, no
          LLM session spawned). Fix these and retry:

          - **B2**: Missing a `## Acceptance criteria` heading with at least one
          G
      - timestamp: '2026-09-12T14:52:52.090Z'
        message: Working
        type: info
      - timestamp: '2026-09-12T14:53:07.097Z'
        message: Working
        type: info
      - timestamp: '2026-09-12T14:54:07.357Z'
        message: >-
          **RUNE-1 is groomed and handed to the plan-review gate** — it runs
          automatically, then flags you to confirm the move to Todo.


          What changed, beyond restructuring the plan:


          **Two of the ticket's own assumptions were wrong and are now fixed.**
          The filing named `tests/OcrPricingSimulator/` as the spike harness —
          it's a text-only pricing tool with zero image code, and no PNG-driven
          OCR harness or fixture exists anywhere in the repo. Building one
          (`tests/RuneIconProbe/`) is now an explicit step, not an assumed
          starting point. Conversely, the plan got *simpler* in one place:
          `preprocessed` shares the raw frame's exact coordinate space, so
          there's no raw↔preprocessed remapping to write.


          **The spike gate now has numbers.** "Reliably separable" and "stable"
          were unfalsifiable as written. They're now ≥24px cells, zero-overlap
          gilded metric over ≥30 samples per class, and dHash Hamming ≤2 / ≤4 /
          ≥10. That last bound is the one worth knowing about — without a
          cross-shape *minimum*, "stable" is trivially satisfied by a hash that
          returns the same value for every glyph.


          **The one thing that will stall this card: fixtures.** Checks (b) and
          (c) are questions about real game art, and there are no screenshots in
          the repo. Check (a) is pure arithmetic over the resolution profiles
          and needs nothing, so the plan sequences that plus the harness, the
          fingerprinter, and the synthetic unit tests to proceed while captures
          are gathered — and instructs the implementer to park on Require Input
          and ask you for screenshots rather than fake the gate with synthetic
          art. **You'll likely get that request**: Combinations-panel captures
          with gilded runes across blue/purple/gold tiers, at two different
          resolutions, plus five consecutive frames of one unchanged panel.


          Closed decisions from SCRATCH-1 (composite key, raw-frame sampling,
          32×32 normalisation, no new dependency) were carried through untouched
          and marked as not-for-relitigation. Plan artifact rev 1 is on the
          ticket — data flow, strip geomet...
    user: Claude Code
    date: '2026-09-12T14:44:51.080Z'
    enginePid: 9336
    groupId: 3a85ffcd-2994-412c-b0b0-02d98d3feef1
    role: planner
    pattern: supervisor
    outcome: Claude Code session ended with code 0.
    endedAt: '2026-09-12T14:54:07.357Z'
    originalProgressCount: 10
    finalMessage: >-
      **RUNE-1 is groomed and handed to the plan-review gate** — it runs
      automatically, then flags you to confirm the move to Todo.


      What changed, beyond restructuring the plan:


      **Two of the ticket's own assumptions were wrong and are now fixed.** The
      filing named `tests/OcrPricingSimulator/` as the spike harness — it's a
      text-only pricing tool with zero image code, and no PNG-driven OCR harness
      or fixture exists anywhere in the repo. Building one
      (`tests/RuneIconProbe/`) is now an explicit step, not an assumed starting
      point. Conversely, the plan got *simpler* in one place: `preprocessed`
      shares the raw frame's exact coordinate space, so there's no
      raw↔preprocessed remapping to write.


      **The spike gate now has numbers.** "Reliably separable" and "stable" were
      unfalsifiable as written. They're now ≥24px cells, zero-overlap gilded
      metric over ≥30 samples per class, and dHash Hamming ≤2 / ≤4 / ≥10. That
      last bound is the one worth knowing about — without a cross-shape
      *minimum*, "stable" is trivially satisfied by a hash that returns the same
      value for every glyph.


      **The one thing that will stall this card: fixtures.** Checks (b) and (c)
      are questions about real game art, and there are no screenshots in the
      repo. Check (a) is pure arithmetic over the resolution profiles and needs
      nothing, so the plan sequences that plus the harness, the fingerprinter,
      and the synthetic unit tests to proceed while captures are gathered — and
      instructs the implementer to park on Require Input and ask you for
      screenshots rather than fake the gate with synthetic art. **You'll likely
      get that request**: Combinations-panel captures with gilded runes across
      blue/purple/gold tiers, at two different resolutions, plus five
      consecutive frames of one unchanged panel.


      Closed decisions from SCRATCH-1 (composite key, raw-frame sampling, 32×32
      normalisation, no new dependency) were carried through untouched and
      marked as not-for-relitigation. Plan artifact rev 1 is on the ticket —
      data flow, strip geomet...
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
  - type: activity
    user: Agent
    date: '2026-09-12T14:53:47.479Z'
    comment: Updated description.
    id: a-2026-09-12t14-53-47-479z
  - type: activity
    user: Plan Gate
    comment: >-
      Plan-review gate on — looping review → revise until approved (approval
      flags you to confirm), or parking after 2 revise attempt(s).
    date: '2026-09-12T14:53:54.410Z'
    id: a-2026-09-12t14-53-54-410z
  - type: activity
    user: Furnace
    date: '2026-09-12T14:53:54.436Z'
    comment: "\U0001F3AF Launch focus: You are reviewing a TICKET PLAN, not committed code — this ticket is still in Grooming and has no diff. Read its full description (title, body, `## Acceptance criteria`) and its latest published artifact (if any) as the plan under review. Depth: thorough. Full method for each check below: `read_skill('orchestrator', 'Plan-review methodology')`. Anchor check: verify every cited file/symbol/line still exists and means what the plan says — re-derive fresh every pass, never trust a prior citation. Artifact check (FLUX-1313): a plan artifact revision has already been published for this ticket (confirmed deterministically by the pre-gate lint) — no gap here regardless of how UI/UX-shaped the plan reads. Reground (FLUX-1048): check `.docs/release-notes/INDEX.md` + sibling/recently-Done tickets for work that already landed part of this plan. Acceptance-criteria coverage: confirm the AC checklist is testable and every item is addressed by the plan. Consequence tracing: for every destination this plan moves content/config into, name who consumes it and confirm the move still serves the plan's goal. Duplicate check: search open/groomed tickets for one that already covers this same scope. Adversarial self-review: read the plan as its harshest critic — flag weak/missing/wrong steps, unstated hard-to-reverse decisions, and judgment calls the plan ducked. Record your verdict via `change_status` — leave `newStatus` as \"Grooming\" (do NOT move the ticket) and set `planReviewState` to \"approved\" or \"changes-requested\" (never `reviewState`; that is a different field for the post-Todo code-review gate). Posting a comment that starts with **APPROVED** or **CHANGES NEEDED** is not enough by itself — without the `change_status` call the ticket will be parked for a human over an unrecorded verdict. Deterministic lint already ran and flagged this (non-blocking — already reflected in the artifact check above where applicable):\n- **W2**: Body is 12348 chars (soft limit 10000). Usual causes: a constraint restated across sections instead of stated once (FLUX-1582), revision-archaeology from a prior plan-review round (FLUX-1583), or dense line-number citations in place of stable symbol anchors (FLUX-1582). Worth a trim, but not by itself grounds to reject an otherwise-sound plan."
    summary: >-
      Launch focus recorded (2289 chars) — methodology detail via
      read_skill('orchestrator', 'Plan-review methodology'). Full text
      recoverable via expand.
    id: a-2026-09-12t14-53-54-436z
  - type: agent_session
    sessionId: 57b656d1-252e-4473-9589-47abb4a4e199
    startedAt: '2026-09-12T14:53:54.436Z'
    status: completed
    progress:
      - timestamp: '2026-09-12T14:54:10.210Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T14:54:25.210Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T14:54:40.216Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T14:54:55.221Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T14:55:40.237Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T14:55:55.238Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T14:56:10.242Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T14:56:25.250Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T14:56:40.253Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T14:56:55.252Z'
        message: Working
        type: info
      - timestamp: '2026-09-12T14:57:10.268Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T14:57:25.275Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T14:58:10.294Z'
        message: Working
        type: info
      - timestamp: '2026-09-12T14:59:17.882Z'
        message: >-
          **CHANGES NEEDED** — verdict recorded (`planReviewState:
          changes-requested`, ticket left in Grooming). Full findings are in a
          pinned comment on RUNE-1.


          The plan's code anchors are almost all correct — I re-derived every
          one against `4930b67` and confirmed the `textColX` snippet, the 5
          resolution profiles, the same-coordinate-space claim, the absolute row
          Ys, the snapshot record shape, the construction-site counts, and the
          "OcrPricingSimulator has no image code" correction. Two load-bearing
          assumptions fail:


          **Blocker 1 — the keys won't line up with the rows Card B scores.**
          `OcrTextPostProcessor.ExtractFromRowTexts` drops rows whose OCR text
          is under 3 chars or has no letter, so `ItemNames` is a *filtered
          subset* of the `rowYs` the plan says to reuse. A per-`rowYs` key list
          is misindexed the moment any row fails OCR, and `PriceRowLayout`
          indexes `ItemNames[i]` positionally — silently pairing the wrong runes
          with the wrong combination row, exactly the "actively wrong advice"
          failure the plan's own fail-soft rationale exists to prevent.


          **Blocker 2 — the gate tests the hash but never the crop.** Nothing
          validates that 5 equal cells across `[0, textColX)` actually bound the
          sprites. `textColX` comes from a fraction tuned to keep icons out of
          *row-width* detection, not a measured strip edge; and the row band's
          height is derived from dark pixels found *only right of* `textColX` —
          the text line — so it may clip the icon's border ring, which is what
          check (b) measures. Both (b) and (c) can pass on hand-cropped samples
          while the live path fails.


          Four Majors follow, the sharpest being that the "cell count ≠ 5"
          fail-soft can never fire under equal-fifths slicing, and that RUNE-2's
          catalog needs a cropped sprite per entry that RUNE-1's `(shapeHash,
          hueBucket)` contract doesn't emit.


          Worth noting: **the approach itself is sound.** Scope, sequencing, the
          closed-decisions section, and the no-new-capture-path constraints
          should be kept as-is — the fix list is about geometry v...
    user: Claude Code
    date: '2026-09-12T14:53:54.436Z'
    enginePid: 9336
    outcome: Claude Code session ended with code 0.
    endedAt: '2026-09-12T14:59:17.882Z'
    originalProgressCount: 14
    finalMessage: >-
      **CHANGES NEEDED** — verdict recorded (`planReviewState:
      changes-requested`, ticket left in Grooming). Full findings are in a
      pinned comment on RUNE-1.


      The plan's code anchors are almost all correct — I re-derived every one
      against `4930b67` and confirmed the `textColX` snippet, the 5 resolution
      profiles, the same-coordinate-space claim, the absolute row Ys, the
      snapshot record shape, the construction-site counts, and the
      "OcrPricingSimulator has no image code" correction. Two load-bearing
      assumptions fail:


      **Blocker 1 — the keys won't line up with the rows Card B scores.**
      `OcrTextPostProcessor.ExtractFromRowTexts` drops rows whose OCR text is
      under 3 chars or has no letter, so `ItemNames` is a *filtered subset* of
      the `rowYs` the plan says to reuse. A per-`rowYs` key list is misindexed
      the moment any row fails OCR, and `PriceRowLayout` indexes `ItemNames[i]`
      positionally — silently pairing the wrong runes with the wrong combination
      row, exactly the "actively wrong advice" failure the plan's own fail-soft
      rationale exists to prevent.


      **Blocker 2 — the gate tests the hash but never the crop.** Nothing
      validates that 5 equal cells across `[0, textColX)` actually bound the
      sprites. `textColX` comes from a fraction tuned to keep icons out of
      *row-width* detection, not a measured strip edge; and the row band's
      height is derived from dark pixels found *only right of* `textColX` — the
      text line — so it may clip the icon's border ring, which is what check (b)
      measures. Both (b) and (c) can pass on hand-cropped samples while the live
      path fails.


      Four Majors follow, the sharpest being that the "cell count ≠ 5" fail-soft
      can never fire under equal-fifths slicing, and that RUNE-2's catalog needs
      a cropped sprite per entry that RUNE-1's `(shapeHash, hueBucket)` contract
      doesn't emit.


      Worth noting: **the approach itself is sound.** Scope, sequencing, the
      closed-decisions section, and the no-new-capture-path constraints should
      be kept as-is — the fix list is about geometry v...
  - type: comment
    user: Agent
    comment: >-
      Grooming complete — plan rewritten and grounded against current code by a
      Context Scout + Requirements Interrogator pass (both findings pinned
      above).


      Two of the original filing's assumptions were corrected and are now folded
      into the plan:

      1. `tests/OcrPricingSimulator/` is a text-only pricing tool with zero
      image code — it cannot run this spike, and no PNG-driven OCR harness or
      OCR fixture exists in the repo. Building `tests/RuneIconProbe/` is now
      step 2.

      2. `preprocessed` shares `capturedBitmap`'s coordinate space (both
      allocated from `source.Width`/`source.Height`), so no raw↔preprocessed
      coordinate remapping is needed — simpler than the filing assumed.


      Spike checks now carry numeric pass bars (≥24px cells; zero-overlap gilded
      metric over ≥30 samples per class; dHash Hamming ≤2 same-profile, ≤4
      cross-profile, ≥10 cross-shape). The ≥10 cross-shape bound was added
      because without it "stable" is satisfiable by a degenerate always-equal
      hash.


      Sequencing note for the implementer: check (a) is pure arithmetic and
      needs no screenshots — do it first. Checks (b) and (c) need real game
      captures that are not in the repo; if none are available at step 3, move
      to Require Input and ask the user rather than substituting synthetic art.


      No blocking questions. Plan artifact rev 1 published (data flow, strip
      geometry, spike gate, fail-soft contract).
    date: '2026-09-12T14:53:55.113Z'
    id: c-2026-09-12t14-53-55-113z
  - type: comment
    user: Agent
    comment: >-
      **CHANGES NEEDED** — the plan's code anchors are almost all correct, but
      two load-bearing assumptions are wrong: **the rune keys won't line up with
      the rows Card B scores**, and **the spike gate never tests the one thing
      that can sink the card — whether the derived cells actually land on the
      rune sprites**.


      PLAN REVIEW — RUNE-1 (plan-review gate, attempt 1). Reviewed: ticket body
      + artifact rev 1, re-derived fresh against working tree at `4930b67`.


      ---


      ## Blocker 1 — `RuneKeys` will not index-align with `ItemNames`


      `src/OCR/OcrTextPostProcessor.cs:51-66` (`ExtractFromRowTexts`) **drops**
      any row whose normalised OCR text is `<3` chars or has no letter:


      ```csharp

      if (trimmed.Length >= 3 && ContainsLetter(trimmed))

      { names.Add(trimmed); yPositions.Add(rowYPositions[i]); }

      ```


      So `LeagueWindowSnapshot.ItemNames` / `RowYPositions` are a **filtered
      subset** of the `rowYs` that step 5 tells the implementer to reuse. A
      `RuneKeys` list built one-per-`rowYs[i]` is therefore misindexed relative
      to `ItemNames` whenever any row fails OCR.
      `src/Overlay/PriceRowLayout.cs:17-22` indexes `snapshot.ItemNames[i]`
      positionally against rows, and RUNE-2 scores "each gilded rune in the row"
      — so this silently pairs the wrong runes with the wrong combination row.
      That is precisely the "a wrong key produces actively wrong advice" failure
      the plan's own fail-soft rationale is written to prevent.


      **Fix:** make the emitted element carry its own row anchor (e.g.
      `IReadOnlyList<RuneRowKeys>` where `RuneRowKeys(int RowY,
      IReadOnlyList<RuneKey> Keys)`) and have the snapshot assembly join on the
      same Y values `ExtractFromRowTexts` retained — or build keys only for
      surviving rows. The acceptance criterion "Keys reach Card B in row order"
      must name the alignment key instead of assuming positional order.


      ## Blocker 2 — the spike gate never validates cell localisation


      Checks (a), (b), (c) cover cell *size*, border-metric *separability*, and
      hash *stability*. Nothing tests the assumption everything else rests on:
      that **5 equal-width cells spanning `x ∈ [0, textColX)` of the row band
      bound the 5 rune sprites**. Two concrete reasons they may not:


      - **`textColX` is a tuning heuristic, not a measured strip boundary.**
      `src/OCR/OcrOptions.cs:48` — `PanelLeftFraction = 0.30`, applied at
      `src/OCR/OcrLeagueWindowReader.cs:660`. It was chosen to keep icons out of
      row-*width* detection. Nothing establishes the icons fill `[0, textColX)`
      edge-to-edge with no panel chrome or padding, which equal-fifths slicing
      requires.

      - **The row band's height is text-derived, not icon-derived.**
      `OcrPipeline.DetectRowPositions` (`src/OCR/OcrPipeline.cs:60-75`) counts
      dark pixels **only inside `contentCrop` — i.e. right of `textColX`, the
      text column** — then expands the band ±4px
      (`src/OCR/OcrPipeline.cs:118-125`). The band tracks the text line. Nothing
      guarantees it covers the icon vertically, and a band that clips the icon
      top or bottom removes part of the border ring that check (b)'s gilded
      metric measures.


      Consequence: (b) and (c) can both pass on hand-cropped sample images and
      the feature still fails in the live path, because the gate proved the
      *hash* works without ever proving the *crop* works.


      **Fix:** add gate check **(a2)**, ordered before (b)/(c): on a real
      fixture at ≥2 profiles, dump the 5 derived cell rects as an overlay on `1
      Raw.png` and confirm each bounds one rune sprite with its full border
      ring. Make (b)/(c) conditional on (a2) passing.


      ---


      ## Major 3 — AC (a)'s `≥24×24` is not what step 1 tests


      AC (a) requires each cell be `≥24×24` raw px. Step 1 asserts only
      `textColX / 5 ≥ 24` — **width**. Cell height is `rowHeights[i]`, produced
      empirically by text-band detection, so it is not analytically derivable
      and the step-1 unit test cannot cover it. Split (a) into an analytic width
      check (step 1) and a height check that moves into the fixture phase.


      For the record, I ran the width arithmetic — it passes, but with a caveat
      worth writing into the plan:


      | profile | `CaptureWidth` | `textColX` | cell width |

      |---|---|---|---|

      | 1600x900 | 414 | 124 | **24** |

      | 1920x1080 | 497 | 149 | 29 |

      | 2560x1440 | 663 | 198 | 39 |

      | 3440x1440 | 663 | 198 | 39 |

      | 3840x2160 | 994 | 298 | 59 |


      (Confirmed `OcrCaptureRegionResolver.Resolve` passes
      `profile.CaptureWidth` through unscaled, and both preprocessing steps
      preserve dimensions — so `preprocessed.Width == CaptureWidth`.) **1600x900
      lands exactly on the bar with zero margin**, and `PanelLeftFraction` is a
      user-settable `OcrOptions` property — any configured value below ~0.29
      drops it under 24px at runtime while a unit test pinned to the 0.30
      default still passes. The step-1 test should read the default from `new
      OcrOptions()` rather than hardcoding 0.30, and the runtime path needs a
      guard.


      ## Major 4 — the "cell count ≠ 5" fail-soft criterion is unsatisfiable


      "A row yielding a cell count ≠ 5 is skipped and logged" cannot fire:
      equal-fifths slicing of a fixed strip **always** yields exactly 5 cells,
      except on a degenerate strip narrower than 5px. So the criterion is
      untestable as written and gives no protection against the failures it
      appears aimed at — a row with fewer than 5 runes, or two combination rows
      merged into one band by the `<= 10px` merge at
      `src/OCR/OcrPipeline.cs:106-113`. Replace it with a predicate that can
      actually fail (e.g. skip when strip width `< 5 × MinCellPx`, plus a guard
      on `rowHeights[i]` exceeding a plausible single-row maximum to catch
      merged bands), or drop it.


      ## Major 5 — step 2's harness cannot compile as described


      `OcrPipeline` (`src/OCR/OcrPipeline.cs:6`) and `OcrImagePreprocessor`
      (`src/OCR/OcrImagePreprocessor.cs:8`) are both **`internal static
      class`**. `RuneshapePriceChecker.csproj:20-23` grants `InternalsVisibleTo`
      to `RuneshapePriceChecker.Tests`, `RuneshapePriceChecker.Dashboard` and
      `CrashSimulator` only. A new `tests/RuneIconProbe/` console project gets
      no access, and the new `src/OCR/RuneIconFingerprinter.cs` will be internal
      for the same reason. The plan must add the `InternalsVisibleTo
      Include="RuneIconProbe"` entry explicitly.


      Related judgment call the plan ducked: `RuneshapePriceChecker.Tests`
      **already** has internals access, and the "fixture regression" item under
      Recommended tests is an xUnit test regardless. State why a separate
      console exe is needed beyond eyeballing dumps, or fold the harness into
      the existing test project. The stated rationale ("the pricing simulator is
      single-purpose text/pricing") argues against extending
      `OcrPricingSimulator` — confirmed, it has zero `Bitmap`/`System.Drawing`
      references — but it does not argue for a new project over the test
      project.


      ## Major 6 — RUNE-2 needs a cropped sprite this card does not emit


      RUNE-2's body specifies catalog entries `{ id, displayName, spriteRef,
      dHash, hueBucket, weight }` and an auto-discovery sink that banks "unseen
      keys from RUNE-1 **with their cropped sprite**". RUNE-1 emits only
      `(shapeHash, hueBucket)` and writes cell images *only* under
      `SaveDebugImages` — "With it off, no new file I/O." As specified, RUNE-2's
      dashboard cannot render a sprite for a newly discovered rune. Decide it
      here, not at Card B time: either the emitted element carries the
      normalised 32×32 cell (or enough to re-derive it), or RUNE-2's `spriteRef`
      requirement is renegotiated and that is written down.


      ---


      ## Minor


      7. **`5 IconCells.png` collides with an existing file.**
      `OcrPipeline.SaveRowOverlayDebugImage` already writes `"5 Rows.png"`
      (`src/OCR/OcrPipeline.cs:162`). The real sequence is `1 Raw.png`, `2
      Mask.png`, `3 Preprocessed.png`, `4 Cropped.png`, `5 Rows.png`, plus
      `Row{n}.png`. Use `6 IconCells.png`.

      8. **`UpscaleForOcr` misattributed.** It is defined at
      `src/OCR/OcrImagePreprocessor.cs:269`; the `UpscaleForOcr(rowBitmap, 3)`
      *call* is at `src/OCR/OcrPipeline.cs:25` inside `PrepareRowBitmap`. The
      plan's substantive point — upscale runs after row detection, per-row, for
      Tesseract only — is correct.

      9. **The carrier out of `CaptureAndRecognize` is unstated.**
      `CaptureAndRecognize` returns `string`
      (`src/OCR/OcrLeagueWindowReader.cs:418`); the snapshot is built in a
      different method at line 389. Step 5 says "before the method returns" and
      step 6 says "thread it through the construction site" without naming the
      hand-off. Name the field, following `_lastCropBounds` /
      `_lastOcrRowYPositions`. The frame-differencing cache path needs no extra
      work — `src/OCR/OcrLeagueWindowReader.cs:246` returns `_lastSnapshot`
      wholesale, so keys ride along correctly on a cache hit. Worth stating so
      the implementer doesn't re-derive it.

      10. **Body is 12,348 chars vs the 10,000 soft limit.** The "do not
      re-open" / "do not add any" / "out of scope" constraints are each restated
      two or three times across sections. Trimming the duplication gets under
      the limit without losing content.


      ---


      ## Anchors confirmed — do not re-verify


      `textColX` code verbatim at `src/OCR/OcrLeagueWindowReader.cs:660-663`.
      `PanelLeftFraction = 0.30` at `src/OCR/OcrOptions.cs:48`,
      `SaveDebugImages` at line 26. The 5 profiles are exactly
      1600x900/1920x1080/2560x1440/3440x1440/3840x2160. `KeepBlackAndNeighbors`
      and `PreprocessForOcr` both allocate at `source.Width`/`source.Height` —
      **the same-coordinate-space claim holds, no remapping needed**.
      `DetectRowPositions` returns **absolute** Ys (`result[r] = scanY + start`,
      `src/OCR/OcrPipeline.cs:123`), so row Ys apply to the raw frame directly.
      `LeagueWindowSnapshot` is a positional record — a trailing optional field
      is genuinely additive. Construction sites: 5 in
      `OcrLeagueWindowReader.cs`, 3 in `src/App/LeaguePricingWorker.cs`, 10 in
      `tests/src/Overlay/PriceRowLayoutTests.cs` (plan said "~5 / ~3 / plus
      consumers" — correct). `IsLikelyTextColor`
      (`src/OCR/OcrImagePreprocessor.cs:604`) is RGB distance + luminance,
      nothing hue-reusable. `tests/OcrPricingSimulator/` has zero
      `Bitmap`/`System.Drawing` references — the grooming correction was right.


      **Reground:** no `.docs/release-notes/` directory exists in this repo, so
      there is no landed work overlapping this plan. **Duplicate check:** the
      board holds only SCRATCH-1, RUNE-1 and RUNE-2; RUNE-2 is the declared
      consumer, not a duplicate. **Artifact:** rev 1 present.


      **Scope, sequencing, the closed-decisions section, and the "no new capture
      path / no new dependency" constraints are all sound and should be kept
      as-is.** The fix list above is about geometry validation and the output
      contract, not about the approach.
    date: '2026-09-12T14:58:54.532Z'
    selfAttested: true
    summary: >-
      PLAN REVIEW: CHANGES NEEDED. 2 Blockers (RuneKeys won't index-align with
      ItemNames because ExtractFromRowTexts filters rows; spike gate never
      validates cell localisation — textColX is a heuristic and row bands are
      text-derived, not icon-derived), 4 Majors (AC(a) ≥24x24 vs width-only
      step-1 test; the "≠5 cells" fail-soft is unsatisfiable under equal-fifths
      slicing; RuneIconProbe can't see internal OcrPipeline/OcrImagePreprocessor
      without InternalsVisibleTo; RUNE-2 needs a cropped sprite RUNE-1 doesn't
      emit), 4 Minors. All other anchors confirmed correct.
    pin: true
    id: c-2026-09-12t14-58-54-532z
  - type: comment
    user: Agent
    comment: >-
      Plan review attempt 1: CHANGES NEEDED. 2 Blockers — (1) RuneKeys built per
      rowYs will not index-align with LeagueWindowSnapshot.ItemNames because
      OcrTextPostProcessor.ExtractFromRowTexts
      (src/OCR/OcrTextPostProcessor.cs:51-66) filters rows with <3 chars or no
      letter, so Card B would pair runes with the wrong combination row; emit a
      row-Y anchor instead of relying on positional order. (2) The spike gate
      never validates cell localisation — textColX derives from the
      PanelLeftFraction=0.30 tuning heuristic, and row bands come from
      text-pixel detection right of textColX (src/OCR/OcrPipeline.cs:60-75), so
      neither the horizontal extent nor the vertical coverage of the icon is
      established; add a check (a2) that the 5 derived rects bound the sprites,
      ordered before (b)/(c). 4 Majors: AC(a)'s ≥24x24 vs a width-only step-1
      test (width arithmetic does pass, but 1600x900 lands exactly on 24px with
      zero margin and PanelLeftFraction is user-settable); the "cell count ≠ 5"
      fail-soft is unsatisfiable under equal-fifths slicing;
      tests/RuneIconProbe/ cannot see internal OcrPipeline/OcrImagePreprocessor
      without a new InternalsVisibleTo entry; RUNE-2 requires a cropped sprite
      per catalog entry that RUNE-1's (shapeHash, hueBucket) contract does not
      emit. 4 Minors incl. the proposed "5 IconCells.png" colliding with the
      existing "5 Rows.png". Full findings with file:line evidence and confirmed
      anchors in the pinned review comment.
    date: '2026-09-12T14:59:05.077Z'
    id: c-2026-09-12t14-59-05-077z
  - type: activity
    user: Furnace
    date: '2026-09-12T14:59:20.599Z'
    comment: "\U0001F3AF Launch focus: A plan-review pass just requested changes on this ticket's plan (see the latest review comment in its history) — revise the ticket body via `update_ticket` to address every point raised, then STOP. Do not call `change_status` yourself and do not start implementing; the plan-review gate automatically re-reviews your revision. Write the revision as if the plan had been right the first time — ticket history already records what changed; never annotate the body with what a prior draft got wrong or which review round/annotation resolved a point. When revising an artifact: revise minimally — answer every annotation explicitly, show the annotated element before→after, and never silently redesign elements the user already approved."
    id: a-2026-09-12t14-59-20-599z
  - type: agent_session
    sessionId: 89655259-5e90-4d9a-9890-8a86b792cf1b
    startedAt: '2026-09-12T14:59:20.599Z'
    status: completed
    progress:
      - timestamp: '2026-09-12T14:59:36.188Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T14:59:51.193Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T15:00:06.192Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T15:00:21.192Z'
        message: Working
        type: info
      - timestamp: '2026-09-12T15:00:36.195Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T15:01:51.231Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T15:02:51.246Z'
        message: Editing
        type: info
      - timestamp: '2026-09-12T15:03:51.276Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T15:04:06.277Z'
        message: Editing
        type: info
      - timestamp: '2026-09-12T15:05:06.316Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T15:05:36.329Z'
        message: Editing
        type: info
      - timestamp: '2026-09-12T15:06:36.339Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T15:06:51.347Z'
        message: Editing
        type: info
      - timestamp: '2026-09-12T15:07:36.376Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T15:07:51.378Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T15:08:36.397Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T15:08:56.836Z'
        message: >-
          ⚠️ Tool failed: Bash — Exit code 2

          /usr/bin/bash: -c: line 20: unexpected EOF while looking for matching
          `''
      - timestamp: '2026-09-12T15:09:21.416Z'
        message: Editing
        type: info
      - timestamp: '2026-09-12T15:10:06.436Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T15:10:21.447Z'
        message: Editing
        type: info
      - timestamp: '2026-09-12T15:10:51.472Z'
        message: Working
        type: info
      - timestamp: '2026-09-12T15:11:51.504Z'
        message: Working
        type: info
      - timestamp: '2026-09-12T15:12:36.531Z'
        message: Working
        type: info
      - timestamp: '2026-09-12T15:13:18.248Z'
        message: >-
          RUNE-1 is groomed and back in **Todo**. All ten review findings are
          resolved in the plan — nothing deferred.


          **The two blockers:**


          1. **Key/row misalignment** —
          `OcrTextPostProcessor.ExtractFromRowTexts` filters rows out of
          `ItemNames`, so positional keys would pair runes with the wrong
          combination row. The output is now `RuneRowKeys(int RowY,
          IReadOnlyList<RuneKey> Keys)`, filtered at snapshot construction to
          rows whose `RowY` appears in `matchedYPositions` — the same join key
          `ExtractFromRowTexts` retains.


          2. **Crop never validated** — added gate check **(a2)**: overlay the
          five derived rects on `1 Raw.png` at ≥2 profiles and confirm each
          bounds one sprite with its full border ring. (b) and (c) are
          explicitly gated on it, since both can pass on hand-cropped images
          while the live path still fails.


          **The majors:** (a1) is now a width-only analytic test that reads
          `PanelLeftFraction` from `new OcrOptions()` rather than hardcoding
          0.30 — 1600x900 lands on exactly 24px with zero margin and the option
          is user-settable. The unsatisfiable "cell count ≠ 5" is replaced by
          three predicates that can fire (degenerate strip, band too short, band
          fused by the ≤10px merge). The probe folds into
          `RuneshapePriceChecker.Tests`, which already has the
          `InternalsVisibleTo` grant the internal OCR types need — no new
          project, no new grant. And `RuneKey` now carries its normalised 32×32
          sprite bytes, which RUNE-2's auto-discovery sink requires and the old
          contract didn't emit.


          Two things worth flagging:


          - **The body is 12,935 chars, over the 10,000 soft limit.** I cut
          every restated constraint the review named, but the six fixes added
          more than the duplication removed. It's roughly the same size as
          before with substantially more content.

          - **Steps 3 onward are blocked on screenshots only you can produce** —
          gilded runes across blue/purple/gold tiers at ≥2 resolutions, plus ≥5
          consecutive frames of one unchanged panel. The plan tells the
          implementer to do the arithmetic check and the...
    user: Claude Code
    date: '2026-09-12T14:59:20.599Z'
    enginePid: 9336
    outcome: Claude Code session ended with code 0.
    endedAt: '2026-09-12T15:13:18.248Z'
    originalProgressCount: 26
    finalMessage: >-
      RUNE-1 is groomed and back in **Todo**. All ten review findings are
      resolved in the plan — nothing deferred.


      **The two blockers:**


      1. **Key/row misalignment** — `OcrTextPostProcessor.ExtractFromRowTexts`
      filters rows out of `ItemNames`, so positional keys would pair runes with
      the wrong combination row. The output is now `RuneRowKeys(int RowY,
      IReadOnlyList<RuneKey> Keys)`, filtered at snapshot construction to rows
      whose `RowY` appears in `matchedYPositions` — the same join key
      `ExtractFromRowTexts` retains.


      2. **Crop never validated** — added gate check **(a2)**: overlay the five
      derived rects on `1 Raw.png` at ≥2 profiles and confirm each bounds one
      sprite with its full border ring. (b) and (c) are explicitly gated on it,
      since both can pass on hand-cropped images while the live path still
      fails.


      **The majors:** (a1) is now a width-only analytic test that reads
      `PanelLeftFraction` from `new OcrOptions()` rather than hardcoding 0.30 —
      1600x900 lands on exactly 24px with zero margin and the option is
      user-settable. The unsatisfiable "cell count ≠ 5" is replaced by three
      predicates that can fire (degenerate strip, band too short, band fused by
      the ≤10px merge). The probe folds into `RuneshapePriceChecker.Tests`,
      which already has the `InternalsVisibleTo` grant the internal OCR types
      need — no new project, no new grant. And `RuneKey` now carries its
      normalised 32×32 sprite bytes, which RUNE-2's auto-discovery sink requires
      and the old contract didn't emit.


      Two things worth flagging:


      - **The body is 12,935 chars, over the 10,000 soft limit.** I cut every
      restated constraint the review named, but the six fixes added more than
      the duplication removed. It's roughly the same size as before with
      substantially more content.

      - **Steps 3 onward are blocked on screenshots only you can produce** —
      gilded runes across blue/purple/gold tiers at ≥2 resolutions, plus ≥5
      consecutive frames of one unchanged panel. The plan tells the implementer
      to do the arithmetic check and the...
  - type: activity
    user: Agent
    date: '2026-09-12T15:11:41.161Z'
    comment: Updated description.
    id: a-2026-09-12t15-11-41-161z
  - type: activity
    user: Agent
    comment: >-
      Published artifact revision 2 — RUNE-1 plan rev 2 — crop validation gate +
      row-Y join (9,605 bytes).
    date: '2026-09-12T15:12:34.230Z'
    id: a-2026-09-12t15-12-34-230z
  - type: activity
    user: Agent
    date: '2026-09-12T15:12:58.584Z'
    comment: >-
      PLAN REVISION — RUNE-1, addressing plan-review attempt 1. Body rev 2 and
      artifact rev 2 published. Every finding resolved; nothing deferred.


      **Blocker 1 — key/row misalignment.** Output is now `RuneRowKeys(int RowY,
      IReadOnlyList<RuneKey> Keys)`. At the snapshot construction site the list
      is filtered to rows whose `RowY` appears in `matchedYPositions` — the same
      key `OcrTextPostProcessor.ExtractFromRowTexts` retains — so it aligns 1:1
      with `ItemNames`, and `RowY` stays on the record so RUNE-2 asserts rather
      than assumes. Plan step 6; a regression test for the filtered-row case is
      in Recommended tests.


      **Blocker 2 — cell localisation never tested.** New gate check **(a2)**:
      dump the 5 derived rects as an overlay on `1 Raw.png` at ≥2 profiles and
      confirm each bounds one sprite with its full border ring. (b) and (c) are
      explicitly gated on it; plan step 3 states the reason (textColX is a
      tuning heuristic, and the band is derived from text pixels right of
      textColX then expanded ±4px).


      **Major 3 — ≥24×24.** Split. (a1) is the analytic width check in step 1
      and **reads `PanelLeftFraction` from `new OcrOptions()`** rather than
      hardcoding 0.30; the 1600x900 zero-margin case (414 → 124 → 24) is written
      into the step and into the artifact's profile table. Height is covered by
      (a2) plus the runtime `MinCellPx` guard.


      **Major 4 — unsatisfiable fail-soft.** "cell count ≠ 5" removed, with a
      sentence explaining why it could never fire. Replaced by three predicates
      that can: `stripWidth < 5 × MinCellPx`; `rowHeights[i] < MinCellPx`;
      `rowHeights[i] > 3 × (stripWidth / 5)` (catches bands fused by the ≤10px
      merge in `DetectRowPositions`).


      **Major 5 — harness cannot compile.** `tests/RuneIconProbe/` dropped. The
      probe goes into `RuneshapePriceChecker.Tests` (`tests/Tests.csproj`),
      which already holds the `InternalsVisibleTo` grant the internal
      `OcrPipeline`/`OcrImagePreprocessor`/`RuneIconFingerprinter` need, plus
      xUnit and WinForms. No new csproj, no new grant. Rationale for not
      extending `OcrPricingSimulator` kept and the "why not the test project"
      gap closed.


      **Major 6 — RUNE-2 needs a sprite.** Decided here: `RuneKey(ulong
      ShapeHash, int HueBucket, byte[] Sprite32Rgb)`. The normalised 32×32 cell
      is already computed en route to the hash, so carrying it costs ~3 KB per
      gilded rune and is the only way RUNE-2's auto-discovery sink can render a
      newly discovered rune. RUNE-2's `spriteRef` requirement stands unchanged.


      **Minors.** `6 IconCells.png` (7 collided with `5 Rows.png`).
      `UpscaleForOcr` attributed to `OcrImagePreprocessor.cs`, called from
      `OcrPipeline.PrepareRowBitmap`. Carrier field named `_lastRuneRowKeys`,
      alongside `_lastOcrRowYPositions`/`_lastCropBounds`, with the note that
      the cache path returns `_lastSnapshot` wholesale so keys ride along. Body
      duplication trimmed hard — it still lands at 12,935 chars because the six
      fixes above added load-bearing content; the restated constraints the
      review flagged are gone.


      Scope, sequencing, closed decisions and the no-new-capture-path
      constraints are unchanged, as the review directed.
    summary: >-
      Plan-review attempt 1 findings all addressed in body rev 2 + artifact rev
      2. Blocker 1 fixed by RuneRowKeys(RowY, Keys) joined on matchedYPositions
      instead of positional order. Blocker 2 fixed by new gate check (a2)
      validating cell localisation on a real fixture, ordered before (b)/(c).
      Major 3: AC split into analytic (a1) width test reading PanelLeftFraction
      from new OcrOptions() plus fixture-phase height coverage. Major 4:
      unsatisfiable "cell count != 5" replaced by three firing predicates
      (stripWidth < 5*MinCellPx; rowHeights[i] < MinCellPx; rowHeights[i] >
      3*(stripWidth/5)). Major 5: probe folded into RuneshapePriceChecker.Tests
      (tests/Tests.csproj) which already has InternalsVisibleTo —
      tests/RuneIconProbe/ dropped, no new csproj. Major 6: RuneKey carries
      normalised 32x32 sprite bytes so RUNE-2 can render discovered runes.
      Minors: 6 IconCells.png, UpscaleForOcr attribution, _lastRuneRowKeys
      carrier field named, body trimmed 12348 -> 12935 chars while absorbing all
      new content.
    pin: true
    id: a-2026-09-12t15-12-58-584z
  - type: comment
    user: Agent
    comment: >-
      Plan revised against plan-review attempt 1 — both Blockers, all 4 Majors
      and all 4 Minors addressed. Body rev 2 and artifact rev 2 published.


      **The two structural fixes:** keys now carry a **row-Y anchor** and are
      joined on `matchedYPositions` instead of list position, so a row that
      fails OCR can no longer pair runes with the wrong combination row; and a
      new gate check **(a2)** proves the five derived cells actually land on the
      rune sprites before the border-metric and hash checks are allowed to run.


      Also settled here rather than deferred: the emitted key carries its
      normalised 32×32 sprite bytes (RUNE-2 can't render a newly discovered rune
      without it), the probe folds into the existing
      `RuneshapePriceChecker.Tests` project instead of a new console exe that
      couldn't see the internal OCR types, and the unsatisfiable "cell count ≠
      5" fail-soft is replaced by three predicates that can actually fire.


      **First step for the implementer is pure arithmetic** — the (a1) width
      check needs no screenshots. Checks (a2), (b) and (c) need real game
      captures that are not in the repo; if none are available at step 3, move
      to Require Input and ask rather than substituting synthetic art.
    date: '2026-09-12T15:13:06.222Z'
    id: c-2026-09-12t15-13-06-222z
  - type: status_change
    from: Grooming
    to: Todo
    user: Agent
    date: '2026-09-12T15:13:06.222Z'
  - type: activity
    user: Plan Gate
    comment: >-
      plan gate stopped — the ticket left Grooming (now Todo) while a run was in
      flight.
    date: '2026-09-12T15:13:06.666Z'
    id: a-2026-09-12t15-13-06-666z
  - type: comment
    user: User
    date: '2026-09-12T15:16:49.082Z'
    comment: tell me which screenshots i need to provide
    id: c-2026-09-12t15-16-49-082z
  - type: comment
    user: User
    date: '2026-09-12T15:21:00.313Z'
    comment: i put a bunch of pics in the description of the ticket
    id: c-2026-09-12t15-21-00-313z
  - type: activity
    user: Agent
    date: '2026-09-12T15:30:17.829Z'
    comment: Updated description.
    id: a-2026-09-12t15-30-17-829z
  - type: activity
    user: Agent
    date: '2026-09-12T15:30:42.352Z'
    comment: >-
      FIXTURE FINDINGS — RUNE-1. Four reference screenshots are attached to this
      ticket at `.flux-store/assets/RUNE-1/` (`image.png`, `image-2.png` —
      byte-identical duplicate of `image.png` —, `image-3.png`, `image-4.png`).
      **They invalidate the strip geometry in the plan body. Do not implement
      steps 1, 4 or checks (a1)/(a2) as currently written.**


      **1. Icon count per row is variable, not 5.** Observed across the four
      images: **2, 3, 4, 5 and 6** icons. `image.png` has six rows at
      3/3/3/2/2/2; `image-3.png` three rows at 3 each; `image-4.png` three rows
      at 6/5/4. Icons are left-packed at a fixed pitch. Equal-fifths slicing of
      the strip is the wrong model, and the plan's claim that "a cell count of
      ≠5 can never fire" is moot — the count is genuinely variable and must be
      *detected*, not assumed.


      **2. A wide row wraps its text onto a second line.** In `image-4.png` the
      6-icon row renders its icons on one line with "3x Divine Orb" on the line
      *below*; the 5-icon and 4-icon rows keep text beside the icons. So for
      wide rows the icon strip is vertically disjoint from the text.
      `OcrPipeline.DetectRowPositions` derives bands from dark pixels **only
      inside `crop`, i.e. right of `textColX`**, so the band it finds for such a
      row tracks the text line and will not contain that row's icons at all.


      **3. The strip extends past the `PanelLeftFraction` boundary.** Measured
      on `image-4.png` (779×298): the 6-icon strip's ink ends at **x≈355**, and
      the row bar spans **x≈66–687**. That puts the strip's right edge at **~46%
      of the panel width**, versus the `0.30` cut. `x ∈ [0, textColX)` truncates
      a 6-icon row to roughly its first four runes.


      **4. There are three border styles, not two.** Confirmed by 3×
      nearest-neighbour zoom on `image-4.png` row 1 and `image.png` rows 1–3:

      - **gold border with three small tabs along the top edge**

      - **blue border with square corner studs**

      - **plain thin orange-brown border**


      Gold and tan-brown are **adjacent hues**, so check (b)'s "proportion of
      border-ring pixels inside a gold hue+saturation range" has a far narrower
      separation than the plan assumed. The three top tabs are a *structural*
      feature and may be the more reliable discriminator. One cell in
      `image-4.png` row 1 carries a gold border **and** a blue/teal background
      fill, so the styles are not mutually exclusive.


      **5. Hue must be measured over glyph strokes, not the cell.** Glyph stroke
      colour varies (purple in `image-4.png`, near-black in
      `image.png`/`image-3.png`), but the cell is mostly parchment — a
      whole-cell dominant hue resolves to beige for every rune. Already
      corrected in the body's closed-decisions section.


      **6. Rows are cumulative.** In `image-4.png` the 4-, 5- and 6-icon rows
      are the same rune sequence growing by one, paying 1x / 2x / 3x Divine Orb.
      Relevant to RUNE-2's scoring, not to extraction.


      **These images cannot be used as probe fixtures** — they are cropped,
      zoomed screenshots, not capture-region frames. The `1 Raw.png` captures
      described in step 3 are still required.


      **Open blocking question with the user:** which border style marks a
      succession rune. Proposed default if unanswered: gold-with-tabs.
    summary: >-
      FIXTURE FINDINGS from the 4 reference screenshots attached to RUNE-1
      (.flux-store/assets/RUNE-1/image.png, image-2.png [byte-identical dup of
      image.png], image-3.png, image-4.png). These invalidate the plan's strip
      geometry before implementation. (1) Icons per row are VARIABLE — 2,3,4,5,6
      observed — left-packed at fixed pitch, so equal-fifths slicing is the
      wrong model. (2) A 6-icon row wraps its text to a second line, putting
      icons ABOVE the text band; DetectRowPositions finds bands from dark pixels
      right of textColX, so that row's band will not contain its icons. (3) The
      6-icon strip ends at ~46% of panel width (measured: icons end x=355, panel
      row bar x=66..687 in image-4.png, 779px wide), past the
      PanelLeftFraction=0.30 cut, so [0,textColX) truncates wide rows. (4) THREE
      border styles exist, not two: gold border with three tabs along the top
      edge; blue border with square corner studs; plain thin orange-brown. Gold
      vs tan-brown are adjacent hues, so a hue-proportion metric has a much
      narrower gap than check (b) assumed — the three tabs may be the reliable
      structural signal. (5) Glyph stroke colour varies (purple vs near-black)
      but the cell is mostly parchment, so hue must be measured over glyph
      strokes, not the whole cell. (6) Rows in image-4 are cumulative — same
      rune sequence growing by one per row (4,5,6 icons → 1x/2x/3x Divine Orb).
      The attached images are cropped/zoomed screenshots and CANNOT drive the
      probe; real 1 Raw.png capture-region frames are still required. Blocking
      question raised with the user: which border style marks a succession rune.
    pin: true
    id: a-2026-09-12t15-30-42-352z
  - type: comment
    user: Agent
    comment: >-
      **Your screenshots broke the plan's geometry — which is good news, because
      they broke it before anyone wrote code.** Findings are pinned on the
      ticket; the body now carries a blocker notice so nobody implements the old
      layout.


      The short version: rows carry **2 to 6 icons, not 5**, a 6-icon row wraps
      its text onto a second line so the icons sit above the text band, and that
      strip reaches **~46% of the panel width** — past the 30% cut the plan
      sliced within. The "5 equal cells" model has to be replaced with real icon
      detection.


      **One question before I replan.** I can see three border styles: **gold
      with three small tabs along the top edge**, **blue with square corner
      studs**, and **plain thin orange-brown**. Which marks a succession rune —
      the one that carries forward?


      My read is **gold-with-tabs**, and that is what I will assume if you would
      rather I just proceed. Worth knowing either way: gold and tan-brown are
      adjacent hues, so the gold-vs-plain split is a narrower colour gap than
      the plan assumed and the detector may need to key on the tabs instead. If
      the answer is **blue**, the separation is far easier and check (b) gets
      simpler.


      Still needed regardless: the real `1 Raw.png` capture-region frames. The
      attached images are cropped screenshots, so they can't drive the probe.
    date: '2026-09-12T15:30:50.974Z'
    id: c-2026-09-12t15-30-50-974z
  - type: swimlane_change
    swimlane: require-input
    action: set
    user: Agent
    date: '2026-09-12T15:30:50.974Z'
    comment: >-
      **Your screenshots broke the plan's geometry — which is good news, because
      they broke it before anyone wrote code.** Findings are pinned on the
      ticket; the body now carries a blocker notice so nobody implements the old
      layout.


      The short version: rows carry **2 to 6 icons, not 5**, a 6-icon row wraps
      its text onto a second line so the icons sit above the text band, and that
      strip reaches **~46% of the panel width** — past the 30% cut the plan
      sliced within. The "5 equal cells" model has to be replaced with real icon
      detection.


      **One question before I replan.** I can see three border styles: **gold
      with three small tabs along the top edge**, **blue with square corner
      studs**, and **plain thin orange-brown**. Which marks a succession rune —
      the one that carries forward?


      My read is **gold-with-tabs**, and that is what I will assume if you would
      rather I just proceed. Worth knowing either way: gold and tan-brown are
      adjacent hues, so the gold-vs-plain split is a narrower colour gap than
      the plan assumed and the detector may need to key on the tabs instead. If
      the answer is **blue**, the separation is far easier and check (b) gets
      simpler.


      Still needed regardless: the real `1 Raw.png` capture-region frames. The
      attached images are cropped screenshots, so they can't drive the probe.
  - type: comment
    user: User
    date: '2026-09-12T15:30:51.175Z'
    comment: the succesive rune is the one with the tabs on top.
    id: c-2026-09-12t15-30-51-175z
  - type: comment
    user: User
    comment: the succesive rune is the one with the tabs on top.
    date: '2026-09-12T15:30:51.296Z'
    id: c-2026-09-12t15-30-51-296z
  - type: swimlane_change
    swimlane: require-input
    action: cleared
    user: User
    date: '2026-09-12T15:30:51.296Z'
  - type: comment
    user: Agent
    comment: >-
      Reopening for replan: the succession rune is confirmed as the tabbed one,
      and the reference screenshots invalidated the strip geometry. Rewriting
      the cell model and the gilded metric.
    date: '2026-09-12T15:33:52.392Z'
    id: c-2026-09-12t15-33-52-392z
  - type: status_change
    from: Todo
    to: Grooming
    user: Agent
    date: '2026-09-12T15:33:52.392Z'
  - type: activity
    user: Agent
    comment: Reaped 1 stale parked session from an earlier phase on move to Grooming.
    date: '2026-09-12T15:33:52.458Z'
    id: a-2026-09-12t15-33-52-458z
  - type: agent_session
    sessionId: 1370bd62-d6ab-4cbf-89af-fe06d6383616
    startedAt: '2026-09-12T15:41:35.861Z'
    status: waiting-input
    progress:
      - timestamp: '2026-09-12T15:41:51.348Z'
        message: Thinking
        type: info
      - timestamp: '2026-09-12T15:41:55.941Z'
        message: >-
          There's already an active replanning session running on RUNE-1
          (started 2026-09-12 15:41, still in progress) — it picked up right
          after you confirmed **gold-with-tabs is the succession rune** and is
          now rewriting the cell model to handle the fixture findings: variable
          icon counts (2–6, not 5), the wide-row text-wrap issue, and the
          narrower gold-vs-tan-brown color gap.


          Nothing new has landed yet from that session. Quick recap of where
          things stand:


          - **Confirmed:** gold border with three top tabs = succession rune
          (your answer).

          - **Still open:** the plan needs a genuine icon-detection step instead
          of "5 equal cells," since row icon count varies and a 6-icon row's
          text wraps onto a second line (icons sit above the text band, not
          beside it).

          - **Still needed:** real `1 Raw.png` capture-region frames — the four
          screenshots you attached are cropped/zoomed and can't drive the probe
          fixture.


          Is there something specific you wanted to check on, or were you just
          looking for a status update?
    user: Claude Code
    date: '2026-09-12T15:41:35.861Z'
    enginePid: 9336
    outcome: Claude Code paused — waiting for user input.
    originalProgressCount: 2
    finalMessage: >-
      There's already an active replanning session running on RUNE-1 (started
      2026-09-12 15:41, still in progress) — it picked up right after you
      confirmed **gold-with-tabs is the succession rune** and is now rewriting
      the cell model to handle the fixture findings: variable icon counts (2–6,
      not 5), the wide-row text-wrap issue, and the narrower gold-vs-tan-brown
      color gap.


      Nothing new has landed yet from that session. Quick recap of where things
      stand:


      - **Confirmed:** gold border with three top tabs = succession rune (your
      answer).

      - **Still open:** the plan needs a genuine icon-detection step instead of
      "5 equal cells," since row icon count varies and a 6-icon row's text wraps
      onto a second line (icons sit above the text band, not beside it).

      - **Still needed:** real `1 Raw.png` capture-region frames — the four
      screenshots you attached are cropped/zoomed and can't drive the probe
      fixture.


      Is there something specific you wanted to check on, or were you just
      looking for a status update?
  - type: comment
    user: User
    date: '2026-09-12T15:45:20.367Z'
    comment: "is this screenshot good or do i need fullscreen\n\n\U0001F4CE image-5.png"
    id: c-2026-09-12t15-45-20-367z
  - type: comment
    user: User
    date: '2026-09-12T15:47:39.703Z'
    comment: "\U0001F4CE image-6.png"
    id: c-2026-09-12t15-47-39-703z
  - type: activity
    user: Agent
    date: '2026-09-12T15:48:13.850Z'
    comment: >-
      FIXTURE UPDATE — user supplied `image-6.png`: a genuine fullscreen capture
      at native 2560x1440 (confirmed via image metadata, not a crop/scale). Row
      icon counts observed: 5, 5, 4, 4, 3, 3 — all with text beside the icons
      (no wrap in this sample). Panel position in the screenshot is roughly
      consistent with the existing `2560x1440` profile in
      `src/OCR/OcrResolutionProfiles.cs` (X=69,Y=205,W=663,H=715) — visual
      left/right edges line up closely; top/bottom have some slack but are in
      the right neighborhood. This is a usable `1 Raw.png`-equivalent fixture
      for (a2)/(b)/(c1 partial). Still missing: a 5-6 icon row that wraps text
      to a second line (the hard case), and a burst of ≥5 consecutive
      identical-panel captures for the c1 stability check. Earlier `image-5.png`
      was rejected as a fixture candidate — cropped/scaled, did not match any
      resolution profile.
    id: a-2026-09-12t15-48-13-850z
  - type: status_change
    from: Grooming
    to: Todo
    user: Guy
    date: '2026-09-12T15:52:49.491Z'
  - type: activity
    user: Agent
    date: '2026-09-12T15:52:57.404Z'
    comment: >-
      Created worktree for branch
      flux/RUNE-1-extract-and-fingerprint-succession-runes-from-the-discarded-
    event: worktree-created
    id: a-2026-09-12t15-52-57-404z
  - type: status_change
    from: Todo
    to: In Progress
    user: Guy
    date: '2026-09-12T15:52:57.448Z'
  - type: activity
    user: User
    date: '2026-09-12T15:52:57.552Z'
    comment: "\U0001F3AF Launch focus: ## Dynamic Delegation\n\nUse `list_available_agents` to discover specialists, then delegate as needed. Only delegate when specialist knowledge adds clear value over doing it yourself."
    id: a-2026-09-12t15-52-57-552z
  - type: agent_session
    sessionId: c76f5b61-fd19-4218-af67-1a562b8765f1
    startedAt: '2026-09-12T15:52:57.551Z'
    status: waiting-input
    progress:
      - timestamp: '2026-09-12T15:53:13.604Z'
        message: Reading
        type: info
      - timestamp: '2026-09-12T15:53:28.615Z'
        message: Working
        type: info
      - timestamp: '2026-09-12T15:54:13.638Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T15:54:28.641Z'
        message: Reading
        type: info
      - timestamp: '2026-09-12T15:55:28.668Z'
        message: Reading
        type: info
      - timestamp: '2026-09-12T15:55:43.668Z'
        message: Reading
        type: info
      - timestamp: '2026-09-12T15:56:43.712Z'
        message: Reading
        type: info
      - timestamp: '2026-09-12T15:57:43.729Z'
        message: Reading
        type: info
      - timestamp: '2026-09-12T15:57:58.729Z'
        message: Editing
        type: info
      - timestamp: '2026-09-12T15:58:13.729Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T15:58:43.739Z'
        message: Working
        type: info
      - timestamp: '2026-09-12T15:58:55.890Z'
        message: >-
          ⚠️ Tool failed: Read — File does not exist. Note: your current working
          directory is E:\Git\.eh-worktrees\RuneshapePriceChecker-RUNE-1.
      - timestamp: '2026-09-12T15:58:58.739Z'
        message: Reading
        type: info
      - timestamp: '2026-09-12T15:59:13.742Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T15:59:28.751Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T15:59:43.752Z'
        message: Reading
        type: info
      - timestamp: '2026-09-12T15:59:58.765Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T16:00:13.773Z'
        message: Working
        type: info
      - timestamp: '2026-09-12T16:00:28.775Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T16:00:43.782Z'
        message: Working
        type: info
      - timestamp: '2026-09-12T16:00:58.784Z'
        message: Reading
        type: info
      - timestamp: '2026-09-12T16:02:43.826Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T16:02:58.838Z'
        message: Editing
        type: info
      - timestamp: '2026-09-12T16:03:43.862Z'
        message: Editing
        type: info
      - timestamp: '2026-09-12T16:04:43.899Z'
        message: Editing
        type: info
      - timestamp: '2026-09-12T16:05:13.914Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T16:05:28.918Z'
        message: Working
        type: info
      - timestamp: '2026-09-12T16:05:58.941Z'
        message: Reading
        type: info
      - timestamp: '2026-09-12T16:07:28.968Z'
        message: Reading
        type: info
      - timestamp: '2026-09-12T16:07:43.977Z'
        message: Editing
        type: info
      - timestamp: '2026-09-12T16:07:58.991Z'
        message: Editing
        type: info
      - timestamp: '2026-09-12T16:08:29.003Z'
        message: Working
        type: info
      - timestamp: '2026-09-12T16:08:44.005Z'
        message: Reading
        type: info
      - timestamp: '2026-09-12T16:09:14.023Z'
        message: Working
        type: info
      - timestamp: '2026-09-12T16:09:29.034Z'
        message: Reading
        type: info
      - timestamp: '2026-09-12T16:11:29.102Z'
        message: Reading
        type: info
      - timestamp: '2026-09-12T16:11:44.107Z'
        message: Editing
        type: info
      - timestamp: '2026-09-12T16:11:59.106Z'
        message: Reading
        type: info
      - timestamp: '2026-09-12T16:12:14.109Z'
        message: Editing
        type: info
      - timestamp: '2026-09-12T16:12:29.115Z'
        message: Reading
        type: info
      - timestamp: '2026-09-12T16:12:44.121Z'
        message: Reading
        type: info
      - timestamp: '2026-09-12T16:12:59.120Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T16:13:14.129Z'
        message: Working
        type: info
      - timestamp: '2026-09-12T16:13:44.143Z'
        message: Editing
        type: info
      - timestamp: '2026-09-12T16:13:59.150Z'
        message: Working
        type: info
      - timestamp: '2026-09-12T16:15:29.194Z'
        message: Editing
        type: info
      - timestamp: '2026-09-12T16:15:44.204Z'
        message: Editing
        type: info
      - timestamp: '2026-09-12T16:15:59.207Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T16:16:14.214Z'
        message: Working
        type: info
      - timestamp: '2026-09-12T16:16:29.218Z'
        message: Editing
        type: info
      - timestamp: '2026-09-12T16:16:44.219Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T16:16:59.220Z'
        message: Working
        type: info
      - timestamp: '2026-09-12T16:17:29.246Z'
        message: Thinking
        type: info
      - timestamp: '2026-09-12T16:17:44.252Z'
        message: Editing
        type: info
      - timestamp: '2026-09-12T16:17:59.252Z'
        message: Reading
        type: info
      - timestamp: '2026-09-12T16:18:14.263Z'
        message: Reading
        type: info
      - timestamp: '2026-09-12T16:18:29.272Z'
        message: Editing
        type: info
      - timestamp: '2026-09-12T16:18:44.311Z'
        message: Reading
        type: info
      - timestamp: '2026-09-12T16:18:59.314Z'
        message: Editing
        type: info
      - timestamp: '2026-09-12T16:19:14.314Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T16:19:29.323Z'
        message: Working
        type: info
      - timestamp: '2026-09-12T16:20:14.351Z'
        message: Reading
        type: info
      - timestamp: '2026-09-12T16:20:29.366Z'
        message: Editing
        type: info
      - timestamp: '2026-09-12T16:20:44.379Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T16:20:59.386Z'
        message: Working
        type: info
      - timestamp: '2026-09-12T16:21:44.409Z'
        message: Reading
        type: info
      - timestamp: '2026-09-12T16:21:59.440Z'
        message: Editing
        type: info
      - timestamp: '2026-09-12T16:22:14.447Z'
        message: Editing
        type: info
      - timestamp: '2026-09-12T16:22:29.453Z'
        message: Editing
        type: info
      - timestamp: '2026-09-12T16:22:59.474Z'
        message: Editing
        type: info
      - timestamp: '2026-09-12T16:23:29.492Z'
        message: Editing
        type: info
      - timestamp: '2026-09-12T16:23:44.498Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T16:23:59.506Z'
        message: Reading
        type: info
      - timestamp: '2026-09-12T16:24:14.509Z'
        message: Reading
        type: info
      - timestamp: '2026-09-12T16:24:29.514Z'
        message: Editing
        type: info
      - timestamp: '2026-09-12T16:24:44.525Z'
        message: Editing
        type: info
      - timestamp: '2026-09-12T16:24:59.530Z'
        message: Editing
        type: info
      - timestamp: '2026-09-12T16:25:14.545Z'
        message: Editing
        type: info
      - timestamp: '2026-09-12T16:25:29.557Z'
        message: Editing
        type: info
      - timestamp: '2026-09-12T16:25:44.566Z'
        message: Editing
        type: info
      - timestamp: '2026-09-12T16:25:59.575Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T16:26:14.576Z'
        message: Working
        type: info
      - timestamp: '2026-09-12T16:26:29.581Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T16:26:44.588Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T16:26:59.597Z'
        message: Running command
        type: info
      - timestamp: '2026-09-12T16:27:29.616Z'
        message: Working
        type: info
      - timestamp: '2026-09-12T16:28:29.638Z'
        message: Working
        type: info
      - timestamp: '2026-09-12T16:28:59.644Z'
        message: Working
        type: info
      - timestamp: '2026-09-12T16:29:07.706Z'
        message: >-
          I've moved RUNE-1 to Require Input and will stop here per the wait
          instruction. Summary of this session's work:


          **What happened:** The plan I inherited was already known-stale (its
          own body said so), assuming icons and text always share one row band
          via a fixed `textColX` crop. I built a real fixture from a screenshot
          you'd already provided, and discovered two things the plan missed
          entirely: wide rows wrap their icon strip *above* the text line while
          narrow rows keep it *beside* the text on the same line, and icon size
          is actually constant across rows (an earlier scaled-thumbnail read had
          suggested otherwise, wrongly).


          **What's built and committed** (`8ff49bc`): dynamic per-row icon-band
          detection, column-based cell segmentation, a validated
          gold-border-proportion metric for gilded classification, dHash +
          hue-bucket fingerprinting, the reader hook, contracts, debug overlay,
          and 18 passing unit tests — all fail-soft, so imperfect rows just
          under-report rather than emit wrong data.


          **What's unresolved:** cell-boundary precision is inconsistent — the
          gilded rune is cleanly isolated in 2 of 6 test rows, clipped in the
          other 4. That's a segmentation-precision gap, not evidence the metric
          itself is wrong (where boundaries land right, separation is clean:
          0.15–0.27 vs 0.00–0.08).


          I've asked you to decide: keep tuning against this one fixture, or
          wait for more real captures (second resolution, a wrap-case row, a
          burst sequence) first — with a stated default of continuing to tune
          now if you don't have a preference.
    user: Claude Code
    date: '2026-09-12T15:52:57.551Z'
    enginePid: 9336
    groupId: deb52fba-784a-4da0-b436-949366ca7627
    role: dev-lead
    pattern: supervisor
    outcome: Claude Code paused — waiting for user input.
    originalProgressCount: 90
    finalMessage: >-
      I've moved RUNE-1 to Require Input and will stop here per the wait
      instruction. Summary of this session's work:


      **What happened:** The plan I inherited was already known-stale (its own
      body said so), assuming icons and text always share one row band via a
      fixed `textColX` crop. I built a real fixture from a screenshot you'd
      already provided, and discovered two things the plan missed entirely: wide
      rows wrap their icon strip *above* the text line while narrow rows keep it
      *beside* the text on the same line, and icon size is actually constant
      across rows (an earlier scaled-thumbnail read had suggested otherwise,
      wrongly).


      **What's built and committed** (`8ff49bc`): dynamic per-row icon-band
      detection, column-based cell segmentation, a validated
      gold-border-proportion metric for gilded classification, dHash +
      hue-bucket fingerprinting, the reader hook, contracts, debug overlay, and
      18 passing unit tests — all fail-soft, so imperfect rows just under-report
      rather than emit wrong data.


      **What's unresolved:** cell-boundary precision is inconsistent — the
      gilded rune is cleanly isolated in 2 of 6 test rows, clipped in the other
      4. That's a segmentation-precision gap, not evidence the metric itself is
      wrong (where boundaries land right, separation is clean: 0.15–0.27 vs
      0.00–0.08).


      I've asked you to decide: keep tuning against this one fixture, or wait
      for more real captures (second resolution, a wrap-case row, a burst
      sequence) first — with a stated default of continuing to tune now if you
      don't have a preference.
  - type: activity
    user: Agent
    date: '2026-09-12T16:28:23.441Z'
    comment: Updated description.
    id: a-2026-09-12t16-28-23-441z
  - type: comment
    user: Agent
    comment: >-
      SPIKE-GATE EVIDENCE — RUNE-1, commit
      `8ff49bc2453f9ec20c6f6a7df584d9f1d71c0510`.


      **Fixture:** `tests/fixtures/runeicons/2560x1440/1 Raw.png` — real,
      pixel-exact crop of the `OcrResolutionProfiles["2560x1440"]` capture
      region (X=69,Y=205,W=663,H=715) taken from user-supplied `image-6.png`, a
      confirmed native 2560x1440 fullscreen capture. This is the only real
      fixture available; earlier reference screenshots (`image.png`,
      `image-2..5.png`) are cropped/zoomed and confirmed unusable as pipeline
      input.


      **Ground truth for this fixture (visually confirmed):** 6 Combinations
      rows, icon counts 6/6/5/4/4/3, exactly one gold-tabbed succession rune per
      row always at icon position 3 (0-indexed 2), same rune's hash recurring
      since the rows are cumulative builds of one combo.


      **Per-check results:**


      - **(a1)** SUPERSEDED — the "5 equal cells, analytic per-profile width
      check" no longer applies. Cell geometry is now detected dynamically from
      pixels every cycle (variable icon count, 2-6+ observed), not computed from
      a fixed fraction of capture width. There is no meaningful "pure
      arithmetic" check left to run for a dynamic detector.


      - **(a2)** PARTIAL PASS. Measured per-row
      (`RuneIconFingerprinterFixtureTests.Fixture_RowIconBands_YieldExactlyOneGildedCellPerRow`,
      overlay written to `<test output>/rune-probe-out/6
      IconCells-2560x1440.png`):
        - Row 0 (6 icons): cells at X=0/57/109/162/223/277, gold cell X=109 W=53 → goldRing=0.27 ✓ clean isolation.
        - Row 1 (6 icons): gold cell (~X=111) → goldRing=0.09, best non-gold competitor 0.10 → NOT cleanly isolated, near-overlap.
        - Row 2 (5 icons): gold cell (~X=111) → goldRing=0.11, best non-gold competitor 0.08 → narrow separation, inside ambiguous band.
        - Row 3 (4 icons): gold cell (~X=111) → goldRing=0.11, ambiguous band, best non-gold 0.00.
        - Row 4 (4 icons): gold cell X=109 W=54 → goldRing=0.15 ✓ clean isolation (right at threshold).
        - Row 5 (3 icons): gold cell (~X=109) → goldRing=0.14, ambiguous band, best non-gold 0.00.
        - **2 of 6 rows (0, 4) cleanly isolate the gilded cell's true boundary; 4 of 6 (1,2,3,5) have a fused-segment split boundary that clips enough of the gold ring to fall into or below the ambiguous band.** Only 1 profile tested (criterion requires ≥2).

      - **(b)** NOT MET. Across the 6 real gilded samples: range is 0.09-0.27
      (not the required zero-overlap `min(gilded) > max(non-gilded)` — 0.09 and
      0.11 sit inside/below the same range as some non-gilded neighbours at
      0.08-0.10). Sample count (6 gilded, ~30 non-gilded, 1 profile) is far
      short of the ≥30/class × ≥2 profiles bar. **Root cause is segmentation
      precision (the estimated fused-cell split boundary), not the metric
      itself** — in the 2 rows where segmentation is exact, separation is clean
      (0.15-0.27 vs 0.00-0.08, zero overlap).


      - **(c1)/(c2)/(c3)** UNTESTED — no burst-capture sequence (≥5 consecutive
      frames, same unchanged panel) and no second resolution profile fixture
      exist yet.


      **Also discovered and fixed along the way (see commit message for full
      list):** the panel uses TWO row layouts (icons-above-text for wide rows,
      icons-beside-text for narrow rows), not the single "textColX-relative"
      layout the original plan assumed for any row width; icon cell size is
      constant (~46-54px) across all rows contrary to an earlier
      scaled-thumbnail misread.


      **Verdict: spike gate is not closed.** Implementation is fail-soft (an
      unresolved row under-reports, never emits a wrong key) so it is safe to
      have landed, but per the ticket's own governing instruction ("stop if any
      check fails... do not fake the gate"), (b) cannot be signed off yet. Two
      paths forward, routed to the user as a Require Input decision: (1) invest
      more engineering time in cell-boundary refinement against this one fixture
      (risk: overfitting its specific noise), or (2) gather more real fixtures
      first (second profile, wrap-case-only capture, burst sequence) so
      refinement has more than one sample set to validate against.
    date: '2026-09-12T16:28:46.497Z'
    selfAttested: true
    pin: true
    id: c-2026-09-12t16-28-46-497z
  - type: comment
    user: Agent
    comment: >-
      The spike gate is not fully closed — I need a decision on how to proceed,
      not just more solo iteration.


      **What's done and committed (`8ff49bc`):** the icon-strip geometry model
      is rebuilt from scratch against a real 2560x1440 screenshot you provided
      (`image-6.png`). Two things the original plan got wrong are now fixed:
      wide rows wrap their name text *below* the icon strip while narrower rows
      keep it *beside* the icons on the same line (both layouts are handled),
      and icon size turned out to be constant across all rows (an earlier
      reading of a shrunk thumbnail wrongly suggested otherwise). The
      gold-border detection metric is validated as sound — where cell boundaries
      land exactly right, gilded vs. non-gilded separates cleanly (0.15–0.27 vs
      0.00–0.08, zero overlap).


      **What's not resolved:** cell segmentation only lands exactly right in 2
      of the 6 rows on this one fixture. In the other 4, an estimated (not
      measured) split boundary clips part of the gold border, pulling the
      reading down into an ambiguous range that sometimes gets close to
      non-gilded neighbors. This means the ticket's own zero-overlap acceptance
      bar for the border metric isn't met yet — not because the idea is wrong,
      but because cell-boundary precision needs more work.


      **My question:** should I keep refining the boundary-splitting logic
      against this single fixture, or would you rather I wait for more real
      screenshots first (a second resolution/monitor size, a screenshot of a row
      whose icons visibly wrap above the text, and a few back-to-back
      screenshots of the same unchanged panel)? Continuing to tune against just
      one image risks fitting to its specific noise rather than the general case
      — more real samples would make the next round of fixes more likely to
      stick. My default, if you'd rather not decide: keep refining against the
      current fixture, since it's cheap to try and the remaining gap looks like
      a boundary-math issue rather than a missing capability.


      Full numbers and per-row detail are in the pinned "SPIKE-GATE EVIDENCE"
      comment on this ticket.
    date: '2026-09-12T16:28:58.370Z'
    id: c-2026-09-12t16-28-58-370z
  - type: swimlane_change
    swimlane: require-input
    action: set
    user: Agent
    date: '2026-09-12T16:28:58.370Z'
    comment: >-
      The spike gate is not fully closed — I need a decision on how to proceed,
      not just more solo iteration.


      **What's done and committed (`8ff49bc`):** the icon-strip geometry model
      is rebuilt from scratch against a real 2560x1440 screenshot you provided
      (`image-6.png`). Two things the original plan got wrong are now fixed:
      wide rows wrap their name text *below* the icon strip while narrower rows
      keep it *beside* the icons on the same line (both layouts are handled),
      and icon size turned out to be constant across all rows (an earlier
      reading of a shrunk thumbnail wrongly suggested otherwise). The
      gold-border detection metric is validated as sound — where cell boundaries
      land exactly right, gilded vs. non-gilded separates cleanly (0.15–0.27 vs
      0.00–0.08, zero overlap).


      **What's not resolved:** cell segmentation only lands exactly right in 2
      of the 6 rows on this one fixture. In the other 4, an estimated (not
      measured) split boundary clips part of the gold border, pulling the
      reading down into an ambiguous range that sometimes gets close to
      non-gilded neighbors. This means the ticket's own zero-overlap acceptance
      bar for the border metric isn't met yet — not because the idea is wrong,
      but because cell-boundary precision needs more work.


      **My question:** should I keep refining the boundary-splitting logic
      against this single fixture, or would you rather I wait for more real
      screenshots first (a second resolution/monitor size, a screenshot of a row
      whose icons visibly wrap above the text, and a few back-to-back
      screenshots of the same unchanged panel)? Continuing to tune against just
      one image risks fitting to its specific noise rather than the general case
      — more real samples would make the next round of fixes more likely to
      stick. My default, if you'd rather not decide: keep refining against the
      current fixture, since it's cheap to try and the remaining gap looks like
      a boundary-math issue rather than a missing capability.


      Full numbers and per-row detail are in the pinned "SPIKE-GATE EVIDENCE"
      comment on this ticket.
  - type: activity
    user: Agent
    date: '2026-09-13T04:16:21.495Z'
    comment: Updated description.
    id: a-2026-09-13t04-16-21-495z
  - type: comment
    user: Agent
    comment: >-
      **SPIKE-GATE EVIDENCE v2 — RUNE-1, commit
      `14382aec00073fad4fc0e449aa059460c8e7a771`** (supersedes the 8ff49bc
      evidence).


      **Fixture:** `tests/fixtures/runeicons/2560x1440/1 Raw.png` (unchanged).
      Overlay: `<test output>/rune-probe-out/6 IconCells-2560x1440.png`, test
      `RuneIconFingerprinterFixtureTests.Fixture_EveryRow_IsolatesExactlyOneGildedCell_WithZeroRingOverlap`.


      **Ground truth corrections (confirmed at 3–4× zoom):** icon counts are
      **6/6/6/4/4/4**, not 6/6/5/4/4/3. The gilded cell is at slot 3 (index 2)
      in every row. The rows show **four distinct gilded runes**, not one
      recurring rune: row 0 alone, rows 1+3 (ring-with-legs glyph, black ink),
      rows 2+4 (purple spiral), row 5 alone (purple trident). These are now the
      asserted ground truth in the fixture test.


      **Root cause of the 2-of-6 result:** vertical, not horizontal.
      `DetectIconBand` took the first dark region in the window — the separator
      line above the row — and capped it at 1.9× text height from there, so row
      1's band was y 90–138 while its icons span y 109–158. Every cell rectangle
      from row 1 down was 7–20 px too high; the ring's bottom edge sampled glyph
      pixels. Row 4's old 0.15 was the same defect at 7 px.


      **Per-row result now** (X, W of gilded drawn box; lattice slot; ring):


      | Row | Cells | Gilded box | Slot | Gilded ring | Max non-gilded ring |

      |---|---|---|---|---|---|

      | 0 | 6 | X=110 W=53 | (115,1,45,45) | 0.29 | 0.02 |

      | 1 | 6 | X=111 W=52 | (115,109,45,45) | 0.29 | 0.05 |

      | 2 | 6 | X=111 W=52 | (115,217,45,45) | 0.29 | 0.03 |

      | 3 | 4 | X=111 W=52 | (115,325,45,45) | 0.29 | 0.02 |

      | 4 | 4 | X=111 W=51 | (115,388,45,45) | 0.27 | 0.01 |

      | 5 | 4 | X=111 W=52 | (115,451,45,45) | 0.31 | 0.01 |


      **(a2)** PASS on this profile — 6/6 rows, exact counts, gilded slot
      correct. 1 of 2 required profiles.

      **(b)** Separation PASS: gilded 0.26–0.31 vs non-gilded 0.00–0.05, zero
      overlap (n=6 / n=24). The yellow-glyph plain rune (slot 5 in rows 0–2)
      reads 0.02–0.04. **Sample bar NOT MET:** 6 gilded on 1 profile vs
      ≥30/class × ≥2 profiles.

      **(c2)** PASS in-fixture: rows 1/3 hash 2 bits apart, rows 2/4 1 bit
      apart; all distinct-rune pairs ≥22 bits (the test asserts ≤8 / ≥16).
      Hashing the whole gilded sprite had given 11–12 bits for the same rune,
      and a 1 px frame difference 14–23 — fixed by hashing the lattice slot
      interior.

      **(c1)/(c3)** UNTESTED — no burst sequence, no second profile.


      **Residual risks recorded in the body:** dHash loses 15+ bits per 1 px box
      shift (mitigated by lattice slots; RUNE-2 should match with a small
      Hamming tolerance); row 0's icons are clipped by the 2560x1440 capture
      region top; plain-border saturation sits close to the ink threshold.


      Full suite: 822 passed, 0 failed (built with `-p:LangVersion=preview`, a
      pre-existing SDK/`field`-keyword mismatch unrelated to this ticket).
    date: '2026-09-13T04:16:41.707Z'
    selfAttested: true
    summary: >-
      SPIKE-GATE EVIDENCE v2 at commit 14382aec. Root cause of the 2-of-6
      result: the band detector anchored on the separator line above the icons,
      so boxes sat 7-20px too high. New border-line segmentation: 6/6 rows
      correct (counts 6/6/6/4/4/4 — earlier 6/6/5/4/4/3 was a miscount; gilded
      at slot 3 everywhere). Gold ring 0.26-0.31 gilded vs 0.00-0.05 non-gilded,
      zero overlap. Lattice-slot hashing: same rune 1-3 bits apart, distinct 22+
      (fixture shows 4 distinct gilded runes: row 0; rows 1+3; rows 2+4; row 5).
      Open: 1 fixture/1 profile, no burst; (b) sample bar and (c1)/(c3)
      unproven. Full suite 822/822.
    pin: true
    supersedes:
      - c-2026-09-12t16-28-46-497z
    id: c-2026-09-13t04-16-41-707z
  - type: comment
    user: Agent
    comment: >-
      Segmentation is fixed and committed (`14382aec`): on the real 2560x1440
      capture all six rows now isolate the gilded rune, the gold-ring metric
      separates gilded from non-gilded with zero overlap (0.26–0.31 vs
      0.00–0.05), and the same rune hashes 1–3 bits apart across rows while
      distinct runes sit 22+ bits apart. Full suite 822/822.


      What the gate still lacks is evidence, not code: one fixture, one
      resolution, no back-to-back frames. The ticket's own bar asks for ≥30
      samples per class on ≥2 profiles and a ≥5-frame burst.


      **Decision needed — how do you want to close this card?**


      1. **Accept the single-fixture evidence and move RUNE-1 to Ready now**
      (proposed default). RUNE-2 grooming can start immediately; the sample-size
      and burst checks become a follow-up card that runs whenever you next play
      and grab captures. Reasonable if you only play at 2560x1440.

      2. **Hold RUNE-1 in progress until you supply more captures**: a few more
      fullscreen 2560x1440 shots of different Combinations panels, 5+
      back-to-back shots of one unchanged panel, and one shot at any other
      resolution you use. I then run the gate against them before moving to
      Ready.


      If I hear nothing, I'll take option 1.
    date: '2026-09-13T04:16:55.465Z'
    id: c-2026-09-13t04-16-55-465z
  - type: swimlane_change
    swimlane: require-input
    action: set
    user: Agent
    date: '2026-09-13T04:16:55.465Z'
    comment: >-
      Segmentation is fixed and committed (`14382aec`): on the real 2560x1440
      capture all six rows now isolate the gilded rune, the gold-ring metric
      separates gilded from non-gilded with zero overlap (0.26–0.31 vs
      0.00–0.05), and the same rune hashes 1–3 bits apart across rows while
      distinct runes sit 22+ bits apart. Full suite 822/822.


      What the gate still lacks is evidence, not code: one fixture, one
      resolution, no back-to-back frames. The ticket's own bar asks for ≥30
      samples per class on ≥2 profiles and a ≥5-frame burst.


      **Decision needed — how do you want to close this card?**


      1. **Accept the single-fixture evidence and move RUNE-1 to Ready now**
      (proposed default). RUNE-2 grooming can start immediately; the sample-size
      and burst checks become a follow-up card that runs whenever you next play
      and grab captures. Reasonable if you only play at 2560x1440.

      2. **Hold RUNE-1 in progress until you supply more captures**: a few more
      fullscreen 2560x1440 shots of different Combinations panels, 5+
      back-to-back shots of one unchanged panel, and one shot at any other
      resolution you use. I then run the gate against them before moving to
      Ready.


      If I hear nothing, I'll take option 1.
baselineCommit: 79e13186bd635da01a8d14958a13c8c2d8260bd0
tokenMetadata:
  inputTokens: 54679880
  outputTokens: 360232
  costUSD: 26.58001
  costIsEstimated: false
  cacheReadTokens: 53636579
  cacheCreationTokens: 1042755
artifacts:
  latest: 2
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
    - rev: 2
      createdAt: '2026-09-12T15:12:34.230Z'
      bytes: 9605
      title: RUNE-1 plan rev 2 — crop validation gate + row-Y join
      note: >-
        Rev 2. Changes from rev 1: (1) new gate check (a2) validating that the
        derived cells actually land on the rune sprites, ordered before the
        border-metric and hash checks; (2) keys now carry a row-Y anchor instead
        of relying on list position, because ExtractFromRowTexts filters rows
        out of ItemNames; (3) three real fail-soft predicates replace the
        unsatisfiable "cell count != 5"; (4) the key carries its 32x32 sprite
        bytes so RUNE-2 can render newly discovered runes.
needsAction: null
planReviewState: null
planReviewBodyHash: null
swimlane: require-input
order: 0
branch: flux/RUNE-1-extract-and-fingerprint-succession-runes-from-the-discarded-
---
> **TL;DR** — The app already grabs the rune-icon strip on every scan of the Runeshape Combinations panel and throws it away to keep row detection clean. This card picks those pixels back up, works out which runes have the gold "carries forward" border, and turns each one into a stable fingerprint so RUNE-2 can tell you which rows grant runes you aren't already carrying. Implementation is committed (`14382aec`, on top of `8ff49bc`). On the one real fixture the spike gate's segmentation and separation checks now pass cleanly (6/6 rows, zero ring overlap, same rune 1-3 hash bits apart); what remains open is **sample size**, not code — one fixture, one profile, no burst sequence.

**Card A of two.** RUNE-2 consumes this output and carries almost no technical risk. **All the risk lives here.** Carved from SCRATCH-1; the pinned DESIGN RECORD v2 + ADDENDUM there hold the rejected alternatives.

## Spike gate status (2026-09-13, commit `14382aec`)

Full numbers in the pinned **SPIKE-GATE EVIDENCE v2** comment.

- **(a1)** SUPERSEDED — cell geometry is measured from pixels every cycle, so an analytic per-profile width check has nothing to check.
- **(a2)** PASS on 2560x1440: every row's cells land on the icons (counts 6/6/6/4/4/4, gilded cell at slot 3 in all six rows, overlay confirmed by eye). Only 1 of the required 2 profiles.
- **(b)** PASS on 2560x1440 for separation: gold ring 0.26–0.31 gilded vs 0.00–0.05 non-gilded, zero overlap, including the plain-bordered rune whose glyph is itself yellow. **NOT MET for sample size:** 6 gilded / 24 non-gilded samples on 1 profile vs the ≥30/class × ≥2 profiles bar.
- **(c2)** PASS in-fixture: the same rune in two rows hashes 1–3 bits apart, distinct runes 22+ bits apart. **(c1)/(c3)** UNTESTED — no burst of ≥5 identical frames, no second profile.

### Why the first attempt read 2 of 6 rows

The band detector anchored on the dark horizontal separator line above each row's icons, then capped the band at 1.9× text height from that wrong top, so every cell rectangle from row 1 down sat 7–20 px too high and the ring metric sampled glyph pixels. The metric was never the problem.

### Geometry model as built

Every icon cell is a near-square box (45 px at 2560x1440) with thin 2 px border lines, left-packed at a fixed pitch (54 px) with a parchment gap. The gilded cell has a thicker gold frame plus three tabs on top; the first cell may have a thick blue frame. Wide rows (5–6 icons) put the name text on a line below the icons; narrow rows (2–4) put it beside them. Segmentation keys on those border lines:

1. `LocateIconRow` — in a text-anchored zone (3 text heights above the row's text top to 2 below), columns whose contiguous ink run is roughly one icon tall are vertical border lines; their median top/bottom is the icon row. The bottom is then pinned on the cells' dark bevel line, which the saturated shadow under wide rows never matches. Separator lines form no such runs.
2. `SegmentIconCells` — border columns (≥85% inked over the row height) are paired left-to-right into square cells (0.85–1.35× row height). A gilded cell widens over its contiguous gold frame (2 px gap tolerance) and consumes the frame's stray anti-aliased edge run instead of letting it seed a phantom cell.
3. `AssignLatticeGlyphBounds` — plain cells give the pitch; every cell gets a lattice slot one row-height square. **Identity hashing uses the slot interior (20% inset)**, so the same rune hashes identically wherever it appears regardless of where anti-aliasing lands on the gold frame's edge (a 1 px frame difference had pushed one identical rune pair 14–23 bits apart). The drawn box, gold frame included, still drives the ring metric and the display sprite.

All constants are ratios of per-cycle measured values (text height, icon-row height, capture width) — no new resolution offsets.

## Problem / Motivation

In Path of Exile 2's Runeshape/Expedition remnant panel, gold/gilded-bordered runes are *succession runes*: they carry forward to the next remnant. This card produces a stable per-rune identity for every gilded rune in the Combinations panel so RUNE-2 can surface which rows grant runes the player isn't already carrying. Identification only — no scoring, no overlay, no run state.

The pixels are already in hand: `CaptureAndRecognize` (`src/OCR/OcrLeagueWindowReader.cs`) captures the full panel region; the icon strip is read from `capturedBitmap` (raw frame), never from `preprocessed`.

**No new capture path, no new `OcrResolutionProfiles` entry, no new `LeaguePanelDetector` work, no new NuGet dependency.**

## Decisions that are closed — do not re-open

- **Identity key is `(greyscale dHash of the normalised glyph, dominant hue bucket)`** — shape plus tier colour. **Do not simplify to shape alone.** Both are measured over the glyph interior of the lattice slot (`GlyphInsetRatio = 0.2`), not the whole cell: hashing the whole gilded sprite put the same rune 11–12 bits apart; the interior puts it 1–3 apart. A near-black glyph votes hue only through anti-aliased edges and lands in bucket 1 (the parchment's warm hue) — consistent, but a property of the parchment, not the rune.
- **Gilded classification uses the gold hue+saturation border-ring proportion** (`GoldHueRingProportion`, `GoldRingThreshold = 0.15`, ambiguous band down to `0.09`), not shape/tabs. The tabs alternative was tried and discarded.
- **Sample the raw frame, never `preprocessed`.**
- **Normalise to 32×32 before hashing** — correctness, not optimisation; hashes must be resolution-independent.
- **Keys join on row Y, never on list position** — `RuneRowKeys.RowY`, filtered to rows surviving `ExtractFromRowTexts` at the `LeagueWindowSnapshot` construction site.
- **The key carries its normalised 32×32 full-cell sprite** (`RuneKey.Sprite32Rgb`) so RUNE-2 can render a newly discovered rune.

## Acceptance criteria

**Spike gate** — status as of `14382aec`:

- [x] **(a1)** SUPERSEDED (see above).
- [~] **(a2)** PASS on 1 profile; criterion asks for ≥2.
- [~] **(b)** Zero overlap demonstrated on 1 profile (0.26–0.31 vs 0.00–0.05); sample count 6/24 on 1 profile vs ≥30/class × ≥2 profiles.
- [~] **(c2)** Same rune 1–3 bits, distinct runes 22+ bits, in-fixture. **(c1)/(c3)** untested.
- [x] Pass/fail per check, measured values, and fixture filenames recorded (pinned evidence comment; `RuneIconFingerprinterFixtureTests` asserts the ground truth and regression margins).

**Implementation** (done; fail-soft holds regardless of the gate):

- [x] Cells are sliced from `capturedBitmap`; `preprocessed` is never sampled for colour; gilded test happens before normalisation/hashing.
- [x] `RuneRows` reaches the `LeagueWindowSnapshot` aligned 1:1 with `ItemNames` via the row-Y join.
- [x] Skip predicates (unusable text height, zone under `MinCellPx`, no border-line runs, no border pairs) log once and yield no keys — never throws.
- [x] Ambiguous-band cells (`0.09`–`0.15`) are dropped as non-gilded with a debug log entry.
- [x] `6 IconCells.png` is written only when `SaveDebugImages` is on; it draws exactly what the extractor saw (`DetectCells` is shared).

## Implementation plan (as built)

**Landed in:** `src/OCR/RuneIconFingerprinter.cs`, `src/OCR/OcrLeagueWindowReader.cs` (hook + debug overlay), `src/Contracts/RuneKey.cs`, `src/Contracts/LeagueWindowSnapshot.cs`.

1. **Fixture harness** — `tests/src/OCR/RuneIconFingerprinterFixtureTests.cs` loads `tests/fixtures/runeicons/<resolution>/1 Raw.png`, gets text rows through the reader's own pipeline, runs `DetectCells`/`ExtractRowKeys`, writes the overlay, and asserts per-fixture ground truth: icon counts, gilded slot, ring margins, same-rune ≤8 bits, distinct-rune ≥16 bits. Skips when the fixture is absent.
2. **`LocateIconRow`**, 3. **`SegmentIconCells`**, 4. **`AssignLatticeGlyphBounds`** — see the geometry model above.
5. **Gilded classification** — `GoldHueRingProportion` over the drawn box's outer 12% ring.
6. **Fingerprinting** — `NormalizeTo32x32` of the lattice-slot interior → `ComputeDHash` + `DominantGlyphHueBucket`; full-cell sprite carried separately.
7. **Reader hook** — `ComputeRuneRowKeys` in `CaptureAndRecognize` right after `DetectRowPositions`; rows filtered to those surviving `ExtractFromRowTexts`.
8. **Tests** — 21 unit tests (HSV, ink, dHash identity/distinctness/noise, hue bucket, ring metric, fail-soft, synthetic lattice segmentation, run merging) plus the fixture test. Full suite 822/822 green.

## Risks / caveats

- **One fixture, one profile.** The other 4 `OcrResolutionProfiles` entries are unvalidated against real pixels; `Interpolate(width, height)` resolutions likewise.
- **Hash sensitivity to box jitter.** A 1 px shift of the hashed box costs 15+ bits with dHash on a 30 px glyph. The lattice slot makes boxes integer-exact for identical pixels, so this only bites if the panel ever renders the same rune at sub-pixel-different positions. RUNE-2 should match keys with a small Hamming tolerance (≤8) rather than exact equality.
- **First-row clipping.** The 2560x1440 capture region (Y=205) cuts the top of row 0's icons, so row 0's gilded slot starts at the frame edge. Same-rune identity across row 0 and other rows is unverified (the fixture's row 0 rune is unique). If it proves unstable, nudging the profile's Y up ~10 px is the fix — a profile edit, not a new entry.
- **Plain-border saturation is marginal.** The thin orange-brown border reads at saturation 0.32–0.45 against the ink threshold 0.28, with shadowed parchment reaching 0.39. Coverage-based border detection tolerates this, but a darker or differently lit panel could shift it.

## Out of scope

- Scoring, weighting, catalog, overlay — RUNE-2.
- Run-boundary detection — later card.
- Overlay on the in-world remnant socket bar — deferred.
