---
id: RUNE-14
title: Freeze the panel read per session; magazine as an interactive icon strip
status: Done
priority: High
effort: M
assignee: unassigned
tags:
  - runes
  - overlay
  - ocr
createdBy: Agent
updatedBy: Agent
history:
  - type: activity
    user: Agent
    date: '2026-09-13T10:53:57.969Z'
    comment: Created ticket.
    id: a-2026-09-13t10-53-57-969z
  - type: comment
    user: Agent
    comment: >-
      Merged as PR #15. 942 passing, deployed at 20:52.


      Both latch faults were mine from RUNE-13: keying the session on jittery
      OCR text, and a "replace when a later read finds more runes" rule that
      handed the session to a mangled read. Removed the second, replaced the
      first with panel presence.


      The strip giving up click-through is the one thing that could go wrong in
      a new way, and it cannot be tested here — if it swallows clicks meant for
      the game, the fallback is a click-through strip that only becomes
      interactive under a modifier, or a hit area shrunk to the icons alone.
    completionComment: true
    date: '2026-09-13T10:54:07.520Z'
    completion:
      changedFiles:
        - src/App/LeaguePricingWorker.cs
        - src/Overlay/RuneMagazineOverlay.cs
        - tests/src/Runes/RuneLatchTests.cs
      decisions:
        - >-
          Bound the session by panel presence rather than row text, since OCR
          text jitters.
        - >-
          Drop the replace-on-more-runes rule that let a mangled read take over
          mid-session.
        - >-
          Keep a 1.5s settling window so a read taken mid-animation is not
          frozen in.
        - >-
          Give up click-through on the strip, since hover and right-click are
          required; keep NOACTIVATE.
      residualRisk: >-
        The strip is no longer click-through and may swallow clicks intended for
        the game. Fallbacks: interactive only under a modifier, or hit area
        shrunk to the icons.
      docsUpdated: false
    id: c-2026-09-13t10-54-07-520z
  - type: status_change
    from: Ready
    to: Done
    user: Agent
    date: '2026-09-13T10:54:07.856Z'
baselineCommit: 99f459b4bdd0a2f86ba2a939007b448d475032ca
needsAction: null
implementationLink: 'https://github.com/guybnd/RuneshapePriceChecker/pull/15'
swimlane: null
diffSummary:
  - file: src/App/LeaguePricingWorker.cs
    additions: 30
    deletions: 16
  - file: src/Overlay/RuneMagazineOverlay.cs
    additions: 233
    deletions: 59
  - file: tests/src/Runes/RuneLatchTests.cs
    additions: 133
    deletions: 70
---
Merged as PR #15. 942 tests passing, deployed at 20:52.

## 1. The latch was not holding

> "it current does seem to re read itself and mangle the first row"
> "we should parse the values ONLY ONCE on load and we can dismiss everything when the screen closes. idk why we would need to re-read and reparse, the runes NEVER change during the same window session"

Both causes were mine, introduced in RUNE-13:

- **Keyed the session on row text.** OCR text jitters between reads, so a wobble looked like a new panel and re-read everything. Now keyed on the panel being detected at all.
- **Replaced the latch whenever a later read found *more* runes.** A mangled read that splits a row into extra cells has more, so the bad read won. That rule was added to rescue a first read taken mid-hover and it cost more than it bought.

A session now runs from the panel appearing to it closing. A better read may still replace the latched one during the first 1.5s, so a read taken mid-animation is not frozen in; after that it is fixed. "Nothing latched yet" is kept distinct from "frozen", so a panel that opens before its icons draw still latches when they arrive.

## 2. Magazine rebuilt to the sketch

Icons only in a narrow strip on the left edge, reset button at the top, hover to name, right-click to dismiss, scrolls when it overflows.

**It cannot be click-through any more** — it has to receive hover and right-click, so `WS_EX_TRANSPARENT` is gone. `WS_EX_NOACTIVATE` stays, so it never takes focus. The hover-label gutter is chroma-keyed, so everywhere outside the painted strip still passes clicks to the game.

## Residual risk

The overlay's on-screen behaviour is untested: that it lands on the left edge, that right-click reaches it rather than the game, and — the real unknown — that a non-click-through strip does not swallow clicks the user wanted in game. If it does, the fallback is to make the strip click-through except while a modifier is held, or to shrink its hit area to the icons alone.

Geometry is covered directly: row hit-testing, scroll limits, the reset button never being mistaken for a row, rows scrolled out of view not being clickable, and the gutter lying outside the painted strip.

## Acceptance

- [x] A session is bounded by the panel's presence, not its text.
- [x] After settling, a later read never replaces the latched one.
- [x] Panel close drops the latch; reopening starts fresh.
- [x] Strip geometry: hit-testing, scrolling, reset button.
- [ ] Confirmed in game: placement, right-click reaching the strip, no stolen clicks.
