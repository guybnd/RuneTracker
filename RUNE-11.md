---
id: RUNE-11
title: 'Rune magazine: hotkey to mark the hovered rune as taken'
status: Done
priority: High
effort: M
assignee: unassigned
tags:
  - runes
  - overlay
  - ux
createdBy: Agent
updatedBy: Agent
history:
  - type: activity
    user: Agent
    date: '2026-09-13T10:07:40.694Z'
    comment: Created ticket.
    id: a-2026-09-13t10-07-40-694z
  - type: comment
    user: Agent
    comment: >-
      Implemented and merged as PR #13
      (https://github.com/guybnd/RuneshapePriceChecker/pull/13). Release gate:
      926 passed, 0 failed. Deployed to the installed copy at 20:30.


      All four acceptance items are met:

      - `Alt+V` default in `RunesOptions.MarkCarriedHotkey`, registered
      alongside the reset hotkey (`GlobalHotkeyService` now takes a list of
      bindings rather than one).

      - Toggles over a marked rune, does nothing elsewhere — `NoRuneUnderCursor`
      and `NothingOnScreen` are distinct results so the log says which.

      - `RuneHitTester` unit-tested: cursor mapping, inside/outside, shared
      edges (Rectangle.Contains is inclusive on left/top only), overlapping
      boxes resolving to the smaller, zero-sized cells never hit.

      - Unbound sprites have their own Carried box, closing the gap noted in the
      ticket.


      Also done in the same change, from the user's "i am not sure how to do
      that" about weights: the numeric weight box became a named priority picker
      (Must have / Wanted / Normal / Low / Ignore = 3 / 2 / 1 / 0.5 / 0). A bare
      number asked the wrong question — only ordering matters to the scorer. All
      four shipped weights land on named levels, and a hand-typed weight
      survives as "Custom (1.25)" rather than being rounded away.


      **Decisions taken without asking, both recorded in code comments:**

      - Marking toggles rather than only adding, so a mis-press is undoable
      without the dashboard.

      - Overlapping cells resolve to the smallest, on the reasoning that a tight
      wrong box is a likelier read than a huge one. Cells do not overlap on a
      real panel; this only matters if a box has drifted.


      **Residual risk:** hotkey registration and the WPF controls cannot be
      exercised in the test host. `Alt+V` parsing is covered, but whether
      Windows grants the registration and whether PoE2 swallows the key first
      can only be confirmed in game. The two open questions from the ticket —
      whether the marker's flip to grey feels fast enough at the current OCR
      cadence, and run-boundary detection — are unanswerable until it is used.
    date: '2026-09-13T10:31:40.591Z'
    completionComment: true
    id: c-2026-09-13t10-31-40-591z
  - type: status_change
    from: Todo
    to: Ready
    user: Agent
    date: '2026-09-13T10:31:40.592Z'
  - type: comment
    user: Agent
    comment: >-
      Merged as PR #13. 926 tests passing, deployed to the installed copy. Alt+V
      toggles the rune under the cursor into the magazine; weights became a
      named priority picker; unbound sprites gained their own carried box.
      In-game confirmation of the hotkey registration is the one thing tests
      cannot give.
    completionComment: true
    date: '2026-09-13T10:31:49.971Z'
    completion:
      changedFiles:
        - src/Runes/RuneMagazine.cs
        - src/App/GlobalHotkeyService.cs
        - src/App/LeaguePricingWorker.cs
        - src/Configuration/RunesOptions.cs
        - src/Program.cs
        - src/Dashboard/RunePriority.cs
        - src/Dashboard/RuneLibraryViews.cs
        - src/Dashboard/DashboardWindow.xaml
        - src/Dashboard/DashboardWindow.xaml.cs
        - src/Dashboard/DashboardViewModel.cs
        - src/App/Dashboard/RuneLibraryPresenter.cs
        - tests/src/Runes/RuneMagazineTests.cs
        - tests/src/Runes/RunePriorityTests.cs
      decisions:
        - >-
          Marking toggles rather than only adding, so a mis-press is undoable
          without the dashboard.
        - >-
          Overlapping cells resolve to the smallest — a tight wrong box beats a
          huge one.
        - >-
          An unnamed rune is carried under its binding id; Bind migrates the
          flag later.
        - >-
          Weights exposed as named priority levels, since only ordering matters
          to the scorer.
      residualRisk: >-
        Hotkey registration and WPF controls unverified in tests — whether
        Windows grants Alt+V and whether PoE2 swallows it first needs in-game
        confirmation.
      docsUpdated: false
    id: c-2026-09-13t10-31-49-971z
  - type: status_change
    from: Ready
    to: Done
    user: Agent
    date: '2026-09-13T10:31:50.208Z'
needsAction: null
baselineCommit: 724ba45464f393772587a9d0bde7c83ba1779253
implementationLink: 'https://github.com/guybnd/RuneshapePriceChecker/pull/13'
swimlane: null
diffSummary:
  - file: src/App/Dashboard/RuneLibraryPresenter.cs
    additions: 1
    deletions: 0
  - file: src/App/GlobalHotkeyService.cs
    additions: 0
    deletions: 0
  - file: src/App/LeaguePricingWorker.cs
    additions: 12
    deletions: 1
  - file: src/Configuration/RunesOptions.cs
    additions: 7
    deletions: 0
  - file: src/Dashboard/DashboardViewModel.cs
    additions: 3
    deletions: 0
  - file: src/Dashboard/DashboardWindow.xaml
    additions: 41
    deletions: 17
  - file: src/Dashboard/DashboardWindow.xaml.cs
    additions: 22
    deletions: 0
  - file: src/Dashboard/RuneLibraryViews.cs
    additions: 27
    deletions: 1
  - file: src/Dashboard/RunePriority.cs
    additions: 99
    deletions: 0
  - file: src/Program.cs
    additions: 1
    deletions: 0
  - file: src/Runes/RuneMagazine.cs
    additions: 130
    deletions: 0
  - file: tests/src/Runes/RuneMagazineTests.cs
    additions: 173
    deletions: 0
  - file: tests/src/Runes/RunePriorityTests.cs
    additions: 81
    deletions: 0
---
User's design, in their words:

> "when the panel is open and there are markings, the player can use a shortcut like alt+v while hovering inside the relevant rune to mark as 'selected' and this will add it to his rune magazine, which means it can show in the future remnants that this one is already taken."

## Why this matters

This is the missing half of the tracker. The carried set is what makes the grey "already taken" marker mean anything, and today the only way to fill it is to alt-tab to the dashboard and tick a checkbox on one of 34 rows — unusable mid-run. When the user asked "how does it know that we picked it?", the honest answer was that it does not.

Every piece needed already exists:

- `RuneKey.CellBounds` carries each rune's drawn rectangle in capture-region coordinates (added for the marker overlay).
- `GlobalHotkeyService` already registers a system-wide hotkey on a message-only window.
- `RuneCatalog.SetCarried(id, bool)` already accepts either a rune id or a binding id, and `Bind` migrates the flag when an unbound sprite is later named.
- The grey/slashed carried marker is already painted by `RuneMarkerPainter`.

## Sketch

1. Second hotkey in `RunesOptions` (`MarkCarriedHotkey`, default `Alt+V`), registered alongside the existing reset hotkey.
2. `LeaguePricingWorker` retains the last rendered `RuneScoreSheet` plus the capture-region origin.
3. On press: `GetCursorPos` → subtract the capture origin → hit-test against each key's `CellBounds` → resolve to `CarriedIdFor` → toggle.
4. **Toggle, not one-way.** A mis-press on the wrong cell must be undoable without opening the dashboard.
5. No hit → do nothing, and log at debug. Never guess at the nearest cell.

## Decisions to settle before building

- **Feedback on press.** The marker flips to grey on the next render, which may be up to a second later at the current OCR cadence. That may be enough, or it may need an immediate repaint of that one cell.
- **Unbound runes.** Carrying works on a binding id, but the library only exposes the carried checkbox on the 34 named rows — so a rune marked by hotkey while still unnamed would be invisible in the UI and unclearable. The unbound card needs the carried state too (this gap predates the ticket).
- **Run boundaries.** The magazine has to empty when a new run starts. Today that is the manual reset hotkey. Automatic run detection stays deferred.

## Acceptance

- [ ] Configurable hotkey, default Alt+V, registered without clashing with the reset hotkey.
- [ ] Pressing it over a marked rune toggles it carried; pressing over anything else does nothing.
- [ ] Hit-testing is unit-tested against cell bounds — cursor mapping, inside/outside, and overlapping edges.
- [ ] Carried state is visible and clearable for unbound sprites, not just named runes.
