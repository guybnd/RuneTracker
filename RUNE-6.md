---
id: RUNE-6
title: 'Make the repo publish with no extra flags, and add a local build script'
status: Done
priority: Medium
effort: S
assignee: unassigned
tags:
  - build
  - dx
createdBy: Agent
updatedBy: Agent
history:
  - type: activity
    user: Agent
    date: '2026-09-13T06:42:46.953Z'
    comment: Created ticket.
    id: a-2026-09-13t06-42-46-953z
  - type: activity
    user: Agent
    date: '2026-09-13T06:42:53.354Z'
    comment: >-
      Created worktree for branch
      flux/RUNE-6-make-the-repo-publish-with-no-extra-flags-and-add-a-local-bu
    event: worktree-created
    id: a-2026-09-13t06-42-53-354z
  - type: comment
    user: Agent
    comment: >-
      Commit dd5b1b8 off master 75824ce. Verified by running a bare `dotnet
      publish -c Release -r win-x64` in the worktree: it compiled, ran the
      post-publish suite (868 passed) and produced the zip, none of which was
      possible before without two `-p:` flags.


      `scripts/build-local.ps1` was force-added, matching how the other scripts
      in that folder are tracked despite `.gitignore` line 3 ignoring
      `scripts/*`.


      Flagged, not fixed: `.github/workflows/ci.yml` pins `dotnet-version:
      8.0.x`, where the `field` keyword does not exist at any language version,
      so CI is failing on master independently of this change. Bumping it is the
      user's call.
    date: '2026-09-13T08:32:52.313Z'
    completionComment: true
    id: c-2026-09-13t08-32-52-313z
  - type: activity
    user: Agent
    comment: 'PR created: https://github.com/guybnd/RuneshapePriceChecker/pull/6'
    date: '2026-09-13T08:32:57.035Z'
    id: a-2026-09-13t08-32-57-035z
  - type: status_change
    from: In Progress
    to: Ready
    user: Agent
    date: '2026-09-13T08:32:57.035Z'
  - type: activity
    user: Temper
    comment: >-
      Temper on — auto-reviewing this ticket. It will loop review →
      re-implementation until the reviewer approves (PR left open at Ready,
      never merged), or park after 2 re-implementation attempts.
    date: '2026-09-13T08:32:57.454Z'
    id: a-2026-09-13t08-32-57-454z
  - type: activity
    user: Furnace
    date: '2026-09-13T08:32:57.520Z'
    comment: "\U0001F3AF Launch focus: You are the ONLY reviewer for this ticket in this Furnace run — no orchestrator will synthesize other reviews, so you own the decision. Your review is not complete until you call `change_status` with `reviewState` set to \"approved\" or \"changes-requested\" to match your verdict. Posting a comment that starts with **APPROVED** or **CHANGES NEEDED** is not enough by itself — without the `change_status` call, the ticket will be parked for a human to unblock even though your review already happened."
    id: a-2026-09-13t08-32-57-520z
  - type: activity
    user: Agent
    date: '2026-09-13T08:32:57.874Z'
    comment: >-
      Created worktree for branch
      flux/RUNE-6-make-the-repo-publish-with-no-extra-flags-and-add-a-local-bu
    event: worktree-created
    id: a-2026-09-13t08-32-57-874z
  - type: agent_session
    sessionId: bfb1bfaa-f038-4bce-bd03-ff4840f9e7ac
    startedAt: '2026-09-13T08:32:57.519Z'
    status: cancelled
    progress: []
    user: Claude Code
    date: '2026-09-13T08:32:57.519Z'
    enginePid: 21456
    outcome: Claude Code session stopped by user.
    endedAt: '2026-09-13T08:33:11.663Z'
    originalProgressCount: 0
  - type: comment
    user: Agent
    comment: >-
      Merged. `dotnet publish -c Release -r win-x64` now works with no extra
      flags, runs the post-publish suite and zip, and refreshes the repo's own
      publish output. `scripts/build-local.ps1` wraps it for local testing,
      mirroring the exe to extra folders and handling a running instance holding
      the file. CI's pinned .NET 8 remains a separate problem, flagged on the
      ticket.
    completionComment: true
    date: '2026-09-13T08:33:11.136Z'
    completion:
      changedFiles:
        - Directory.Build.props
        - scripts/build-local.ps1
      decisions:
        - >-
          LangVersion=preview repo-wide rather than rewriting the `field`
          properties, since the flag cannot reach the child MSBuild the Test
          target spawns
      residualRisk: >-
        CI pins .NET 8 where `field` is unavailable at any language version, so
        CI stays red until the SDK is bumped.
    id: c-2026-09-13t08-33-11-136z
  - type: status_change
    from: Ready
    to: Done
    user: Agent
    date: '2026-09-13T08:33:11.338Z'
  - type: activity
    user: Temper
    comment: >-
      Temper disarmed — a finish/merge flow is taking over session teardown for
      this ticket.
    date: '2026-09-13T08:33:11.352Z'
    id: a-2026-09-13t08-33-11-352z
  - type: activity
    user: Agent
    date: '2026-09-13T08:37:09.840Z'
    comment: Task worktree automatically reclaimed (ready-or-terminal-status)
    id: a-2026-09-13t08-37-09-840z
