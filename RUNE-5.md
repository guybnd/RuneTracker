---
id: RUNE-5
title: 'Rune Library UI: columns clipped at narrow width, weight boxes blank'
status: In Progress
priority: High
effort: S
assignee: unassigned
tags:
  - bug
  - ui-ux
createdBy: Agent
updatedBy: Agent
history:
  - type: activity
    user: Agent
    date: '2026-09-13T06:36:17.603Z'
    comment: Created ticket.
    id: a-2026-09-13t06-36-17-603z
  - type: activity
    user: Agent
    date: '2026-09-13T06:36:23.620Z'
    comment: >-
      Created worktree for branch
      flux/RUNE-5-rune-library-ui-columns-clipped-at-narrow-width-weight-boxes
    event: worktree-created
    id: a-2026-09-13t06-36-23-620z
branch: flux/RUNE-5-rune-library-ui-columns-clipped-at-narrow-width-weight-boxes
---
> **TL;DR** — The Rune Library's seven-column grid is wider than the dashboard at its default width, so the carried checkbox and part of the seen column are cut off with no way to scroll to them. Rebuild the row so it fits the narrowest supported window.

Found by the user on the merged build, 2026-09-13.

## What's wrong

`DashboardWindow.xaml` sets `MinWidth="500" MaxWidth="960"`, and the user runs it near 500. The settings `ScrollViewer` has `HorizontalScrollBarVisibility` unset (defaults to Disabled/Hidden for the vertical-only layout used everywhere else), so anything past the right edge is unreachable rather than scrollable.

The library row grid is `40 + 160 + 90 + 56 + 44 + 90 + *`, about 480px before the panel's 20px side padding and the 14px section indent — wider than the ~440px available. The carried checkbox sits in the last column and is entirely off-screen, which is the one control the feature depends on.

Also reported: the weight boxes render blank. `RuneLibraryEntryView.WeightText` is bound `OneWay` and the presenter sets `Weight` in the object initializer, so the model should be populated; needs a check on whether the value is present and merely unreadable at that size, or genuinely empty.

## Fix

- Rebuild the row to fit ~440px: reference glyph, then a flexible name column carrying the tier dot and seen count as secondary text, then weight, seen sprite and a label-less carried checkbox. Drop the separate tier-text and seen-text columns.
- Keep the header aligned with the new columns (the current header also mislabels column 4, which holds the sprite, as "Seen").
- Verify the weight value renders; add a model-level test for `WeightText`.

## Acceptance criteria

- [ ] At the 500px minimum window width, every control in a rune row is fully visible, including the carried checkbox.
- [ ] Weight shows its numeric value for every rune (3 for Opulent, 2 for Power, 0.5 for blue-tier, 1 otherwise).
- [ ] Editing a weight and toggling carried still round-trip to `config/rune-catalog.json`.
- [ ] Suite stays green.
