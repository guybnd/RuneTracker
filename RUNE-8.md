---
id: RUNE-8
title: 'Rune Library: bind-progress, filtering, carried strip, bulk forget'
status: In Progress
priority: Medium
effort: S
assignee: unassigned
tags:
  - ux
  - runes
createdBy: Agent
updatedBy: Agent
history:
  - type: activity
    user: Agent
    date: '2026-09-13T09:08:34.315Z'
    comment: Created ticket.
---
UX work on the rune tracker that needs no new captures and no sprite names, so it can proceed while both of those are outstanding.

## Problems being fixed

1. **No sense of progress toward "set and forget."** The user's ask was to configure the library once and be done. Nothing tells them how far along that is — 34 rows look identical whether 0 or 30 are bound.
2. **All 34 rows, always.** The library sits inside an already-scrolling settings pane in a window fixed at 500–960px wide. Finding the handful of rows that matter means scrolling past the rest.
3. **The carried set — the actual tracker — is invisible.** It is a column of 34 checkboxes. Answering "what have I taken this run?" means scanning all of them.
4. **Mish-mash sprites accumulate with no bulk escape.** The user hit a screen full of junk sprites (pre-hover-fix) and could only clear them one "Forget" at a time, up to the 64 cap.

## Changes

- `RuneLibrarySummary.Describe` — "9 of 34 bound · 3 carried this run".
- `RuneLibraryFilter` (All / Unseen / Seen / Carried) with a pure `Apply`, driving a filtered view over a full backing list.
- Carried strip: chips (glyph + name + dismiss) above the rows, hidden when empty; a chip's × clears that rune.
- `RuneCatalog.ForgetAllUnbound()` + a "Forget all" button on the unbound section.

Filtering and summary logic go in pure helpers so they are testable without opening a WPF window — same approach as `RuneLibraryViewTests`.

## Acceptance

- [ ] Summary reflects bound/carried counts and updates on change.
- [ ] Each filter shows exactly the matching rows; switching filters does not lose edits.
- [ ] Carried chips appear/disappear with the carried set; × clears one.
- [ ] Forget-all removes only unbound bindings, leaves bound ones and the carried set for bound runes intact.
- [ ] Everything fits the 500px minimum width.