branch: flux/RUNE-6-make-the-repo-publish-with-no-extra-flags-and-add-a-local-bu
needsAction: null
implementationLink: 'https://github.com/guybnd/RuneshapePriceChecker/pull/6'
swimlane: null
baselineCommit: 75824ce321f646fa5dfc80fba89e660a4e3ecc2c
diffSummary:
  - file: Directory.Build.props
    additions: 8
    deletions: 1
  - file: scripts/build-local.ps1
    additions: 100
    deletions: 0
---
> **TL;DR** — `dotnet publish` fails on the installed SDK unless `-p:LangVersion=preview` is passed, and the failure lands in the csproj's own post-publish test step, which runs in a child process that cannot inherit the flag. Set the language version in `Directory.Build.props` so the repo's publish → test → zip pipeline works unaided, and add a script that publishes and syncs the exe to extra folders.

Asked for by the user after several hand-built drops: they launch from the repo's default publish output and want the build to refresh that exe rather than only a side folder.

## Why publish currently fails

`Directory.Build.props` sets `LangVersion=latest`, which is C# 13 on the installed .NET 9 SDK. `src/Dashboard/DashboardWindow.xaml.cs` uses the C# 14 `field` keyword, so the Dashboard project fails with CS8652 unless `LangVersion=preview` is passed. Passing it on the command line gets the main publish through, but the csproj's `Test` target (`AfterTargets="Publish"`) shells out to `scripts/run-release-tests.ps1`, which starts a fresh MSBuild that does not see the flag and fails with MSB3073 — so publishing has only been possible with `-p:SkipTest=true` as well.

Separately, `.github/workflows/ci.yml` pins `dotnet-version: 8.0.x`, where `field` is unavailable at any language version, so CI is presumably failing on master for the same reason. Out of scope here; flagged for the user to decide.

## Work

- Set `<LangVersion>preview</LangVersion>` in `Directory.Build.props` so publish, test and zip all work with a bare `dotnet publish -c Release -r win-x64`.
- Add `scripts/build-local.ps1`: publishes to the repo's default output, optionally stops a running instance that would lock the exe, mirrors the exe into extra folders, seeds `config/appsettings.json` into new folders so no re-setup is needed, and can drop `rune-catalog.json` for a clean library.

## Acceptance criteria

- [ ] `dotnet publish -c Release -r win-x64` succeeds with no extra `-p:` flags and refreshes `obj/Release/publish/RuneshapePriceChecker.exe`.
- [ ] The post-publish test and zip targets run rather than being skipped.
- [ ] `scripts/build-local.ps1` publishes, refuses politely when the exe is locked by a running instance unless told to stop it, and mirrors to any folders given.
- [ ] Suite stays green.
