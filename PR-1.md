---
kind: pr
title: 'PR #1: Extract and fingerprint succession runes from the discarded icon strip'
branch: flux/RUNE-1-extract-and-fingerprint-succession-runes-from-the-discarded-
prNumber: 1
prState: MERGED
reviewDecision: ''
isDraft: false
implementationLink: 'https://github.com/guybnd/RuneshapePriceChecker/pull/1'
ciStatus: unknown
members:
  - RUNE-1
swimlane: null
status: Done
id: PR-1
history:
  - type: activity
    user: Agent
    date: '2026-09-13T05:55:09.144Z'
    comment: Created ticket.
    id: a-2026-09-13t05-55-09-144z
  - type: activity
    user: Agent
    date: '2026-09-13T05:55:09.144Z'
    comment: Created (engine-managed).
    id: a-2026-09-13t05-55-09-144z
  - type: activity
    user: Agent
    comment: 'Published doc-recap artifact revision 1 (5,189 bytes).'
    date: '2026-09-13T08:48:03.948Z'
    id: a-2026-09-13t08-48-03-948z
updatedBy: Agent
docRecapCommit: e6e8326b402fad34f6c5fe7cb5d84a2daf255c52
docRecap:
  latest: 1
  revisions:
    - rev: 1
      createdAt: '2026-09-13T08:48:03.948Z'
      bytes: 5189
      title: Doc Recap
      kind: doc-recap
      docPaths:
        - .docs/project-overview.md
---
<!-- flux:RUNE-1 -->
### Extract and fingerprint succession runes from the discarded icon strip

> **TL;DR** — The app already grabs the rune-icon strip on every scan of the Runeshape Combinations panel and throws it away to keep row detection clean. This card picks those pixels back up, works out which runes have the gold "carries forward" border, and turns each one into a stable fingerprint so RUNE-2 can tell you which rows grant runes you aren't already carrying. Implementation is committed (`14382aec`, on top of `8ff49bc`). On the one real fixture the spike gate's segmentation and separation checks now pass cleanly (6/6 rows, zero ring overlap, same rune 1-3 hash bits apart); what remains open is **sample size**, not code — one fixture, one profile, no burst sequence.

**Card A of two.** RUNE-2 consumes this output and carries almost no technical risk. **All the risk lives here.**

---
Ticket: RUNE-1
