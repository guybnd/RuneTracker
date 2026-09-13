---
kind: pr
title: 'PR #2: Extract and fingerprint succession runes from the discarded icon strip'
branch: flux/RUNE-1-extract-and-fingerprint-succession-runes-from-the-discarded-
prNumber: 2
prState: OPEN
reviewDecision: ''
isDraft: false
implementationLink: 'https://github.com/guybnd/RuneshapePriceChecker/pull/2'
ciStatus: unknown
members:
  - RUNE-1
swimlane: null
status: Ready
id: PR-2
history:
  - type: activity
    user: Agent
    date: '2026-09-13T05:56:39.178Z'
    comment: Created ticket.
    id: a-2026-09-13t05-56-39-178z
  - type: activity
    user: Agent
    date: '2026-09-13T05:56:39.178Z'
    comment: Created (engine-managed).
    id: a-2026-09-13t05-56-39-178z
updatedBy: Agent
docRecapCommit: 93bc9d99f2cdf7670e2796c7a54cdaf8f9687e8a
---
<!-- flux:RUNE-1 -->
### Extract and fingerprint succession runes from the discarded icon strip

> **TL;DR** — The app already grabs the rune-icon strip on every scan of the Runeshape Combinations panel and throws it away to keep row detection clean. This card picks those pixels back up, works out which runes have the gold "carries forward" border, and turns each one into a stable fingerprint so RUNE-2 can tell you which rows grant runes you aren't already carrying. Implementation is committed (`14382aec`, on top of `8ff49bc`). On the one real fixture the spike gate's segmentation and separation checks now pass cleanly (6/6 rows, zero ring overlap, same rune 1-3 hash bits apart); what remains open is **sample size**, not code — one fixture, one profile, no burst sequence.

**Card A of two.** RUNE-2 consumes this output and carries almost no technical risk. **All the risk lives here.**

---
Ticket: RUNE-1
