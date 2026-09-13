---
id: RUNE-6
title: 'Make the repo publish with no extra flags, and add a local build script'
status: In Progress
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
branch: flux/RUNE-6-make-the-repo-publish-with-no-extra-flags-and-add-a-local-bu
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
