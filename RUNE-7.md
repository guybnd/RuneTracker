---
id: RUNE-7
title: Build script blocks on unrelated instances; point builds at the install folder
status: In Progress
priority: High
effort: XS
assignee: unassigned
tags:
  - build
  - dx
createdBy: Agent
updatedBy: Agent
history:
  - type: activity
    user: Agent
    date: '2026-09-13T08:34:07.890Z'
    comment: Created ticket.
    id: a-2026-09-13t08-34-07-890z
  - type: activity
    user: Agent
    date: '2026-09-13T08:34:14.663Z'
    comment: >-
      Created worktree for branch
      flux/RUNE-7-build-script-blocks-on-unrelated-instances-point-builds-at-t
    event: worktree-created
    id: a-2026-09-13t08-34-14-663z
branch: flux/RUNE-7-build-script-blocks-on-unrelated-instances-point-builds-at-t
---
> **TL;DR** — `build-local.ps1` refuses to publish whenever *any* instance is running, even one launched from a folder it is not writing to. It should only block on instances that actually hold one of the files it is about to replace. Also found: the user launches the installed copy, which is a stale July build, so builds need to land there.

## What happened

Running the script refused with the executable locked by `C:\Users\Guy-Desktop\AppData\Local\RuneshapePriceChecker\RuneshapePriceChecker.exe`. That path is not a publish target, so the publish would have succeeded. The check compares process existence rather than process path.

## The bigger finding

That installed executable is dated **17 July** and 96 MB, against 98 MB for the current build, and its `config` folder contains only `appsettings.json` with no `rune-catalog.json` — it has never run any rune code. So the app the user reaches from their normal shortcut is the released 1.0.10 with none of RUNE-1, 2, 4 or 5 in it, which is almost certainly why behaviour has looked inconsistent between sessions: some observations came from a hand-placed build, others from the stale installed one.

## Work

- Block only on running instances whose `Path` resolves to a file this run would overwrite; leave unrelated instances alone. With `-Stop`, stop only those blocking instances rather than every process of that name.
- Make the install folder a first-class target so the user's existing shortcut launches the current build.

## Acceptance criteria

- [ ] Publishing succeeds while an instance runs from an unrelated folder.
- [ ] Publishing to a folder whose executable is held still refuses clearly, and `-Stop` closes only that instance.
- [ ] The installed copy can be refreshed so the normal shortcut runs the current build.
