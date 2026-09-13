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
  - type: comment
    user: Agent
    comment: >-
      User-supplied source (2026-09-13):
      https://poe2db.tw/Runeshape_Combinations. What it adds over the Game8
      list:

      - A canonical icon per rune at
      `https://cdn.poe2db.tw/image/Art/2DArt/UIImages/InGame/Expedition/Remnant/RemnantRune<Name>.webp`
      — usable as a reference glyph beside each library entry so binding a seen
      sprite is a visual match; possibly usable for automatic binding by hashing
      (optional, see plan).

      - A rarity marker: 11 runes carry `RemnantRareRune` (Sky, Earth, Life,
      Bond, Ward, Soul, Death, Oath, Time, Power, Bait); the other 23 are
      common. This is the game's own rarity, distinct from glyph colour (Ward is
      blue yet rare; Opulent is gold yet common) — record both, weight by user
      preference.

      - A 34th rune, **Bait**, absent from Game8's list.

      - 322 combination recipes with Divine/Exalted valuations — recipe value,
      not rune value; not used here.

      No text about the succession/carry mechanic on that page.
    date: '2026-09-13T04:37:46.628Z'
    selfAttested: true
    id: c-2026-09-13t04-37-46-628z
  - type: activity
    user: Agent
    date: '2026-09-13T04:39:17.021Z'
    comment: Updated description.
    id: a-2026-09-13t04-39-17-021z
  - type: activity
    user: Agent
    comment: >-
      Published artifact revision 1 — RUNE-2 plan mockup — overlay options, Rune
      Library, data flow (37,852 bytes).
    date: '2026-09-13T04:52:11.009Z'
    id: a-2026-09-13t04-52-11-009z
  - type: activity
    user: Plan Gate
    comment: >-
      Plan-review gate on — looping review → revise until approved (approval
      flags you to confirm), or parking after 2 revise attempt(s).
    date: '2026-09-13T04:52:23.001Z'
    id: a-2026-09-13t04-52-23-001z
  - type: activity
    user: Furnace
    date: '2026-09-13T04:52:23.020Z'
    comment: "\U0001F3AF Launch focus: You are reviewing a TICKET PLAN, not committed code — this ticket is still in Grooming and has no diff. Read its full description (title, body, `## Acceptance criteria`) and its latest published artifact (if any) as the plan under review. Depth: thorough. Full method for each check below: `read_skill('orchestrator', 'Plan-review methodology')`. Anchor check: verify every cited file/symbol/line still exists and means what the plan says — re-derive fresh every pass, never trust a prior citation. Artifact check (FLUX-1313): a plan artifact revision has already been published for this ticket (confirmed deterministically by the pre-gate lint) — no gap here regardless of how UI/UX-shaped the plan reads. Reground (FLUX-1048): check `.docs/release-notes/INDEX.md` + sibling/recently-Done tickets for work that already landed part of this plan. Acceptance-criteria coverage: confirm the AC checklist is testable and every item is addressed by the plan. Consequence tracing: for every destination this plan moves content/config into, name who consumes it and confirm the move still serves the plan's goal. Duplicate check: search open/groomed tickets for one that already covers this same scope. Adversarial self-review: read the plan as its harshest critic — flag weak/missing/wrong steps, unstated hard-to-reverse decisions, and judgment calls the plan ducked. Record your verdict via `change_status` — leave `newStatus` as \"Grooming\" (do NOT move the ticket) and set `planReviewState` to \"approved\" or \"changes-requested\" (never `reviewState`; that is a different field for the post-Todo code-review gate). Posting a comment that starts with **APPROVED** or **CHANGES NEEDED** is not enough by itself — without the `change_status` call the ticket will be parked for a human over an unrecorded verdict."
    summary: >-
      Launch focus recorded (1804 chars) — methodology detail via
      read_skill('orchestrator', 'Plan-review methodology'). Full text
      recoverable via expand.
    id: a-2026-09-13t04-52-23-020z
  - type: comment
    user: Agent
    comment: >-
      Groomed 2026-09-13. Plan rewritten against the code as it is (survey
      findings folded in: no OS config dir, Dashboard is a separate assembly, no
      hotkey infra, render-skip hashes ignore RuneRows). Mockup published (rev
      1) with two decision cards — overlay segment style (default A names) and
      how runes get marked carried (default dashboard toggle). Catalog is the
      fixed 34-rune list with reference glyphs from poe2db; user weights Opulent
      3 / Power 2 / purple 1 / blue 0.5. Effort raised to L. Open non-blocking
      question for the user: ship the reference glyphs (game artwork) or not —
      default ship as small thumbnails.
    date: '2026-09-13T04:52:23.046Z'
    id: c-2026-09-13t04-52-23-046z
  - type: agent_session
    sessionId: 43ea1e89-2d57-4c63-9936-b9d1681d7465
    startedAt: '2026-09-13T04:52:23.019Z'
    status: completed
    progress:
      - timestamp: '2026-09-13T04:52:38.735Z'
        message: Running command
        type: info
      - timestamp: '2026-09-13T04:52:53.742Z'
        message: Running command
        type: info
      - timestamp: '2026-09-13T04:53:08.748Z'
        message: Running command
        type: info
      - timestamp: '2026-09-13T04:53:23.763Z'
        message: Running command
        type: info
      - timestamp: '2026-09-13T04:53:53.765Z'
        message: Running command
        type: info
      - timestamp: '2026-09-13T04:54:08.772Z'
        message: Running command
        type: info
      - timestamp: '2026-09-13T04:54:23.780Z'
        message: Running command
        type: info
      - timestamp: '2026-09-13T04:54:38.792Z'
        message: Running command
        type: info
      - timestamp: '2026-09-13T04:55:53.828Z'
        message: Running command
        type: info
      - timestamp: '2026-09-13T04:56:08.838Z'
        message: Running command
        type: info
      - timestamp: '2026-09-13T04:56:21.926Z'
        message: |-
          ⚠️ Tool failed: Bash — Exit code 1
          Traceback (most recent call last):
            File "<string>", line 5, in <module>
              print(repr(s[max(0,i-400):i+400]))
              ~~~~~^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            File "C:\Users\Guy-Desktop\App
      - timestamp: '2026-09-13T04:56:23.847Z'
        message: Running command
        type: info
      - timestamp: '2026-09-13T04:57:08.870Z'
        message: Running command
        type: info
      - timestamp: '2026-09-13T04:57:23.870Z'
        message: Working
        type: info
      - timestamp: '2026-09-13T04:58:29.276Z'
        message: >-
          **CHANGES NEEDED** — verdict recorded on RUNE-2 (`planReviewState:
          changes-requested`, ticket left in Grooming).


          The plan is genuinely well-anchored — I re-derived every cited
          file/symbol and most check out exactly as described (the two
          render-skip hashes really do ignore `RuneRows`, `PriceRowLayout`
          really is test-only, the Dashboard really can't see `Contracts`, and a
          new `"Runes"` settings section really does survive the dashboard's
          save path, which I traced through `DashboardViewModel.SaveSettings`).
          Two things will produce wrong behaviour as written:


          **Blockers**

          1. **RUNE-1 isn't `Ready`.** It's `In Progress`, parked after its
          review session died with `auth-expired`, never reviewed. Its code is
          on `flux/RUNE-1-…` (2 ahead of master, 1 behind). RUNE-2's stated
          baseline `4930b67` has no `RuneKey` or `RuneRows` — step 1 wouldn't
          compile. Needs an explicit sequencing decision.

          2. **The tier-by-glyph-colour rule scores black-ink runes at weight
          3**, tying Opulent. `DominantGlyphHueBucket` skips unsaturated pixels,
          so a dark glyph votes only through warm anti-aliased parchment edges →
          bucket 1, which the plan maps to "gold". RUNE-1's own fixture contains
          a black-ink rune, so this misfires on the only real sample that
          exists. Bucket 0 (no qualifying pixels) isn't mapped at all.


          **Majors:** the match predicate is tolerant on shape but *exact* on
          hue — the less stable half for dark glyphs — while `Observe` runs
          every scan cycle (100 ms default) with no cap, so the user's catalog
          file can grow unbounded (burst stability is still unproven, open as
          RUNE-3); rows with no price quote are skipped entirely by
          `BuildEntries`, so the rune segment and best-row marker can silently
          vanish; and ~20 of the 34 runes have no shipped weight defined
          anywhere, with the body and the artifact disagreeing on Death/Rebirth.


          Four minors folded into the comment rather than spun into tickets —
          notably that shipping 34 poe2db game icons inside the binary is a
          licensing call the plan defaults...
    user: Claude Code
    date: '2026-09-13T04:52:23.019Z'
    enginePid: 21456
    outcome: Claude Code session ended with code 0.
    endedAt: '2026-09-13T04:58:29.276Z'
    originalProgressCount: 15
    finalMessage: >-
      **CHANGES NEEDED** — verdict recorded on RUNE-2 (`planReviewState:
      changes-requested`, ticket left in Grooming).


      The plan is genuinely well-anchored — I re-derived every cited file/symbol
      and most check out exactly as described (the two render-skip hashes really
      do ignore `RuneRows`, `PriceRowLayout` really is test-only, the Dashboard
      really can't see `Contracts`, and a new `"Runes"` settings section really
      does survive the dashboard's save path, which I traced through
      `DashboardViewModel.SaveSettings`). Two things will produce wrong
      behaviour as written:


      **Blockers**

      1. **RUNE-1 isn't `Ready`.** It's `In Progress`, parked after its review
      session died with `auth-expired`, never reviewed. Its code is on
      `flux/RUNE-1-…` (2 ahead of master, 1 behind). RUNE-2's stated baseline
      `4930b67` has no `RuneKey` or `RuneRows` — step 1 wouldn't compile. Needs
      an explicit sequencing decision.

      2. **The tier-by-glyph-colour rule scores black-ink runes at weight 3**,
      tying Opulent. `DominantGlyphHueBucket` skips unsaturated pixels, so a
      dark glyph votes only through warm anti-aliased parchment edges → bucket
      1, which the plan maps to "gold". RUNE-1's own fixture contains a
      black-ink rune, so this misfires on the only real sample that exists.
      Bucket 0 (no qualifying pixels) isn't mapped at all.


      **Majors:** the match predicate is tolerant on shape but *exact* on hue —
      the less stable half for dark glyphs — while `Observe` runs every scan
      cycle (100 ms default) with no cap, so the user's catalog file can grow
      unbounded (burst stability is still unproven, open as RUNE-3); rows with
      no price quote are skipped entirely by `BuildEntries`, so the rune segment
      and best-row marker can silently vanish; and ~20 of the 34 runes have no
      shipped weight defined anywhere, with the body and the artifact
      disagreeing on Death/Rebirth.


      Four minors folded into the comment rather than spun into tickets —
      notably that shipping 34 poe2db game icons inside the binary is a
      licensing call the plan defaults...
  - type: comment
    user: Agent
    comment: >-
      **CHANGES NEEDED** — the plan is well-anchored and mostly implementable,
      but two things will produce wrong results as written: it claims a
      dependency that hasn't landed, and its **tier-by-glyph-colour rule scores
      black-ink runes as top-tier** because bucket 1 is also the parchment
      fallback.


      Most anchors verified clean (list at the end) — the revision is targeted,
      not a rewrite.


      ---


      ## Blockers


      **B1 — RUNE-1 is not `Ready`, and its code is not on this ticket's
      baseline.**

      The body says "Pure consumer of **RUNE-1** (Ready)". Actual board state:
      **RUNE-1 is `In Progress`**, swimlane `require-input`, parked by the
      Furnace on 2026-09-13 after its review session died (`auth-expired` /
      SIGTERM) — it was never reviewed. Its code lives on
      `flux/RUNE-1-extract-and-fingerprint-succession-runes-from-the-discarded-`
      (`14382ae`, pushed; **2 ahead of / 1 behind `master`**). RUNE-2's
      `baselineCommit` is `4930b67` on `master`, where
      `src/Contracts/RuneKey.cs`, `LeagueWindowSnapshot.RuneRows`, and
      `src/OCR/RuneIconFingerprinter.cs` **do not exist** — an implementer
      starting from the stated baseline cannot compile step 1.


      Fix: state the dependency explicitly in the body — RUNE-2 must either
      branch from `flux/RUNE-1-…` or wait for it to merge to `master`, and note
      RUNE-1's branch needs a rebase onto `4930b67` first. This is a sequencing
      decision worth surfacing to the user, not a default.


      **B2 — `HueBucket`-as-tier gives a black-ink rune the maximum weight (3),
      tying Opulent.**

      The plan locks in: "an unbound key… scores by glyph colour via `HueBucket`
      (gold 3, purple 1, blue 0.5)", and AC4 depends on it. The bucket
      arithmetic is right (12 × 30° buckets → 1 = 30–60° warm/gold, 7 = 210–240°
      blue, 9 = 270–300° purple), but
      `RuneIconFingerprinter.DominantGlyphHueBucket`
      (`src/OCR/RuneIconFingerprinter.cs:746`, RUNE-1 branch) **skips pixels
      with `s < 0.15 || v < 0.05`** — near-black ink carries no usable hue.
      RUNE-1's own doc comment on that method says a near-black glyph "votes
      only through its anti-aliased edges, which carry the parchment's own warm
      hue (**bucket 1**) — consistent, since the parchment colour is fixed,
      **but not a property of the rune itself**." If no pixel qualifies at all,
      the argmax loop returns **bucket 0**, which the plan's mapping does not
      cover.


      This is not hypothetical: RUNE-1's pinned SPIKE-GATE EVIDENCE v2
      identifies the fixture's rows 1+3 rune as a "ring-with-legs glyph, **black
      ink**". On the only real sample that exists, an unbound rune would score 3
      — the Opulent weight — and could win "best row" outright. That is the
      ticket's core output being wrong.


      Fix options (pick one and record the rationale): (a) treat bucket 0
      **and** bucket 1 as "unknown, not gold" and fall back to a neutral
      `UnknownRuneWeight`, reserving gold=3 for a hue that can only come from
      actual gold glyph pixels; (b) drop the colour-tier default for unbound
      runes entirely and use one flat unknown weight until the user binds it;
      (c) derive tier from the gilded cell's gold frame/ink saturation rather
      than the glyph hue. Option (a) or (b) keeps the "never a silent 0" rule
      intact.


      ---


      ## Major


      **M1 — Exact `HueBucket` equality in the match predicate + `Observe` on
      every scan cycle can grow the user file without bound.**

      "Key matching is tolerant: same `HueBucket` and Hamming(`ShapeHash`) ≤ 8"
      is tolerant on shape but **exact on hue** — and hue is the *less* stable
      component for dark glyphs (see B2: the bucket is decided by a handful of
      anti-aliased edge pixels, so it can flip 0↔1↔2 between frames). RUNE-1's
      gate check **(c1) burst stability across ≥5 identical frames is UNTESTED**
      and is open as RUNE-3. Meanwhile step 3 calls `RuneCatalog.Observe` from
      `LeaguePricingWorker.ExecuteAsync` for every key every cycle, and
      `OCR.ScanIntervalMs` defaults to **100 ms**
      (`src/Startup/AppSettingsBootstrapper.cs`). Every unmatched key adds a
      persisted binding carrying a base64 32×32 sprite (~4 KB) to
      `config/rune-catalog.json`. There is no cap, no "seen N times before
      persisting", and no statement that `Observe` sits *after* the
      snapshot-hash early-`continue` at
      `src/App/LeaguePricingWorker.cs:230-237`.


      Fix: place `Observe` after the snapshot-changed gate; allow a hue-bucket
      mismatch when shape Hamming is very low (or match on shape alone and store
      hue as advisory); cap unbound bindings and require ≥2 sightings before
      persisting one.


      **M2 — Rows with no price quote render nothing, so the rune segment and
      the best-row marker silently vanish.**

      `PricingOverlayRenderer.BuildEntries`
      (`src/Overlay/ConsoleOverlayRenderer.cs:180`) does `if (quote is null) {
      continue; }` — no overlay row is emitted at all. Step 6 adds the rune
      segment *inside* `BuildTextSegments`, which is only reached for priced
      rows. AC4 ("Rows render a rune segment naming new runes; best row(s)
      visually distinct") therefore fails for any Combinations row whose item
      has no quote — an unpriceable item, a cache miss, or an OCR miss — and the
      highest-scoring row can be the one that disappears.


      Fix: decide and record whether a row with gilded runes but no quote still
      emits an entry (rune segment only), and say so in the body + AC.


      **M3 — Seed weights are undefined for ~20 of the 34 runes, and the body
      and the artifact disagree.**

      The body seeds "Opulent 3, Power 2, other purple/'high value' 1, blue 0.5"
      with tier known only for 9 runes (gold: Opulent; purple: Oath, Time, Bond;
      blue: Ward, Rage, Volcanic, Vision, Stone). Artifact rev 1 instead says
      "Opulent 3 · Power 2 · **Bond/Time/Death/Rebirth 1**" — assigning Death
      and Rebirth a weight the body leaves tier-unknown. Neither names a weight
      for the remaining ~20 (Adaptive, Arcane, Bloodletting, Celestial, Cold,
      Cyclonic, Earth, Electrocuting, Fire, Life, Lightning, Momentum, Moon,
      Prismatic, Protective, Sky, Soul, Tempest, Tidal, Toxic, Wisdom).
      `UnknownRuneWeight` is referenced in the pinned catalog-seed comment but
      appears nowhere in the body's `RunesOptions` defaults (`ResetHotkey`,
      `TierWeights`, `OverlayStyle`). AC1 ("shipped weights Opulent 3, Power 2,
      purple 1, blue 0.5") is untestable for most of the shipped file.


      Fix: give every one of the 34 entries an explicit shipped weight (a single
      default for the untiered majority is fine), define `UnknownRuneWeight` in
      `RunesOptions`, and reconcile the artifact with the body.


      ---


      ## Minor (fold into the revision, no separate tickets)


      - **Renderer dependencies unstated.** Extending `BuildContentHash`
      (`src/Overlay/ConsoleOverlayRenderer.cs:144`) with "a catalog/carried
      version" requires `PricingOverlayRenderer` to take
      `RuneCatalog`/`CarriedRuneSet` as new constructor deps — say so in step
      6/8.

      - **Join rule unstated.** The plan never says how `snapshot.RuneRows`
      joins to overlay rows. It is safe by index:
      `src/OCR/OcrLeagueWindowReader.cs:391-396` filters rune rows by
      `matchedYSet` preserving order, so `RuneRows[i]` ↔ `ItemNames[i]` 1:1.
      Record that (and that `RuneRows` is `null`, not empty, when no keys are
      found).

      - **Game-art redistribution is defaulted, not asked.** Shipping 34
      poe2db/GGG rune icons as embedded resources inside a distributed binary is
      a licensing call; the plan lists it as "non-blocking, default ship".
      Recommend making it an explicit user question. The plan also never says
      *how* the `.webp` files become PNGs under `ocr/rune-icons/` — one-time
      manual download committed to the repo, or a build step?

      - **Hotkey thread ownership unstated.** No `RegisterHotKey` exists
      anywhere in `src/` (plan's claim verified). A message-only `NativeWindow`
      must be created on a thread with a running WinForms message pump; name
      which one (`OverlayFormRunner`'s?). If scope needs trimming, the hotkey
      half of step 4 is cleanly separable from the core value.

      - **Manual carried-toggle is the weakest link.** The whole feature's
      usefulness depends on the user diligently toggling 34 rows each run.
      RUNE-3's comment notes the in-world socket bar showing the *inherited*
      rune is already visible in the captures being collected. Deferral is
      reasonable, but worth stating as the known adoption risk.


      ---


      ## Verified clean (anchor + consequence checks, re-derived this pass)


      - `ComputeSnapshotHash` (`src/App/LeaguePricingWorker.cs:409`) and
      `BuildContentHash` (`src/Overlay/ConsoleOverlayRenderer.cs:144`) both
      **do** ignore `RuneRows` today — the plan's "rune-only changes would never
      re-render" is correct.

      - `PriceRowLayout` is genuinely test-only (sole consumer
      `tests/src/Overlay/PriceRowLayoutTests.cs`); `PricingOverlayRenderer` is
      the live renderer. Plan targets the right one.

      - `ItemNameParser.LoadBaseTypeKeywords`
      (`src/Pricing/ItemNameParser.cs:70`) does resource-name `EndsWith` +
      dev-time disk fallback; `ocr/unique-category-map.json` is an
      `EmbeddedResource` (`RuneshapePriceChecker.csproj:36`). The
      catalog-loading pattern is a valid model.

      - `AppSettingsBootstrapper.DeepMergeDefaults`
      (`src/Startup/AppSettingsBootstrapper.cs:115`) exists and back-fills.
      **Consequence trace on the new `"Runes"` section: it survives.** Both
      writers patch the `JsonNode` tree rather than reserializing a typed model
      — `DashboardViewModel.SaveSettings`
      (`src/Dashboard/DashboardViewModel.cs:175`, `rootObj["App"] ??= …`) and
      `DashboardService.ResetInitialSetupComplete`
      (`src/App/Dashboard/DashboardService.cs:238`). Unknown sections are
      preserved.

      - `config/` beside the exe is the real convention
      (`SettingsController.cs:93`, `BugReportService.cs:157`) — no OS config
      dir, as the plan says.

      - Dashboard is its own assembly with **no** `ProjectReference` back to the
      app (`src/Dashboard/Dashboard.csproj`; the app references it at
      `RuneshapePriceChecker.csproj:87`), so the plan is right that view types
      must be defined in the Dashboard assembly.

      - All cited UI anchors exist: `SectionHeader` style
      (`DashboardWindow.xaml:462`), `LogList` `ItemsControl` (`:844`),
      `ScanIntervalBox` (`:1606`), `QueueAutoSave`
      (`DashboardWindow.xaml.cs:2069`), `SetStatus` / `SetReRunSetupTrigger`
      (`DashboardService.cs:131`/`190`).

      - Options pattern confirmed at `src/Program.cs:208-234` (`Configure<>` /
      `AddOptions<>().Bind`, consumers take `IOptionsMonitor<>`). Note the file
      is `src/Program.cs`, not repo-root `Program.cs`.

      - RUNE-1's API matches what the plan consumes: `RuneKey(ulong ShapeHash,
      int HueBucket, byte[] Sprite32Rgb)` and `RuneRowKeys(int RowY,
      IReadOnlyList<RuneKey> Keys)` (`src/Contracts/RuneKey.cs`, RUNE-1 branch).
      Hamming ≤ 8 is consistent with RUNE-1's measured 1–3 same-rune / 22+
      distinct.

      - **Reground:** no `.docs/release-notes/` directory exists; nothing from
      this plan has already landed. **Duplicate check:** none — RUNE-3 is
      fixtures/sample-bar only, SCRATCH-1 is the source scratch card.

      - **AC coverage:** all 7 criteria map to plan steps and are testable,
      except AC1 (blocked by M3) and AC4 (blocked by B2 + M2).
    date: '2026-09-13T04:58:15.562Z'
    id: c-2026-09-13t04-58-15-562z
  - type: activity
    user: Furnace
    date: '2026-09-13T04:58:34.258Z'
    comment: "\U0001F3AF Launch focus: A plan-review pass just requested changes on this ticket's plan (see the latest review comment in its history) — revise the ticket body via `update_ticket` to address every point raised, then STOP. Do not call `change_status` yourself and do not start implementing; the plan-review gate automatically re-reviews your revision. Write the revision as if the plan had been right the first time — ticket history already records what changed; never annotate the body with what a prior draft got wrong or which review round/annotation resolved a point. When revising an artifact: revise minimally — answer every annotation explicitly, show the annotated element before→after, and never silently redesign elements the user already approved."
    id: a-2026-09-13t04-58-34-258z
  - type: agent_session
    sessionId: e207c8b2-ffe3-4b22-802f-c09e437657d2
    startedAt: '2026-09-13T04:58:34.258Z'
    status: active
    progress: []
    user: Claude Code
    date: '2026-09-13T04:58:34.258Z'
    enginePid: 21456
artifacts:
  latest: 1
  revisions:
    - rev: 1
      createdAt: '2026-09-13T04:52:11.003Z'
      bytes: 37852
      title: 'RUNE-2 plan mockup — overlay options, Rune Library, data flow'
      note: >-
        First revision. Section 1: three overlay styles side by side on the real
        2560x1440 panel rows with RUNE-1's actual sprites (decision card 1,
        default A names). Section 2: the Rune Library dashboard section — fixed
        list of 34 runes with tier colour and weight, unbound-sprite strip for
        binding (decision card 2, default dashboard toggle). Section 3: where
        the pieces live. Rune names on sprites are placeholders until bound.
planGateRunning: true
planGateAttempts: 1
planGateMode: loop-confirm
baselineCommit: 4930b6708b97d44d404a035dad8fd0a19ff6085e
planReviewState: changes-requested
planReviewBodyHash: 1fccfdh
tokenMetadata:
  inputTokens: 2278027
  outputTokens: 26777
  costUSD: 3.019356
  costIsEstimated: false
  cacheReadTokens: 2150931
  cacheCreationTokens: 127050
needsAction: null
---
> **TL;DR** — RUNE-1 now tells us, per Combinations row, which gilded (carry-forward) runes it grants, as stable keys with sprites. This card turns that into the thing the player actually wants: a **rune library** in the dashboard listing all 34 runes with the game's reference glyph, tier colour and weight (Opulent > Power > the rest), where each sprite the tool sees gets bound to its rune once; a **carried-this-run set** with a reset; and an **overlay line per row** saying which new runes that row grants and which row is the best pick.

**Card B of two.** Pure consumer of **RUNE-1** (Ready). Rationale and rejected alternatives: SCRATCH-1's pinned DESIGN RECORD v2 + ADDENDUM. User weight input, the rune alphabet and the poe2db source are the pinned/recent comments on this ticket.

## Problem / Motivation

Succession runes carry forward along a remnant chain; picking one already carried wastes the slot. The player eyeballs this today. With per-row rune keys in hand, the tool can rank rows and name the runes — but only if the opaque keys get bound to the known rune names and the player has a place to prioritise them ("a library I can prioritize against, possibly a nice UI in the app").

## Locked decisions (do not re-open)

- **Score:** `rowScore = Σ weight(rune)` over the row's gilded runes **not** in the carried set. Highest wins.
- **The catalog is the fixed list of 34 runeshapes** (user: "finite number of runes, we just need to list them out"): 33 from Game8 plus Bait (poe2db). Shipped with name, effect text, game rarity (`RemnantRareRune`: Sky, Earth, Life, Bond, Ward, Soul, Death, Oath, Time, Power, Bait), glyph-colour tier where known (gold: Opulent; purple: Oath, Time, Bond; blue: Ward, Rage, Volcanic, Vision, Stone) and weight. Seed weights: Opulent 3, Power 2, other purple/"high value" 1, blue 0.5; user-editable. No open-ended "new rune" concept.
- **Reference glyphs ship with the catalog:** one thumbnail per rune from the poe2db icon set (`https://cdn.poe2db.tw/image/Art/2DArt/UIImages/InGame/Expedition/Remnant/RemnantRune<Name>.webp`, `RemnantRareRune<Name>` for the rare ones; Volcanic is `…Gasp`, Rage is `…Enrage`, Bait has no distinct icon), converted to small PNGs under `ocr/rune-icons/` as embedded resources. They are for display beside each library entry so binding is a visual match. Game artwork — see open question on asset use.
- **Sprites are bound, not discovered.** A gilded key with no binding shows as an **unbound sprite** in the library until the user picks which rune it is; meanwhile it scores by glyph colour via `HueBucket` (gold 3, purple 1, blue 0.5) and the overlay marks it `?`. Never a silent 0.
- **Key matching is tolerant:** same `HueBucket` and Hamming(`ShapeHash`) ≤ 8 (RUNE-1: same rune 1–3 bits, distinct 22+).
- **No OS config dir exists in this app** — state lives beside the exe (`AppContext.BaseDirectory/config/appsettings.json`, `…/ocr/`). The catalog's user layer goes in `…/config/rune-catalog.json`.

## Implementation plan

1. **Matcher** — `src/Runes/RuneKeyMatcher.cs`: `IsSame(RuneKey, RuneKey)` per the tolerance; `FindBinding(key, bindings)`. Pure, unit-tested.
2. **Catalog + persistence** — `src/Runes/RuneCatalog.cs`. Shipped `ocr/rune-catalog.json` (`EmbeddedResource`, loaded like `ItemNameParser.LoadBaseTypeKeywords` loads `unique-category-map.json`: resource-name `EndsWith` + dev-time disk fallback): 34 entries `{ id (slug), displayName, effect, rare, tier?, weight, icon }`, icons as sibling embedded PNGs. User layer `config/rune-catalog.json`: `weights` overrides by id, `bindings` (`{ shapeHash, hueBucket, spritePngBase64, runeId?, seenCount, firstSeenUtc, lastSeenUtc }` — `runeId` null while unbound), `carried` id list. Merge on load, user wins by id (same idea as `AppSettingsBootstrapper.DeepMergeDefaults`, one level). Saves debounced via `System.Text.Json`.
3. **Observe** — `RuneCatalog.Observe(RuneKey)`: match an existing binding → bump `seenCount`; else add an unbound binding with the sprite (PNG from the 32×32 RGB24 bytes). Called from `LeaguePricingWorker.ExecuteAsync` right after the snapshot is read, for every key in `snapshot.RuneRows`. *Optional stretch, only if cheap:* suggest a binding by dHash-comparing the glyph interior against each reference icon rendered on parchment; show as a pre-selected suggestion, never auto-commit.
4. **Carried set** — `src/Runes/CarriedRuneSet.cs`: rune ids (an unbound binding is carried by its binding id and migrates on bind), persisted in the user layer so a restart mid-run keeps it. Marking: dashboard toggle per rune (mockup decision 2 offers a per-row hotkey). Reset: dashboard button **and** a global hotkey — new `src/App/GlobalHotkeyService.cs` using `RegisterHotKey` on a message-only `NativeWindow` (no hotkey infrastructure exists; overlay forms are `WS_EX_NOACTIVATE`). `Runes.ResetHotkey`, default `Ctrl+Alt+R`, empty disables.
5. **Scorer** — `src/Runes/RuneRowScorer.cs`: per row → `{ NewRunes, CarriedRunes, UnboundCount, Score }`; `IsBest` for max-score rows when max > 0.
6. **Overlay** — extend `PricingOverlayRenderer.BuildTextSegments` in `src/Overlay/ConsoleOverlayRenderer.cs` (the live renderer; `PriceRowLayout` is test-only — leave it) with one rune segment per row after the price: new-rune names in green (best row) / orange (positive, not best), `↻ <name> carried` grey when nothing new, `? unbound` amber. Dedicated fixed colour rule, **not** `GetPriceColor` (price-scaled auto thresholds). Rows with no gilded rune render no segment. Extend `LeaguePricingWorker.ComputeSnapshotHash` and `PricingOverlayRenderer.BuildContentHash` with rune keys and a catalog/carried version — both ignore `RuneRows` today, so rune-only changes would never re-render.
7. **Dashboard "Rune Library"** — Dashboard is its own assembly (`src/Dashboard/Dashboard.csproj`, referenced *by* the app; cannot see `Contracts`), so define `RuneLibraryEntryView` / `UnboundSpriteView` there. Push via `DashboardService` with `Dispatcher.InvokeAsync` like `SetStatus`; edits return through `Action` callbacks like `SetReRunSetupTrigger`. UI: new `SectionHeader` "Rune Library" in the settings `StackPanel` of `DashboardWindow.xaml`; an unbound-sprites strip (sprite + "bind to" `ComboBox` of the 34 names, each with its reference glyph) above an `ItemsControl` of all 34 runes bound to an `ObservableCollection` (mirror the `LogList`/`LogEntries` pattern). Per rune: reference glyph, name + effect tooltip, rare marker, tier dot, weight box (reuse the `ScanIntervalBox` numeric pattern + `QueueAutoSave`), bound sprite (`BitmapSource.Create(32,32,96,96,PixelFormats.Rgb24,null,bytes,96)`, 2× `NearestNeighbor`) or "not seen yet", seen count, carried toggle. Plus "Reset carried runes" and the hotkey box.
8. **Options + DI** — `src/Configuration/RunesOptions.cs` bound to a new `"Runes"` section in `Program.cs` (`AddOptions<>().Bind`; consumers take `IOptionsMonitor<>`); defaults (`ResetHotkey`, `TierWeights`, `OverlayStyle`) added to the `AppSettingsBootstrapper` default schema so `DeepMergeDefaults` back-fills existing installs. Register `RuneCatalog`, `CarriedRuneSet`, `GlobalHotkeyService` as singletons beside the existing ones.

**Hard-to-reverse:** the catalog JSON schema and id scheme (rune ids are slugs of the 34 names; binding ids `k-<hash hex>-<hue>`), the user-layer file location, and the match tolerance.

**Open questions (non-blocking) — using defaults:** overlay segment style → names (mockup decision 1); carried marking → dashboard toggle (decision 2); shipping the reference glyphs (game artwork, as poe2db does) → ship as small thumbnails, drop them if the user prefers not to redistribute art; tier of Death/Power/Rebirth unknown until their sprites show their colour → weights as seeded.

## Acceptance criteria

- [ ] The library lists all 34 runes with reference glyph, effect text, rare marker, tier colour and editable weight; shipped weights Opulent 3, Power 2, purple 1, blue 0.5.
- [ ] A gilded key seen for the first time appears once as an unbound sprite; re-seeing it bumps its count (matcher tolerance) instead of adding another.
- [ ] Binding a sprite to a rune, and editing a weight, persists to `config/rune-catalog.json` and survives restart and app update (shipped file untouched).
- [ ] Rows render a rune segment naming new runes; best row(s) visually distinct; carried-only rows shown as redundant; unbound sprites marked `?` and scored by tier colour.
- [ ] Toggling carried, binding a sprite, or a rune-only change on screen re-renders the overlay without a name/price change.
- [ ] Reset via button and via the configured hotkey empties the carried set; empty hotkey disables it.
- [ ] Rows with no gilded rune, and snapshots with `RuneRows == null`, render exactly as today.

## Recommended Tests

- Unit: `RuneKeyMatcher` (tolerance edges, hue mismatch), `RuneCatalog` (shipped 34 load, user merge wins by id, unbound→bound migration keeps carried state, save round-trip), `RuneRowScorer` (carried exclusion, tier default for unbound, best-row ties), segment text/colour rules, snapshot/content hash changes on rune-only edits.
- Fixture: `tests/fixtures/runeicons/2560x1440/1 Raw.png` through reader → catalog; assert the two identical-rune row pairs collapse to two bindings.
- Manual: bind a sprite in the dashboard → overlay names it live; hotkey reset in-game (borderless window).

## Out of scope

- Automatic carried-set capture from the in-world remnant socket bar (the game shows the inherited rune there — best future source; needs its own capture region). Automatic run-boundary detection. Both later cards.
