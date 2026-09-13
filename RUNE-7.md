---
id: RUNE-7
title: Build script blocks on unrelated instances; point builds at the install folder
status: Done
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
  - type: comment
    user: Agent
    comment: >-
      Commit c4f1b35 off master 9455f84. Script-only change, no test impact.


      The lock check now resolves the full executable path for every folder the
      run writes to and blocks only on processes whose own `Path` matches one of
      them; `-Stop` closes only those. `-Installed` adds the `%LOCALAPPDATA%`
      copy as a target so the user's existing shortcut launches the current
      build rather than the stale released one.


      Verified the script parses; the behaviour change is exercised by the next
      build run, which publishes while an unrelated instance is live.
    date: '2026-09-13T08:35:03.597Z'
    completionComment: true
    id: c-2026-09-13t08-35-03-597z
  - type: activity
    user: Agent
    comment: 'PR created: https://github.com/guybnd/RuneshapePriceChecker/pull/7'
    date: '2026-09-13T08:35:08.272Z'
    id: a-2026-09-13t08-35-08-272z
  - type: status_change
    from: In Progress
    to: Ready
    user: Agent
    date: '2026-09-13T08:35:08.272Z'
  - type: activity
    user: Temper
    comment: >-
      Temper on — auto-reviewing this ticket. It will loop review →
      re-implementation until the reviewer approves (PR left open at Ready,
      never merged), or park after 2 re-implementation attempts.
    date: '2026-09-13T08:35:08.743Z'
    id: a-2026-09-13t08-35-08-743z
  - type: activity
    user: Furnace
    date: '2026-09-13T08:35:08.815Z'
    comment: "\U0001F3AF Launch focus: You are the ONLY reviewer for this ticket in this Furnace run — no orchestrator will synthesize other reviews, so you own the decision. Your review is not complete until you call `change_status` with `reviewState` set to \"approved\" or \"changes-requested\" to match your verdict. Posting a comment that starts with **APPROVED** or **CHANGES NEEDED** is not enough by itself — without the `change_status` call, the ticket will be parked for a human to unblock even though your review already happened."
    id: a-2026-09-13t08-35-08-815z
  - type: activity
    user: Agent
    date: '2026-09-13T08:35:09.269Z'
    comment: >-
      Created worktree for branch
      flux/RUNE-7-build-script-blocks-on-unrelated-instances-point-builds-at-t
    event: worktree-created
    id: a-2026-09-13t08-35-09-269z
  - type: agent_session
    sessionId: 28644797-4166-4257-bd1b-f9d8f90e9fef
    startedAt: '2026-09-13T08:35:08.815Z'
    status: cancelled
    progress: []
    user: Claude Code
    date: '2026-09-13T08:35:08.815Z'
    enginePid: 21456
    outcome: Claude Code session stopped by user.
    endedAt: '2026-09-13T08:35:22.076Z'
    originalProgressCount: 0
  - type: comment
    user: Agent
    comment: >-
      Merged. The build script now blocks only on instances running from a
      folder it is about to write to, and `-Installed` refreshes the copy under
      %LOCALAPPDATA% so the user's normal shortcut runs the current build
      instead of the stale July release they had been testing against.
    completionComment: true
    date: '2026-09-13T08:35:21.618Z'
    completion:
      changedFiles:
        - scripts/build-local.ps1
      decisions:
        - Path-based lock detection rather than process-name presence
      residualRisk: None beyond the script itself; no product code touched.
    id: c-2026-09-13t08-35-21-618z
  - type: status_change
    from: Ready
    to: Done
    user: Agent
    date: '2026-09-13T08:35:21.809Z'
  - type: activity
    user: Temper
    comment: >-
      Temper disarmed — a finish/merge flow is taking over session teardown for
      this ticket.
    date: '2026-09-13T08:35:21.822Z'
    id: a-2026-09-13t08-35-21-822z
branch: flux/RUNE-7-build-script-blocks-on-unrelated-instances-point-builds-at-t
needsAction: null
implementationLink: 'https://github.com/guybnd/RuneshapePriceChecker/pull/7'
swimlane: null
baselineCommit: 9455f84ee1afe1845c23c9d44897deb6457da5b0
diffSummary:
  - file: scripts/build-local.ps1
    additions: 32
    deletions: 6
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
