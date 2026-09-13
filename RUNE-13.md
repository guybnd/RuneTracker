---
id: RUNE-13
title: Latch the panel read; draw the magazine column on screen
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
    date: '2026-09-13T10:42:45.214Z'
    comment: Created ticket.
    id: a-2026-09-13t10-42-45-214z
  - type: comment
    user: Agent
    comment: >-
      Merged as PR #14. 938 tests passing, deployed to the installed copy at
      20:41.


      The user's suggestion replaced the fix I had started. I was building a TTL
      memory inside `RuneMagazine` so the hotkey could still resolve a rune the
      hover had washed out — which would have worked for marking but left the
      markers themselves still flickering. Latching the panel read fixes both,
      and removes per-cycle work rather than adding a compensating mechanism.
      The memory stayed in as a narrow fallback for an empty sheet.


      **Residual risk:** neither overlay nor the latch can be exercised end to
      end in the test host. The latch's rules are pinned by an executable
      restatement of them in `RuneLatchTests` — the worker cannot be constructed
      without the whole OCR stack, and the test file says so — and the column's
      layout arithmetic is tested directly. Whether the strip lands where it
      should, and whether hover-then-Alt+V now works, needs a look in game.
    completionComment: true
    date: '2026-09-13T10:42:58.446Z'
    completion:
      changedFiles:
        - src/App/LeaguePricingWorker.cs
        - src/Runes/RuneRowScorer.cs
        - src/Runes/RuneMagazine.cs
        - src/Overlay/RuneMagazineOverlay.cs
        - src/Configuration/RunesOptions.cs
        - src/Program.cs
        - src/Dashboard/DashboardWindow.xaml
        - src/Dashboard/DashboardWindow.xaml.cs
        - src/Dashboard/DashboardViewModel.cs
        - tests/src/Runes/RuneLatchTests.cs
      decisions:
        - >-
          Latch the panel read keyed on row text rather than compensating for
          the hover wash downstream.
        - >-
          Replace the latch only on a read finding more runes, so a hovered
          first read self-repairs.
        - >-
          Observe only fresh reads, or sighting counts inflate with how long a
          panel stays open.
        - >-
          Keep scoring per-cycle so priority and carried edits apply without a
          re-read.
      residualRisk: >-
        Overlay placement and hover-then-Alt+V unverified outside the game;
        WPF/WinForms surfaces cannot run in the test host.
      docsUpdated: false
    id: c-2026-09-13t10-42-58-446z
  - type: status_change
    from: Ready
    to: Done
    user: Agent
    date: '2026-09-13T10:42:58.724Z'
baselineCommit: 2e4ae9c5c8bcd1b614ac52a8012767bec9ae108d
needsAction: null
implementationLink: 'https://github.com/guybnd/RuneshapePriceChecker/pull/14'
swimlane: null
diffSummary:
  - file: src/App/LeaguePricingWorker.cs
    additions: 58
    deletions: 4
  - file: src/Configuration/RunesOptions.cs
    additions: 3
    deletions: 0
  - file: src/Dashboard/DashboardViewModel.cs
    additions: 3
    deletions: 0
  - file: src/Dashboard/DashboardWindow.xaml
    additions: 13
    deletions: 0
  - file: src/Dashboard/DashboardWindow.xaml.cs
    additions: 2
    deletions: 0
  - file: src/Overlay/RuneMagazineOverlay.cs
    additions: 337
    deletions: 0
  - file: src/Program.cs
    additions: 1
    deletions: 0
  - file: src/Runes/RuneMagazine.cs
    additions: 50
    deletions: 11
  - file: src/Runes/RuneRowScorer.cs
    additions: 14
    deletions: 3
  - file: tests/src/Runes/RuneLatchTests.cs
    additions: 159
    deletions: 0
---
Two reports from the user, the first with his own fix attached:

> "small problem is that when i mouse over an icon to pick it, the reading changes and it loses its identify"

> "possibly maybe we should only pick up the images and identification ONCE when window opens we dont have to keep repolling it?"

> "id like to possibly show ON the game screen on the leftmost size in like a column, the current picked runes"

## Why hovering lost the rune

Hovering a row makes the game tint it gold, which collapses the gold-ring contrast `MinGoldContrast` requires (RUNE-4). So the rune under the cursor is precisely the one that stops being classified gilded — pointing at a rune in order to press Alt+V is what removed it from the sheet.

The user's diagnosis is right and his fix is better than the one already in progress (a TTL memory inside `RuneMagazine`): a Combinations panel does not change while it is open, so re-reading it every cycle buys nothing and costs both this and the frame-to-frame OCR jitter behind the flickering markers.

## Delivered

- **`LeaguePricingWorker.LatchRuneRows`** — reads the panel once and holds it, keyed on row text, which hovering does not change. A later read of the same panel replaces the latched one only when it finds *more* runes, so a first read taken while a row happened to be hovered repairs itself rather than sticking. Only a fresh read feeds `catalog.Observe`; re-observing latched keys would inflate sighting counts by however long the panel stayed open. Scoring still runs every cycle so priority and carried edits apply immediately.
- **`RuneRowScorer.Score(IReadOnlyList<RuneRowKeys>?)`** — new overload so the caller can score latched rows rather than the current frame's.
- **`RuneMagazine` memory** — kept as a second line of defence for the moment a sheet comes back empty.
- **`RuneMagazineOverlay`** — the runes taken this run as a column against the game window's left edge, click-through and never activated. Named runes show their reference glyph, unnamed ones the sprite as read, so a magazine filled by hotkey before any naming is readable. Rows capped to what fits the window. Toggleable in Settings.

## Acceptance

- [x] Hovering a rune no longer removes it from the sheet.
- [x] A hovered first read repairs itself when a better read arrives.
- [x] Latched keys are not re-observed.
- [x] Magazine column layout fits the window at 720/1080/1440.
- [ ] Confirmed on screen in game — position, and that hover-then-Alt+V now works.
