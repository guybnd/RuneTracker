---
id: RUNE-4
title: 'Rune marker polish: meaningless top-pick star, hotkey warning spam'
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
    date: '2026-09-13T06:18:56.581Z'
    comment: Created ticket.
---
> **TL;DR** — First live test of RUNE-2 found two rough edges. Every marked rune gets the ★ badge when nothing is bound yet, because they all tie on weight, so the badge says nothing. And the reset hotkey logs the same registration failure eight times at startup.

Found by the user testing the merged build (master `ce4bedb`) in game, 2026-09-13. Detection itself works: the catalog learned 9 distinct gilded sprites across several panels and the frames land correctly on the icons.

## 1. Top-pick star is meaningless when weights tie

`RuneRowScorer` awards `IsTopPick` to every new rune whose weight equals the maximum. With nothing bound, every sprite scores `UnknownRuneWeight` (1.0), so **every** marked rune on screen gets a ★ — visible in the user's screenshot, four runes all starred.

**Fix:** only award the badge when the top weight is strictly greater than the next distinct weight among new runes on screen. Ties at the top with no runner-up mean there is nothing to choose between, so no badge.

## 2. Reset hotkey logs the same failure eight times

`Ctrl+Alt+R` is already taken on the user's machine (Win32 1409, `ERROR_HOTKEY_ALREADY_REGISTERED`), which is a legitimate outcome — the fallback button works. But `GlobalHotkeyService` re-registers on every `IOptionsMonitor.OnChange`, which fires many times per settings write, and logs a warning each time. Eight identical warnings at startup.

**Fix:** skip re-registration when the hotkey text has not changed, and log a given failure only once per distinct hotkey string.

## Acceptance criteria

- [ ] With every rune unbound (all weights equal), no ★ is drawn; the `?` badges and frames are unchanged.
- [ ] With one rune at a strictly higher weight than the rest, exactly that rune (or those tied at the top) is starred.
- [ ] Startup logs at most one hotkey warning per distinct hotkey string; changing the hotkey in the dashboard re-registers once.
- [ ] Suite stays green.
