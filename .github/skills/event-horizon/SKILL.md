---
name: event-horizon
description: Event Horizon ticket workflow (orchestrator, grooming, implementation, review). Use when working on a FLUX ticket, grooming, changing ticket status, or before any task that modifies repository files in an Event Horizon workspace.
---

<skill_module name="event-horizon-orchestrator">
---
title: Event Horizon Orchestrator
order: 1
delivery: [pull-only, concatenated, modular]
deliveryNote: "🚚 pull-only for Claude — reached only via read_skill('orchestrator'), never auto-injected · concatenated into gemini/cursor/antigravity/windsurf/generic installs (always-on there) · installed per-file for copilot/cline (modular, on-demand)."
---
> ⚠️ DO NOT DELETE — Required for Event Horizon agent workflow.

## Phase: Orchestrator

Scope: Route the agent to the correct phase-specific skill based on ticket status.

---

# Event Horizon Agent — Orchestrator

Version: 2.17.0

## Overview

Event Horizon is a local-first ticket board backed by markdown files. Tickets are stored either in `.flux/` (in-repo mode) or `.flux-store/` (orphan-branch mode using a git worktree on `flux-data`). The engine abstracts this — agents interact exclusively through MCP tools and never touch ticket files directly.

## Skill Routing

| Ticket Status | Load Skill |
|---|---|
| `Grooming`, `Require Input` | grooming skill |
| `Todo`, `In Progress` | implementation skill |
| `Ready` — review-phase / reviewer-of-record sessions | review skill |
| Release orchestration | release skill |
| Cross-project mapping (multi-repo group) | mapping skill |

Read-only tasks (explanation, search, discussion) need no phase skill.

## Ticket Model

Tickets have these fields (relevant when calling `update_ticket` or reading `get_ticket` output):

| Field | Type | Notes |
|---|---|---|
| `id` | string | e.g. `FLUX-41` — set by engine, never change |
| `title` | string | Short description |
| `status` | string | Board column (e.g. `Grooming`, `Todo`, `In Progress`, `Ready`, `Done`) |
| `priority` | string | `None`, `Low`, `Medium`, `High`, `Critical` |
| `effort` | string | `None`, `XS`, `S`, `M`, `L`, `XL` |
| `assignee` | string | User name or `unassigned` |
| `tags` | string[] | From board config |
| `body` | markdown | Description / plan. MUST open with a one-glance, plain-language **TL;DR** blockquote — see "Body convention" below |
| `subtasks` | string[] | Child ticket IDs — use `create_ticket` with `parentId` to add |
| `implementationLink` | string | Commit hash or PR URL — set by `finish_ticket` |
| `branch` | string | Git branch name (e.g. `flux/FLUX-41-add-effort-field`) — set by `branch` (`action:'create'`) or portal Start Task prompt |

**Body convention — lead with a TL;DR (FLUX-953).** Every time you write or rewrite a ticket `body` (via `update_ticket` or `create_ticket`), the FIRST thing in it MUST be a short, plain-language **TL;DR** — one to three jargon-free, ELI5 sentences saying what the ticket is and what "done" looks like — so the user (and the next agent) can grasp it at a glance without reading the whole body. Format it as a leading blockquote, then the detailed Problem / Plan prose follows. Bold the 2-4 key words/phrases within the sentence(s) itself — the concrete subject, the deliverable, a sharp constraint — so a skimmer catches the gist from the bold words alone (FLUX-1298); cap it at 2-4 short phrases, not every noun, so it doesn't get visually noisy:

> **TL;DR** — one to three plain sentences **summarizing what the ticket is** and what **done looks like**.

Keep it honest and current: if a later body rewrite changes the gist, update the TL;DR in the same edit. Skip it only for a trivially short body (a line or two) where a TL;DR would just repeat it.

History is an append-only event log (types: `comment`, `status_change`, `activity`, `agent_session`). You read it via `get_ticket` and append to it via `add_note`, `change_status`. Never construct history entries manually.

`get_ticket` returns a digest: `agent_session` entries come back without their `progress[]` array (a `progressCount` is kept), and history is windowed to the most recent ~20 entries (`olderHistoryEntries` reports how many were omitted; pass `historyLimit` for more). Use `get_session_log` only when you need a specific prior session's raw progress.

Older entries that carry an agent `summary` are shown **collapsed** — `{ type, user, date, summary, id, collapsed: true }` instead of the full text (`status_change` entries are dropped entirely). Read the summary first; only when it isn't enough, fetch the full text with `get_ticket(ticketId, expand: ["<id>"])` (avoid `fullHistory: true` — it re-inflates context). Recent comments, `pin`ned entries, and anything without a summary are never collapsed. When you write a substantial `add_note` comment or activity note, pass a faithful `summary` (and `pin: true` for review handoffs / key decisions) so it stays cheap-but-recoverable for the next agent.

**Delegating:** a delegate reads the ticket itself via `get_ticket` and gets the same collapsed digest. Put the task-relevant context in the delegation `task` string; if the delegate needs a specific collapsed comment, inline it (or its id) rather than making it hunt. Delegates can `expand` selectively.

## Working Surfaces

- Ticket storage: `.flux/` (in-repo) or `.flux-store/` (orphan mode) — agents NEVER access these directly
- Board config: `config.json` in the active flux directory
- Skill templates, if installed: `.flux/skills/*.md`
- Everything else is this project's own source tree and doc conventions — Event Horizon does not prescribe a layout for it

The REST API table below covers the full endpoint surface (agents should reach it exclusively through MCP tools, never directly — see "REST API (last-resort fallback)").

## User Input Routing

- Chat for broad discussion. Ticket system for ticket-specific decisions.
- `Require Input` → history comment with one clear question + proposed defaults → user answers → route back to next status.
- `Ready` → user reviews → `finish <ticket>` → agent commits + closes atomically.

## Ticket Resolution

- `FLUX-41` → use that ticket. Bare number like `41` or `do 41` → resolve to `FLUX-41`.
- Repo-changing work without a named ticket → find or create a ticket first.
- Pure explanation, brainstorming, or read-only discussion does not require ticket state changes.

## Persisting Changes — CRITICAL

All ticket updates — status changes, metadata, body rewrites, history comments — **MUST** use the MCP tools listed below.

**NEVER do any of the following:**
- Use the `Write` tool on any file in `.flux/` or `.flux-store/`
- Use the `Edit` tool on any file in `.flux/` or `.flux-store/`
- Use `Bash` with `echo`, `sed`, `cat >`, or any shell command that writes to ticket files
- Use `curl` to hit the REST API when MCP tools are available
- Construct YAML frontmatter manually and write it to disk

The MCP tools handle schema validation, timestamps, history normalization, and portal sync. Direct file writes bypass all of this and can corrupt tickets.

### MCP Tools (use these — they appear in your tool list)

| Tool | Use When |
|---|---|
| `get_ticket` | Reading a ticket (frontmatter + body + digested recent history) |
| `get_session_log` | Reading one prior agent session's full progress log (rare — debugging only) |
| `list_tickets` | Finding tickets by status, assignee, tag, or priority |
| `get_board_config` | Checking valid statuses, tags, project key |
| `create_ticket` | Creating a new ticket — pass `parentId` to create it as a linked subtask |
| `update_ticket` | Changing metadata ONLY (title, priority, effort, tags, assignee, body) — does NOT move status |
| `change_status` | Moving to a new status (comment required for Require Input/Ready) |
| `archive` | `action:'archive'` removes a ticket from the active board (moves to `Archived`; reversible — there is no hard-delete tool); `action:'unarchive'` restores it (default `Todo`, or `toStatus`) |
| `extract_ticket` | Carving a topic-slice out of a chat stream into a NEW card (the promotion gate). Human-approved only (CONFIRM gate / board-rebase `promote`); additive + un-doable |
| `merge_tickets` | Folding several tickets/chat-streams into ONE survivor effort (the inverse of extract). Sources are tombstoned + archived (never deleted); the survivor re-derives the chronological union. Human-approved only (CONFIRM gate / board-rebase `fold`) |
| `add_note` | Adding a `type:'comment'` (human-facing comment) or `type:'activity'` (agent progress update) to ticket history |
| `finish_ticket` | Completing a ticket (sets implementationLink + Done atomically) |
| `branch` | `action:'create'` makes a feature branch (`flux/<ID>-<slug>`) + worktree; `action:'status'` returns name/existence/ahead-behind; `action:'delete'` removes it (refuses unmerged unless `force:true`) |

Notes:
- `change_status` enforces comment requirements: you MUST provide a `comment` when transitioning to `Require Input` (the question) or `Ready` (the completion summary).
- `finish_ticket` is atomic: it sets the implementation link, adds a completion comment, and moves status to Done in one operation. When the ticket has a `branch`, it also pushes the branch and creates a PR via `gh` — the PR URL becomes the `implementationLink`.
- `create_ticket` with `parentId` creates a child ticket file and links it to the parent's `subtasks` array atomically.
- All tools handle timestamps, history normalization, and schema validation server-side.
- There is **no** `switch_branch` tool. Agents stay on their ticket branch for the full session. Switching branches requires explicit user confirmation in chat.

### REST API (last-resort fallback)

ONLY use the REST API if MCP tools genuinely fail to load (i.e., `ToolSearch` returns no `event-horizon` tools). If MCP tools appear in your tool list, use them — never fall back to curl/REST "for convenience."

REST base: `http://localhost:3067`

| Method | Endpoint | Purpose |
|---|---|---|
| `GET` | `/api/tasks` | List all tickets |
| `GET` | `/api/tasks/:id?view=agent` | Read a ticket — ALWAYS pass `view=agent` (digested surface; omitting it returns the full portal payload incl. raw session logs) |
| `POST` | `/api/tasks` | Create a ticket |
| `PUT` | `/api/tasks/:id` | Update a ticket (use `appendHistory` for comments) |
| `DELETE` | `/api/tasks/:id` | Delete a ticket |
| `POST` | `/api/tasks/:parentId/subtasks` | Create a linked subtask |
| `GET` | `/api/config` | Get board config |
| `PUT` | `/api/config` | Update board config |
| `POST` | `/api/bulk-rename` | Bulk rename statuses/tags |

If neither MCP tools nor the API are reachable, surface the problem to the user and wait. Do not edit files directly under any circumstances.

Ticket changes that only exist in chat or agent memory are **lost**. The engine is the single source of truth.

## End-of-Turn Action Contract — CRITICAL (FLUX-651/826)

This is the authoritative text — phase skills (grooming, implementation) each carry only a one-line reminder plus their own status-transition mapping and link back here. Read this section once; it applies to every phase.

- **End every working turn on a board action (FLUX-651).** When you finish grooming/implementing/reviewing a ticket — including in a chat/discussion session — you MUST end the turn by moving the ticket to its next status (or `Require Input`, or creating subtasks). Never finish the work and just summarize it in chat: "it was only a discussion turn" is not an exception. If you leave a ticket parked in a working status (`Grooming` / `In Progress`) without an action, the engine flags it **"Needs Action"** on the board and notifies the user.
- **Raise decisions through a structured surface, regardless of status (FLUX-826).** Any question or decision for the user goes through `ask_user_question` or `Require Input` — never chat prose. This holds on **resting/terminal** tickets too (Done/Ready/Todo/Backlog/Released/Archived): a "should I file a ticket / commit / leave it?" call on a closed ticket typed only into chat has no picker, no notification, and no board flag, so it's lost the moment the user looks away. `Require Input` parks the *current* status (wrong for a Done ticket) — on a resting ticket use `ask_user_question` instead, whose timeout also leaves a persistent "Needs Action" flag as a safety net. A softer backstop also fires on resting/terminal tickets: ending a turn having posted a fresh comment but taken no board action and raised no structured prompt surfaces a needs-action nudge, so a decision buried in a comment on a closed ticket isn't lost. Do not rely on either backstop — route the decision yourself.

## Rich Artifacts (`publish_artifact`) — shared mechanics

`publish_artifact` spans **both ends of the lifecycle** — it is not grooming-exclusive. In grooming it publishes a plan-time **mockup / diagram / prototype** the user reasons *against* before code is written; at `Ready` the implementation skill uses the same tool to publish a **visual recap** of the diff. Same tool, same sandboxed viewer, same revision history — only the timing, content, and phase-specific emit/skip judgment differ (pull `read_skill('grooming', 'Rich Artifacts')` / `read_skill('implementation', 'Visual Recap Artifact')` for those).

**Whether to emit is calibrated per phase — there is no tag gate.** For **grooming plan proposals** the default is **ON** (almost always — see `read_skill('grooming', 'Rich Artifacts')` for the UI-or-M+ rule), because a rendered plan is far easier for the user to react to and annotate than prose. For **implementation visual recaps** it stays a judgment call — the exception, not ceremony; default OFF when unsure. Either way, never emit an artifact for something with no visual or structural shape to react to.

**How to emit — shared mechanics for both phases:**
- Pass a **complete, self-contained HTML document** as `html`: inline `<style>`/`<script>`. **Default to hand-written inline CSS** — an artifact is a single document, so a small `<style>` block is enough and renders instantly. Mermaid (`https://cdn.jsdelivr.net`) is loadable via `<script>` tag for diagrams. The Tailwind Play CDN (`https://cdn.tailwindcss.com`) is still allowed but is a **heavy last resort, not the default**: it's an in-browser compiler that recompiles on every DOM mutation and has measured 1-2s+ main-thread freezes per load — reach for it only when a utility framework meaningfully speeds up a complex prototype. Lean on the **`frontend-design`** skill for high-quality markup.
- It renders in a **sandboxed, opaque-origin iframe**: it CANNOT reach the portal, cookies, or storage, and CANNOT make network requests (no fetch/XHR — `connect-src` is blocked). Everything it needs must be inlined or come from the allowed CDNs. Do not rely on external API calls or `localStorage`.
- Do **not** inline the HTML into the ticket body (the body is injected into every session and has a 10K soft limit) — `publish_artifact` stores it in a sidecar.
- Every call is a **new revision** (history is kept — never an overwrite). Add a `title` and, when revising, a `note` on what changed. The viewer defaults to the latest revision.
- **Annotation round-trip (FLUX-874/875/892):** the user can annotate the rendered artifact two ways — **select text** (a floating composer pops up at the selection) or **right-click any element** (FLUX-892), which anchors to non-text controls — toggles, SVG chart bars, buttons — that have no selectable text. Either way the notes **collect in a host-side floating "N changes" pill** (FLUX-1362 — a unified, editable list shared with the plan-review panel; click a pin to edit its note) and **send together**. They arrive as **one** chat message starting with `🎯 Artifact annotations`: text picks list the selected excerpt (`> …`), element picks show the element label (`⊙ \`button "Save"\``); both carry a CSS-path anchor (`_anchor:_`) plus the user's note. When you receive one, revise the artifact to address **every** listed region and call `publish_artifact` again (with a `note` on what changed) so the new revision streams back to the viewer. (The viewer also offers a full-screen mode for reviewing large artifacts.) Right-click annotates the artifact **as-is** — no handles or chrome are injected into your markup, so author the design however you like.

- **Guided annotation — declare feel + decision controls (FLUX-1440):** for two specific shapes of feedback, you can skip right-click entirely by declaring plain, framework-free markup and letting the injected viewer script upgrade it into a live, auto-staging control — no hand-wired JS, no new machinery on top of the round-trip above.
  - **`data-eh-feel`** — for an open-ended variable with no right answer on paper (scroll speed, easing, spacing) that the user should *feel out* with a live control rather than have you guess a number:
    ```html
    <div data-eh-feel data-eh-label="Scroll speed" data-eh-min="0" data-eh-max="100" data-eh-default="40" data-eh-unit="ms"></div>
    ```
    The viewer renders a range slider + live readout inside that element. Settling on a value auto-**stages** an annotation (`kind:'feel'`, value read straight from the control — no right-click, no textContent guessing); re-dragging restages the same annotation in place, not a growing pile.
  - **`data-eh-decision`** — for a pivotal choice buried in prose that deserves a deliberate, located answer instead of being skimmed:
    ```html
    <div data-eh-decision data-eh-question="Empty-state treatment?" data-eh-index="1" data-eh-of="3" data-eh-default="illustration">
      <button data-eh-opt>illustration</button>
      <button data-eh-opt>hidden</button>
      <button data-eh-opt>text-only</button>
    </div>
    ```
    The viewer renders a consistent decision card (question + index/of tag + options); picking an option auto-stages `{kind:'decision', value: chosenOption}`, restaging in place on a different pick. The options MUST be child elements carrying `data-eh-opt` — a `data-eh-decision` host with no `data-eh-opt` children is malformed and gets skipped entirely (no card, not counted as a guided control). Never hand-render your own chips/sliders in place of the injected controls: they'd be dead pixels that stage nothing.
  - Both auto-stage into the **same** `annotations[]` set and **same** `postLive()` preview pill as manual annotations — staging is automatic, but the staged set is **only** transmitted back to you via the user's explicit Send action (auto-stage ≠ auto-send). When it lands in a `🎯 Artifact annotations` message, a feel pick renders its dialed value and a decision renders the chosen option alongside the usual anchor/note. These are **opt-in conventions, not required templates** — reach for them when a plan genuinely has an open-ended variable or a handful of pivotal choices (cap around 3-4 decisions per plan); don't sprout sliders and decision forms on a plan that doesn't call for them.

### Richer artifact kinds (FLUX-875) — diagrams, mockups, charts, prototypes

Because the artifact is a **real HTML page** rendered entirely by the sandboxed iframe, you are not limited to static markup — pick the form that makes the *shape of the thing* easiest to react to:

- **Mermaid diagrams** (flowcharts, sequence, ERD, state) — best for architecture/data-flow tickets. Load Mermaid from jsDelivr and let it render a `<pre class="mermaid">` block:
  ```html
  <script type="module">
    import mermaid from 'https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.esm.min.mjs';
    mermaid.initialize({ startOnLoad: true });
  </script>
  <pre class="mermaid">
  flowchart LR
    A[publish_artifact] --> B[(sidecar .flux/artifacts)]
    B --> C[GET /api/tasks/:id/artifact] --> D[sandboxed iframe]
  </pre>
  ```
- **SVG mockups** — hand-author inline `<svg>` (or inline-CSS-styled `<div>`s) for a UI wireframe the user can eyeball against their mental model.
- **Charts / data shapes** — inline SVG or a chart lib from an allowed CDN (jsDelivr/unpkg). No network calls at runtime (`connect-src` is blocked), so inline the data.
- **Clickable prototypes** — hand-written inline CSS plus a little inline `<script>` for tab/toggle interactions, so the user can click through a flow. Reach for the Tailwind Play CDN (`https://cdn.tailwindcss.com`) only for a complex, heavily-styled multi-state prototype where a utility framework earns its keep — it's a heavy last resort (see above), not the default.
- **React / TSX component previews** (FLUX-961) — for a component-shaped UI ticket, render a live React component instead of hand-drawing it: load React + ReactDOM UMD + `@babel/standalone` from jsDelivr and transpile **one inline** `<script type="text/babel" data-presets="react,typescript">` block that defines the component and mounts it to `#root`. Copy this canonical, self-contained template and drop your component in:
  ```html
  <!doctype html>
  <html>
  <head>
    <meta charset="utf-8" />
    <script src="https://cdn.jsdelivr.net/npm/react@18/umd/react.production.min.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/react-dom@18/umd/react-dom.production.min.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/@babel/standalone@7/babel.min.js"></script>
    <style> body { margin: 0; font-family: system-ui, sans-serif; } </style>
  </head>
  <body>
    <div id="root"></div>
    <script type="text/babel" data-presets="react,typescript">
      // Define your component inline — no imports of project modules (see caveat below).
      type Props = { label: string };
      function Preview({ label }: Props) {
        const [n, setN] = React.useState(0);
        return (
          <button onClick={() => setN(n + 1)} style={{ padding: '8px 16px' }}>
            {label}: {n}
          </button>
        );
      }
      ReactDOM.createRoot(document.getElementById('root')).render(<Preview label="clicks" />);
    </script>
  </body>
  </html>
  ```
  **Inline-only — no exceptions.** `connect-src 'none'` kills every network fetch, so Babel's external `src=`-transform path and any in-iframe `import` resolution are dead. Do **not** `import` project modules (`AppContext`, design tokens, sibling `.tsx`) or fetch an external `.tsx` — the component, its types, and any mock data must all live in that one `text/babel` block. This is **additive/opt-in**: plain HTML, Mermaid, and SVG artifacts are unaffected and need none of this scaffolding. React/Babel load from the CDN at view time (`'unsafe-eval'` transpiles in-browser), so the artifact won't render offline — same tradeoff as every other CDN-backed kind. Keep the block lean so the transpile-then-mount first paint stays quick; the layout audit re-fires after the async React mount settles, so a late mount won't false-warn.

Everything still renders inside the same opaque-origin sandbox (no portal/cookie/storage access, no fetch/XHR) — keep it self-contained.

### Layout audit (FLUX-875, non-blocking as of FLUX-1362) — keep artifacts visually clean

On open (and on every new revision) the viewer runs an automatic **layout audit** inside the iframe. It checks four conservative failure modes: **`overflow-x`** (page wider than the viewport), **`off-canvas`** (an element spilling past a viewport edge), **`clipped`** (`overflow:hidden`/`clip` cutting off real text), and **`overlap`** (two text blocks rendering on top of each other). The audit is **advisory and non-blocking** (FLUX-1362): the artifact **always renders immediately** — warnings surface only as a small **warning icon** on the viewer header (hover to describe each `kind`/selector/detail, click to copy the fix prompt to the clipboard). The user can still send the warnings to you from that popover; they arrive as a chat message starting with **`🧪 Layout audit`**, listing each `kind`, the element selector, and the measured problem.

**When you receive a `🧪 Layout audit` message, treat it like an annotation:** fix the offending layout (constrain widths, wrap/scroll long content, fix positioning) and call `publish_artifact` again with a `note` on what you changed, so the corrected revision re-runs the audit. To avoid warnings in the first place: give the document a sane root width, prefer responsive/flow layouts over fixed pixel widths wider than the frame, and don't absolutely-position text blocks over each other.

### Craft (FLUX-1398) — what makes a mockup actually good

Read this before your **first emit** on a ticket and before every **revision**. These are guidance, not a gate — but the failure modes below (an emoji standing in for an icon, a revision that silently redraws an approved layout, an annotation answered with the wrong fix) are rule-shaped and repeat across sessions:

- **Mock in the app's own visual language** — reuse the real palette, radii, chips, and iconography. Inline SVG icons; never emoji-as-icons.
- **Render at the true target viewport** (e.g. a real ~390px frame for mobile-first), never an idealized wide canvas.
- **Use the same realistic test data across every option**, including worst cases: the longest plausible title, an empty/default item, every status value.
- **When exploring alternatives, show 2–4 options side by side**, each with a one-line thesis and pros/cons, and recommend exactly one with reasons. Keep superseded options visible rather than deleting them.
- **Measure the contested resource** — a px-budget bar, a tap count. A number ("name gets ~85px → ~118px") settles what adjectives can't.
- **Ground every claim in code** — cite file:line for each mockup element, and verify existing affordances (drag/swipe/tap targets) survive the proposal.
- **Open with a chip-list of locked decisions** so reviews don't relitigate settled points.
- **Show interaction states** (pressed, sheet open, hover reveal) — not just a static layout.
- **Every revision answers annotations explicitly** — show the annotated element before → after at the top, and state in the `note` what changed and why. Never silently redesign elements the user already approved.
- **Style-guide lookup** — if `.docs/design/style-guide.md` exists, derive mockup tokens from it rather than re-deriving from source; if it's missing on a UI/UX ticket, pull `read_skill('grooming', 'Design Style Guide')` for the non-blocking bootstrap offer.

## Ceremony by effort — scale mandated writing to ticket size (FLUX-1382)

Output tokens bill roughly 5x input, so an XS one-line fix paying full L-ticket ceremony (standalone plan comment, full Plan-Discipline writeup, structured completion payload, visual recap) is real, avoidable cost. This table is the **canonical lookup** — the per-section `Skip for: XS/S` footnotes scattered through the grooming/implementation skills remain the detail and rationale; this table just indexes them in one place so the scaling is consistent and easy to find. It does not invent new ceremony or new skip rules beyond what those sections already state.

| Output | XS / S | M | L / XL |
|---|---|---|---|
| TL;DR blockquote | Every size — 1 line | Every size | Every size |
| Problem/Motivation + plan prose | Terse, a sentence or two — omit if the TL;DR already covers it | Problem/Motivation: 1 sentence or omit if the TL;DR already covers it. Plan: anchored steps (Plan Discipline, scaled) | Full Plan Discipline treatment |
| Standalone implementation plan comment (before coding) | **Skip** — fold into the first activity note or the completion summary | Optional, judgment call | Post before substantial work |
| Acceptance criteria / Recommended Tests / Adversarial self-review / Anchor-to-code / Hard-to-reverse callouts | Skip (each section states its own skip condition) | Situational — apply what's genuinely relevant | Full treatment |
| Structured `completion` payload + Visual Recap artifact | Skip (non-UI) | Judgment call; UI/UX → lean toward emitting | Emit for structurally interesting changes |
| Ready completion summary / `finish` completionComment / "no docs needed because…" line | Present, terse | Present, normal length | Present, full detail |
| Activity notes + faithful `summary` | By event / by note substance, not by effort — same rule at every size | Same | Same |

**Load-bearing at every size — never diet, regardless of effort:**
- `Require Input` questions (with proposed defaults)
- Review verdict + `reviewState` + changes-requested handoff (see `read_skill('review', 'reviewState Contract')`)
- Pinned review/key-decision handoffs
- The End-of-Turn Action Contract (a board action every turn)

These four are marked here precisely because they are the ones a diet-minded agent could mistakenly shrink or skip — they scale in length like everything else above, but never in presence.

## Communication Style — write for the reader (FLUX-1502)

Every comment, question, summary, and handoff has exactly one audience — the user, or the next agent. Write for that reader, not as a work log: the point lands in the first line, and nothing needs re-reading. This section governs HOW to write; "Ceremony by effort" above governs HOW MUCH.

**To the user** (`Require Input` / `ask_user_question` questions, Ready completion summaries, review verdicts, TL;DRs):
- **Lead with the outcome or the question.** The first sentence answers "what happened" or "what do you need from me"; detail follows for those who read on.
- **Plain language.** No internal codenames, persona names, or ticket-flow jargon without a one-phrase gloss. Say what changes for the user, not what you did procedurally.
- **One decision per question**, 2–4 concrete options, always a recommended default and its consequence.
- **Bold the 2–4 load-bearing phrases** (the TL;DR convention's rule, applied everywhere) so a skimmer catches the gist from the bold alone.
- **Cut filler and hedging** — no "I have successfully…", no restating their question, no padding to look thorough. Short sentences.

**To the next agent — the inter-agent protocol** (handoff comments, delegation `task` strings, worker findings, `summary` fields). This half is a fixed protocol, not a style: the reader is another agent with none of your context, and the optimum here is stable (structured artifacts beat conversational relay; strict JSON degrades reasoning-rich content — semi-structured markdown is the sweet spot):
- **Self-contained.** Exact file paths, symbol names, commands, ticket/entry ids — no pronouns for code, never "see above" or "as discussed", no references to conversation the reader cannot see.
- **Action first, evidence second.** State what the reader must do or decide in the first line; support it after.
- **Carry the decision AND its rationale.** What was decided, why, and what was considered and rejected — a conclusion without its reasoning forces re-derivation and silently loses intent at every handoff.
- **Dense facts, complete sentences.** No narrative of your process, no restating the ticket body, no telegraphic fragments that force the reader to re-derive your meaning.
- **Keep the structured skeletons** — headers like **CONTEXT SCOUT** / **REVIEW SYNTHESIS**, severity tags, `reviewState`, the `completion` payload — they are machine-read and load-bearing at every length.
- **Delegations state an explicit contract.** Every delegation `task` string names: the objective, the expected output format, the boundaries (what NOT to do — e.g. "do not change status"), and where to post the result. A vague delegation produces duplicated or divergent work.

Either audience: length scales with substance, never with effort spent. A sentence that changes nothing for the reader gets deleted.

Prompt injection of these rules is config-driven (`communicationStyle` in board Settings → Agents): the **user-facing style is selectable** (`user`: `concise` default / `detailed` / `custom` with your own text / `off`), while the **inter-agent protocol is a single fixed block** (`interAgent`: on by default, off only as a token-cost lever — it is deliberately not a style menu). The written conventions above still apply whenever this module is read, regardless of the switches.

## Critical Rules

- **End-of-Turn Action Contract (FLUX-651/826)** — see the dedicated section above; it applies to every phase and every session, chat/discussion turns included.
- NEVER use Write, Edit, or Bash to modify files in `.flux/` or `.flux-store/`. These paths are engine-managed.
- Treat ticket files as schema-sensitive. The engine validates and rejects malformed writes.
- Do not delete ticket history; append only.
- The `finish <ticket>` handoff is required before committing. Commit creation, `implementationLink` update, and status → `Done` happen as one atomic step.
- **If this repo keeps a `.docs/event-horizon/reference/*` doc set, keep it in sync with code.** If the ticket changes ticket-schema, MCP tools, REST endpoints, realtime channels, or the agent-adapter contract, update the matching reference page in the same ticket. This is an Event Horizon-repo-specific convention, not every project's — if no such directory exists here, there is nothing to do for this rule.

## Plan-review methodology (FLUX-1469)

This section is for the **plan-review gate** (`gate-runner.ts`) — it fires on a `Grooming` ticket that has no diff yet, judging the ticket's plan text (title, body, `## Acceptance criteria`, latest artifact) instead of a PR. The gate session's launch focus names each check with a one-line headline and points here (`read_skill('orchestrator', 'Plan-review methodology')`) for the full method — pulled on demand instead of pushed into every dispatch and re-persisted into ticket history on every pass (FLUX-1469). It lives in THIS module (never injected into any phase's prelude) rather than the review module, which review-phase sessions — including the gate's own — receive injected at spawn: parking it there would push the methodology into every code-review session's prelude, the exact cost the pull design avoids.

- **Anchor check.** For every file/symbol/line the plan cites, verify with Serena/grep that it still exists and still means what the plan says. Re-derive this fresh from the CURRENT code every pass — never trust a prior pass's citations, even your own. A plan is written against a snapshot of the code; by the time it's reviewed (or re-reviewed after a revise), the snapshot may have drifted. Plans should favor symbol names over bare line numbers (Plan Discipline item 1) since a line drifts the moment an earlier item lands — flag heavy line-number reliance as a Minor gap when a stable symbol was available instead.
- **Reground (FLUX-1048).** Check `.docs/release-notes/INDEX.md` plus recently Done/Released and sibling tickets (same parent) for work that already landed part or all of this plan. A plan that duplicates already-shipped work should be flagged, not approved as if the gap still exists.
- **Acceptance-criteria coverage.** If the ticket body has a `## Acceptance criteria` section, confirm it's concrete/testable and that the Implementation Plan actually addresses every item — flag any item the plan leaves uncovered. An untestable or unaddressed AC item is a real gap, not a formality.
- **Consequence tracing** (standard depth+, FLUX-1480). For every destination the plan moves content or config INTO (a file, a constant, a list, another module), name who actually consumes that destination and confirm the move still achieves the plan's stated goal — don't just check that the destination exists or that the plan is internally consistent. This is the check PR #584 was missing: its plan said "move the methodology into the review module," which is internally consistent and cites a real file, but nobody asked "review-phase sessions get that module INJECTED at spawn — does pushing content there still serve the goal of keeping it a pull?" The answer was no. Re-derive the consuming code path fresh (Serena/grep) — don't trust the plan's own description of what a destination does.
- **Duplicate check** (thorough depth only). Search open/groomed tickets for one that already covers this same change (a duplicate or near-duplicate scope) and flag it if found.
- **Adversarial self-review** (thorough depth only; Plan Discipline item 4). Read the plan as its harshest critic: find what's weak, missing, or wrong — an unanchored step, an implicit hard-to-reverse decision left unstated, a menu of options where the plan should commit to one, an obvious missing decision. A clear-cut fixable gap is Minor; a genuine judgment call the plan ducked is Major/Blocker.
- **Artifact check** (FLUX-1313) — context only; the fact itself (`hasArtifact`) is injected into your launch focus verbatim by the deterministic pre-gate lint, not something you need to re-derive. When no artifact exists and the plan is UI/UX-shaped (visual layout, a new component, an interaction change), flag it in your review comment as a gap — a flag, not a blocker; still record your verdict on the plan's own merits.
- **Body size check** (`W2`, FLUX-1584) — context only, injected verbatim when it fires. A `W2` finding is grounds to include "trim the body" in a `changes-requested` verdict, but never the sole reason to reject an otherwise-sound plan — pair it with a real gap, or note it and still approve.

The verdict contract (how to record `approved`/`changes-requested` via `change_status` and `planReviewState`) is **not** in this pulled section — it stays pushed verbatim in every dispatch (a hard constraint: a skipped pull must never break the gate). Follow the contract text in your launch focus exactly.

## End-to-End Checklist

- Ticket read fully — Relevant docs reviewed — Plan comment added (M+; XS/S folds the plan into the first activity/completion note instead — see "Ceremony by effort")
- Grooming produced a concrete plan with filled metadata
- Implementation-critical choices clarified before coding
- Status moved at the right time — Code changed in smallest surface — Validation passed
- **Docs refreshed before `Ready`/`Done` — reference pages match the new behavior, code-map points at any new modules, and the completion comment says either "docs updated: …" or "no docs needed because …"**
- Questions went through `Require Input`, not only chat
- `finish <ticket>` received before commit — Completion comment added — Status → `Done`
</skill_module>

<skill_module name="event-horizon-grooming">
---
title: Event Horizon Grooming
order: 2
delivery: [injected:grooming, concatenated, modular]
deliveryNote: "🚚 INJECTED into every Claude grooming-phase session at spawn — content added here is paid by every grooming session · concatenated into gemini/cursor/antigravity/windsurf/generic installs · installed per-file for copilot/cline (modular, on-demand)."
---
> ⚠️ DO NOT DELETE — This file is required for the Event Horizon agent workflow. Deleting it will break grooming behaviour.

## Phase: Grooming / Require Input
Scope: Interpret requirements, update frontmatter, and handle `.flux` metadata during the planning phase.

---

# Event Horizon Agent — Grooming Skill

Version: 2.19.0

## When This Skill Applies

Load this skill when a ticket's status is `Grooming` or `Require Input`.

## End-of-Turn Action Contract (FLUX-651/826)

Full contract: `read_skill('orchestrator', 'End-of-Turn Action Contract')`. For grooming specifically: complete → `change_status` to `Todo`; an implementation-critical choice unresolved → `change_status` to `Require Input` with the question + a proposed default. Never leave the ticket parked in `Grooming` with only a chat summary.

## Grooming Workflow

1. Use `get_ticket` to read the full ticket, including all history.
2. Read `.docs/INDEX.md` to identify relevant docs, then read only those files. Skip docs entirely for XS/S effort tickets.
3. Treat `Grooming` as a planning phase — do not code. Use `update_ticket` to tighten the ticket body into a concrete plan and fill inferable metadata (`priority`, `effort`, `tags`, hierarchy links).
4. If implementation-critical choices are unresolved, use `change_status` with `newStatus: 'Require Input'` and a `comment` containing one question + proposed defaults, then wait. For ambiguity that *isn't* blocking, see Plan Discipline item 3 below instead — don't flip status for something you can resolve with a stated default.
5. **Decide the artifact call, and record it (FLUX-1313).** Per the "Rich Artifacts" section below, decide whether this ticket needs a published mockup/diagram/prototype — then either call `publish_artifact`, or note in the plan why one wasn't warranted. Treat this as a checkbox in the workflow, not a standalone judgment call that's easy to forget: under `## Dynamic Delegation` launch focus (grooming split across specialist sessions — Context Scout, Requirements, Plan Review, …), the artifact decision belongs to whichever session finalizes the plan — the one that calls `change_status` to `Todo` in step 7 — not to any narrower-scoped delegate. Don't assume an earlier or later session in the chain already made the call.
6. Once resolved, use `update_ticket` to rewrite `body` with, in this order:
   - **TL;DR** (FIRST, always): a 1–3 sentence plain-language / ELI5 summary as a leading `> **TL;DR** — …` blockquote, so the user grasps the ticket at a glance without reading the full plan.
   - **Problem / Motivation** (1–3 sentences): what problem, who benefits, why prioritised.
   - **Implementation plan**: concrete steps so another agent could pick up without re-discovery. Apply the Plan Discipline items below, scaled to the ticket's size and risk. Scale ceremony to effort generally — see `read_skill('orchestrator', 'Ceremony by effort')`.
7. Use `change_status` with `newStatus: 'Todo'`. **CRITICAL: Stop execution after moving to Todo — do not begin implementation.** If the board's `plan` gate policy is `Auto` or `Auto→You` (FLUX-1263), this call may not move the ticket immediately — it instead kicks off an automated plan-review pass and the tool's response explains what happened. That's expected: stop the same way regardless of whether the move applied directly or the gate took over.
   - **Exception — fast-path / Oneshot sessions (FLUX-1380 / FLUX-1733).** This "stop at Todo" instruction is the default grooming contract, not an absolute rule. When the launch mission text explicitly identifies this session as `fast-path` (product name **Oneshot**, dispatched via `phase:'fast-path'`), that mission overrides this step for this launch only: continue straight into implementation per the fast-path mission's own instructions instead of stopping at Todo. If the mission includes a PLAN-FIRST pause, write the plan then wait for user approval via `ask_user_question` in this same session — do **not** `change_status` to Todo to get that approval (that fires the plan gate and ends oneshot). Before Ready, post an **Oneshot wrap-up** comment (docs / follow-up tickets created / validation / residual risk); never `finish_ticket`. This is a launch-time override, not a change to what a normally-dispatched grooming session does — absent an explicit fast-path mission, stop at Todo as written above. Fast-path sessions get **no** injected grooming skill; the persona mission is the contract. These skill clauses are for readers of a normal grooming session and for the docs.
   - **Exception — batch-grooming sessions (FLUX-1383).** When the launch mission identifies this session as `batch-grooming` (dispatched via `phase:'batch-grooming'` with a `batchTicketIds` member set — one session grooming several sibling tickets sharing one parent, in one sitting, instead of one session per ticket), apply this whole workflow to EACH listed member independently and in turn: read the shared parent once, then for each member run steps 3-7 on its own ticket id, ending with member's own `change_status` call (`Todo`, or `Require Input` with that member's own question). A `Require Input` or any other outcome on one member must never block, skip, or change how you groom the others — each member's status move is its own independent call, made immediately after finishing that member, never deferred or batched together. Ineligible members (already resolved server-side — L/XL effort, epic parents, past Grooming/Require Input) are excluded from the set you're handed; do not re-derive eligibility yourself. End your final turn with a one-line summary naming which members moved to Todo and which (if any) moved to Require Input, and why. This is a launch-time override (like fast-path above), not a change to single-ticket grooming.

All persistence uses MCP tools (see "Editing & Safety" below).

## Plan-reviewer Agent Handoff

When resuming a ticket that's already in `Grooming`, check `planReviewState` first. If it's `changes-requested`, read the latest plan-review comment (or the plan-approval panel's "Send back to Grooming" notes, FLUX-1273) before touching the plan — it explains what needs revising. Address every point raised, then re-run workflow step 7 (`change_status` to `Todo`) as normal. Write the revision as if the plan had been right the first time — ticket history already records what changed; never annotate the body with what a prior draft got wrong or which review round/annotation resolved a point.

The `Auto` gate's own revise-dispatch already carries this instruction via `gate-runner.ts`'s `PLAN_REVISE_FOCUS` session focus text — but that only fires when the gate itself dispatches the revision. A groomer resuming manually (not freshly dispatched by the gate — e.g. picking the ticket back up after a `you`-gate rejection, or continuing a stalled session) gets no equivalent guidance without this section.

## Plan Discipline — scale to the ticket, don't apply blanket (FLUX-978)

Borrowed from Builder.io's `agent-native` `/visual-plan` skill. Like the artifact heuristic below, **none of this is a blanket rule.** A small UI bug fix or a one-line change should stay a two-sentence plan — apply these in proportion to the ticket's size and risk, not because the section exists. Each item states its own skip condition; read the skip condition *before* reaching for the item.

1. **Anchor to real code, lead with reuse.** When the Implementation plan touches existing code, name the actual files/functions/symbols you found while reading the ticket and docs — not invented ones — and state what each step reuses (an existing action, component, or helper) before what it adds. **Prefer symbol names over line numbers** — a line cite drifts the moment an earlier item in the same plan lands; cite a line only where it's genuinely load-bearing (e.g. pinpointing one spot in a large file with no distinguishing symbol). Fewer, stabler anchors also cheapen the plan-review gate's `ANCHOR_CHECK`, which re-derives every citation on every review pass.
   - *Skip for:* XS tickets and single-line fixes where "fix line N in file.ts" is the whole plan.
2. **Call out hard-to-reverse decisions.** If the ticket touches wire format, public ids, data-model/schema shape, or auth/ownership boundaries, name those decisions explicitly in the plan and state what's deferred vs. decided now.
   - *Skip for:* UI-only, XS/S, or bug-fix tickets — never add an empty section just to have covered it; only write it when such a decision genuinely exists.
3. **Non-blocking ambiguity → an "Open Questions" note, not always `Require Input`.** Reserve `Require Input` (workflow step 4) for genuinely blocking, batched (2–4 max) choices. Anything resolvable with a stated assumption goes in a short `Open Questions (non-blocking) — using default: …` line inside the plan instead of a status flip.
   - *Skip for:* the common case — most small tickets have no real ambiguity. Omit the line rather than force one.
4. **Adversarial self-review before `Todo`.** Delegate one pass whose only job is to find what's weak, missing, or wrong in the plan you just wrote (not re-research the repo): unanchored steps, an implicit hard-to-reverse call, a menu of options where the plan should commit to one, an obvious missing decision. Fix clear-cut issues yourself; route genuine judgment calls to `Require Input`.
   - *Reserve for:* L/XL effort tickets, or anything touching architecture, data-model, migration, multi-file changes, or an irreversible decision. This is the most expensive item here and the one most likely to be over-applied — **skip outright for XS/S, UI-only, or single-decision tickets.**
   - *Overlap with the automated gate (FLUX-1263):* when the board's `plan` gate is `Auto`/`Auto→You` and the ticket resolves to Thorough depth (L/XL effort — the same threshold as "Reserve for" above), `gate-runner.ts`'s Thorough-depth check runs this exact wording (`ADVERSARIAL_CHECK`) automatically once you move to `Todo` — doing it manually here is redundant with what the gate is about to do anyway. Still do it manually under a `you` gate (the gate never fires) or at a depth lower than Thorough (the automated check doesn't run there).
5. **Acceptance criteria, for tickets with a Ready/PR review flow (FLUX-1148).** Write a `## Acceptance criteria` section in the body as a GFM checkbox list (`- [ ] …`) — concrete, checkable statements a reviewer (or the portal) can verify without re-deriving intent from prose. This is a documented convention, not a new schema field or an engine gate: the portal renders an advisory "X/Y checked" progress indicator parsed from this section, and the review skill has the reviewer tick items off before recording a verdict — nothing blocks on it.
   - *Skip for:* XS/S-effort tickets and tickets with no Ready/PR review flow (pure discussion, read-only, spikes).
6. **Recommended Tests, for tickets with a non-obvious testing approach (FLUX-1273).** Write a `## Recommended Tests` section in the body — a short list or prose naming what layer to test and the key scenarios, especially anything a reviewer wouldn't guess from the Acceptance Criteria alone. The plan-approval panel's Tests tab parses a `## Recommended Tests` or `## Test plan` heading (case-insensitive) and renders it alongside Acceptance Criteria; without one, it just shows an empty state.
   - *Skip for:* XS/S-effort tickets, UI-only tickets, and tickets where the test approach is self-evident from the Acceptance Criteria (e.g. "existing suite covers this," "run `npm run check`").
7. **Consequence tracing, when the plan moves content/config into a destination (FLUX-1480).** For every file, constant, list, or module the plan says to move something INTO, name who actually consumes that destination and confirm the move still achieves the plan's goal — don't stop at "the destination exists and the plan reads consistently." The gate's own Standard-and-above check (`CONSEQUENCE_CHECK` in `gate-runner.ts`) re-asks this at review time; asking it yourself first catches the mistake before a review pass has to.
   - *Skip for:* plans that don't move anything into a shared destination (most bug fixes, UI tweaks, single-file additions).
8. **State each constraint once (FLUX-1582).** Write a shared constraint — a validation rule, a derived value, an edge case — in the one implementation step where it's acted on. Acceptance Criteria and any Risks section may reference it by name ("see item 2") but must never restate it in their own words. A Risks/Considerations section that just paraphrases the impl plan instead of naming a genuinely new risk gets cut, not trimmed — restating isn't a lighter version of the same information, it's the same information twice.
   - *Skip for:* tickets with only one implementation step — nothing to restate across.

## "Reground before starting" — tickets filed from point-in-time analysis (FLUX-1048)

Tickets born from a **point-in-time codebase analysis** — tech-debt sweeps, refactor epics, audit/churn findings — cite file:line evidence that is only valid on the day of the analysis. When such a ticket is expected to be picked up **later** (Backlog/Todo queue, epic members), its body MUST include a `## ⚠️ Reground before starting` section (placed right after the TL;DR / Problem prose) that tells the implementer to:

1. **State the snapshot date** — "the findings below are a snapshot from YYYY-MM-DD" — so staleness is visible at a glance.
2. **Re-derive the evidence** — re-verify cited file:line references via Serena/grep against current code; recorded line numbers are historical, never trust them as-is.
3. **Check for partial fixes already landed** — check `<releaseNotesPath>/INDEX.md` (default `.docs/release-notes/INDEX.md`) first, the agent-consumable index of every released ticket with a one-line completion gist (FLUX-1151); it only covers already-*released* work, so also scan sibling tickets and recently Done/Released tickets — another ticket may have absorbed part (or all) of the work.
4. **Update the plan against current reality before coding** — rewrite the body (keep the TL;DR honest) to match what the code looks like now. If the finding no longer exists, re-scope or propose archiving — implementing a stale plan is worse than doing nothing.

See epic **FLUX-1043** and its subtasks **FLUX-1044/1045/1046** for the reference format. When grooming an analysis-derived ticket, add this section if it's missing. The section binds the *implementer* too — the implementation skill's "Reground Before Coding" section requires executing it before any code change.

- *Skip for:* tickets being implemented immediately after grooming, and tickets whose plan cites no point-in-time evidence (pure feature requests, UI tweaks, bug reports with a live repro).

## Rich Artifacts (`publish_artifact`) — default ON for plan proposals

Shared mechanics — lifecycle framing, sandbox rules, CDN policy, revisions, the annotation round-trip, the layout-audit gate, and richer artifact kinds (Mermaid/SVG/charts/prototypes, plus live React/TSX component previews) — live in `read_skill('orchestrator', 'Rich Artifacts')`; pull it before your first emit. This section covers only grooming's emit/skip judgment.

For grooming: a plan proposal is far cleaner for the user to work with — and to **annotate their change requirements onto** (the annotation round-trip) — as a rendered artifact than as prose. So **default to publishing a self-contained HTML artifact** the user reasons *against* — a rendered mockup, an architecture/flow diagram, an interactive prototype, or acceptance criteria laid out visually — catching misunderstanding *before* code is written. Use the `publish_artifact` MCP tool; the artifact renders in the ticket's artifact panel.

This is a **default-ON** rule, not the old "exception, not the norm" — **almost always emit for a plan proposal:**

- **Emit** when the ticket is **UI/UX (any effort)**, or **M+ effort** (M / L / XL) otherwise — a mockup/prototype for UI, an architecture/data-flow diagram for non-UI structural work.
- **Skip** only for **XS/S non-UI** tickets with no visual or structural "shape" to react to (a one-line fix, pure backend plumbing). A markdown plan is the right output for these.

When in doubt on a plan proposal, emit one.

A plan with a genuinely open-ended "feel" variable (no right answer on paper — scroll speed, easing, spacing) or several pivotal choices buried in prose is a strong signal to emit — and to use the `data-eh-feel`/`data-eh-decision` guided-annotation controls so the user settles them directly on the rendering instead of guessing in a comment. This reinforces the UI-or-M+ rule above; it doesn't replace it.

**Guided-control markup contract (FLUX-1440).** The viewer script upgrades two declarative attributes — `data-eh-feel` (an open-ended value the user dials in on a slider) and `data-eh-decision` (a pivotal either/or, rendered as a decision card) — into live, auto-staging controls; you write plain markup, the viewer injects the interactive UI. **Do not hand-render your own chips, sliders, or option buttons** (they'd be dead pixels — only the injected controls stage annotations). Pull the exact markup shapes via `read_skill('orchestrator', 'Rich Artifacts')` before using either one.

Interacting stages the annotation into the same "N changes" pill as manual select/right-click annotations; the user still sends the batch explicitly (auto-stage ≠ auto-send). Opt-in, not ceremony: cap around 3-4 decisions per plan, and don't sprout controls on a plan that doesn't call for them.

This judgment call is workflow step 5, not just a section to remember on your own (FLUX-1313) — see the ownership note there for Dynamic Delegation. The plan-review gate also checks for this: a UI/UX-shaped plan with no artifact gets flagged in the review comment as a gap rather than silently approved, so a missed decision here surfaces there too — but that's a backstop, not a substitute for making the call at grooming time.

## Design Style Guide (`.docs/design/style-guide.md`) — convention + bootstrap mission (FLUX-1399)

**The convention.** A project's de-facto visual language belongs in one checkable doc instead of being re-derived from source on every artifact: `.docs/design/style-guide.md` per repo (or a `group_doc` for a multi-repo group, so every member reads the same guide). When it exists, mockup and prototype work should pull tokens from it — palette + semantic colors, type scale, spacing/radius scale, iconography rules, the component vocabulary in active use, interaction conventions (e.g. swipe/hold/hover-reveal on `pointer: fine`), theming constraints, and the primary target viewport — instead of re-reading component source on every revision. Re-deriving the same visual language from scratch each time is exactly the drift this convention removes.

**The bootstrap mission — when the guide is missing.** Any normal grooming session can run this; no engine change or new persona is required:
1. Read the real design system from code — theme/tailwind config (or equivalent), shared/primitive components, a few representative screens — never a prose description of it.
2. Extract the de-facto system from what you read: palette + semantic color roles, type scale, spacing/radius scale, the component vocabulary actually in use, interaction conventions, theming constraints, primary viewport.
3. Publish it as a visual artifact via `publish_artifact` — color swatches, a type ramp, and a small component zoo (buttons, cards, inputs, chips — whatever the project actually uses) — so the user reasons against a rendering, not prose.
4. Iterate through the normal annotation round-trip (see "Rich Artifacts" above) until the user is satisfied.
5. Once approved, write the doc to the conventional path (or submit it via `group_doc` for a multi-repo group) in the same ticket that ran the bootstrap.

**When to offer it.** Non-blocking: a UI/UX ticket with no style guide present is a natural moment to flag the gap and offer to bootstrap one — never block a ticket on it. Small or single-screen projects may never need one; that's fine.

## Epic → Subtask Splitting — Affordance Coverage Check (FLUX-1274)

An epic with published artifact revisions can get cut into well-scoped subtasks that, individually, all look correct — and still **collectively drop an affordance the approved mockup showed**, because no subtask's own review has visibility into what its siblings cover. The plan-review gate (FLUX-1263) doesn't close this either: it reviews one ticket's plan in isolation, so it approves each subtask individually and still misses an epic-level coverage hole. This happened for real on `FLUX-1247`: rev 1-2 of its mockup showed the flagged plan surfacing three ways — a rich panel (artifact embedded inline + an annotation/notes thread), an in-chat prompt, and a board-card stripe — but the 4 subtasks cut from it (`FLUX-1261`-`1264`) only ever scoped the tray item; the panel and in-chat surfacing had no owning subtask and shipped nothing until the user tried the feature and a human filed the gap (`FLUX-1273`).

When you reach "design finalized — ready to split into subtasks" for an epic that has one or more `publish_artifact` revisions, before creating any subtask ticket:

1. **Enumerate every distinct affordance the *latest* revision of each published artifact shows.** A revision supersedes earlier ones — the latest is the approved scope, not the sum of every draft. List screens, panels, and interactions as separate line items, not one blob ("rich panel with inline artifact", "in-chat prompt", "board-card stripe" — not just "the UX").
2. **Map every affordance to the subtask(s) whose Acceptance Criteria will build it.** An affordance with no owner is a blocking gap — fold it into an existing subtask's Acceptance Criteria or `create_ticket` a new subtask for it before any subtask moves to `Todo`. A subtask's own scoping note (e.g. "no new component needed") is not a substitute for this — it only reasons about that subtask's own scope, with no visibility into whether a *sibling* covers what it's excluding.
3. **Write the map into the epic's own body** as a `## Subtask Coverage Map` table (`| Affordance | Subtask |`), inside or directly under its `## Acceptance criteria`. An uncounted mental pass is exactly what failed here — the map only works if it's checkable, not remembered.
4. Only move the epic (and let its subtasks proceed to `Todo`) once every row has an owning subtask.

- *Skip for:* epics with no published artifacts (nothing to drop), and subtask splits with no design/mockup phase behind them.

## Metadata Conventions

| Field | Values |
|---|---|
| `priority` | `None`, `Low`, `Medium`, `High`, `Critical` |
| `effort` | `None`, `XS`, `S`, `M`, `L`, `XL` |
| `tags` | Use existing tags from board config; propose new ones only when clearly distinct |
| `assignee` | Set if user indicated ownership; leave `unassigned` otherwise |

## Editing & Safety

- All writes go through MCP tools (or the REST API as last-resort fallback). NEVER use Write, Edit, or Bash to modify ticket files.
- MCP tools handle `updatedBy` attribution and history normalization automatically.
- Do not read or write files in `.flux/` or `.flux-store/` — use `get_ticket` instead.

## Comment Conventions

- Keep comments factual and short. End input requests with a concrete question and proposed default.
- Prefer comments that help the next agent continue without re-discovery.
- **Write for the reader (FLUX-1502):** to the user (questions, `Require Input`, TL;DRs) — lead with the outcome or question, plain language, bold the load-bearing phrases; to the next agent (handoffs, findings) — self-contained facts with exact paths/symbols/ids, action first, never "see above". No filler, no hedging. Full rules: `read_skill('orchestrator', 'Communication Style')`.
- **Substantial comments: add a faithful `summary`** on `add_note` (preserve the decision / why / actionable detail; concise but not lossy; length scales with importance — don't force one line; skip for short notes). Older summarized comments show collapsed in the agent digest; the full text stays fetchable via `get_ticket` with `expand: ["<id>"]`. Set `pin: true` on entries that must never collapse. When a comment **replaces an earlier decision** in this ticket, pass `supersedes: ["<id>"]` so the dead entry collapses to a marker (a pinned/user-authored target stays full, advisory-only — the engine won't bury human intent).
</skill_module>

<skill_module name="event-horizon-implementation">
---
title: Event Horizon Implementation
order: 3
delivery: [injected:implementation, concatenated, modular]
deliveryNote: "🚚 INJECTED into every Claude implementation-phase session at spawn — content added here is paid by every implementation session · concatenated into gemini/cursor/antigravity/windsurf/generic installs · installed per-file for copilot/cline (modular, on-demand)."
---
> ⚠️ DO NOT DELETE — This file is required for the Event Horizon agent workflow. Deleting it will break implementation behaviour.

## Phase: Todo / In Progress
Scope: Write code, validate logic, format commits, and close tickets during the implementation phase.

---

# Event Horizon Agent — Implementation Skill

Version: 2.18.0

## When This Skill Applies

Load this skill when a ticket's status is `Todo` or `In Progress`.

**Fast-path / Oneshot sessions (FLUX-1380 / FLUX-1733).** A ticket can also reach implementation from `Grooming` in the same session, with no `Todo` handoff, when it was dispatched with `phase:'fast-path'` (user-facing name **Oneshot**) — one session grooms an XS/S ticket inline and then continues straight into this skill's workflow, per the grooming skill's matching "stop at Todo" exception. Before Ready, post an **Oneshot wrap-up** comment covering applicable items (docs updated or "no docs because …"; follow-up tickets created or "none because …"; validation; residual risk). Never `finish_ticket` and never run a product build from this session. If the mission includes PLAN-FIRST, pause for user approval in-session after writing the plan — do not move to Todo. Fast-path sessions get **no** injected implementation skill; the persona mission is the contract. This combined groom-then-implement contract is sanctioned only when the launch mission says fast-path; a normally-dispatched implementation session still expects a ticket that already went through a separate grooming pass and reached `Todo`/`In Progress` on its own.

## Commit-Before-Ready — CRITICAL (FLUX-730)

**If the ticket has a branch or worktree, you MUST `git commit` your work BEFORE moving it to `Ready`.** Moving to `Ready` is what opens the PR for review, and a branch with **no commits ahead of base cannot open a PR** — so reaching `Ready` uncommitted means the work sits silently in the worktree and no review ever happens (the FLUX-716/717/719 incident).

- **The engine now ENFORCES this for worktree branches.** `change_status → Ready` is **refused** (status unchanged, an error returned) when a worktree branch has 0 commits ahead of base. You will *not* reach `Ready`; you'll get an error telling you to commit and retry. Don't fight it — commit, then retry the move.
- Do **not** confuse this with the branchless flow below. **Branch/worktree ticket → commit, THEN `Ready`.** Branchless ticket → stay uncommitted until `finish`. These are opposite; never apply the branchless "don't commit yet" habit to a branch/worktree ticket.
- One focused commit with a real message ("Add X", not "wip"). The engine pushes it for you when you move to `Ready`; `finish` then merges the resulting PR.

## End-of-Turn Action Contract (FLUX-651/826)

Full contract: `read_skill('orchestrator', 'End-of-Turn Action Contract')`. For implementation specifically: complete and validated → `change_status` to `Ready` with a completion summary; blocked on a decision → `change_status` to `Require Input` with the question + a proposed default. "Cannot decide whether to proceed" is itself a `Require Input` — raise it, don't leave it only in your final chat message. This applies just as much on a ticket that's already **Done / Ready / Todo / Backlog / Released / Archived** (a PR follow-up, a backfill, a "should I commit this / file a ticket / leave it?" call) — raise it via `ask_user_question`, not chat prose.

## Implementation Workflow

1. Use `get_ticket` to read the full ticket, including all history, before touching any file. If the body carries a `## ⚠️ Reground before starting` section, **execute it before any code change** — see "Reground Before Coding" below.
2. **Check for a branch.** Call `branch` with `action: 'status'` on the ticket. If `branch` is set, run `git fetch origin <branch>` then `git checkout <branch>` before making any changes (the branch is created remotely via the portal and may not exist locally yet). If no branch is set, proceed on the current branch (the user chose "start normally" at task start). **Exception — dedicated worktree:** if your working directory is already inside `.eh-worktrees/` (an Event Horizon task worktree), you are ALREADY checked out on the ticket branch in an isolated tree — do **not** `git checkout` (it's unnecessary, and you must never switch the worktree's branch). Just work in place — and remember you **MUST `git commit` in the worktree before `Ready`** (the engine refuses the `Ready` move otherwise — see "Commit-Before-Ready" above).
3. For M+ effort tickets, check `.docs/INDEX.md` for relevant docs. Read nearby implementation files. Prefer the smallest owning surface.
4. Use `add_note` (`type: 'comment'`) to post your implementation plan before substantial work. **XS/S carve-out:** skip the standalone plan comment — fold a one-line plan into your first activity note or the completion summary instead. Scale ceremony to effort — see `read_skill('orchestrator', 'Ceremony by effort')` for the full breakdown.
5. Use `change_status` with `newStatus: 'In Progress'` before the first substantive code change.
6. Make small, local changes and validate immediately after the first edit. If you extract a pure helper "for testability," also cover the **caller that sequences it** — a tested leaf with an untested orchestrator isn't done. Drive the assembled behavior through the caller's seam, not only the leaf.
7. Use `add_note` (`type: 'activity'`) to record progress when scope changes, validation fails, or the user redirects.
8. If clarification is needed, use `change_status` with `newStatus: 'Require Input'` and a `comment` — do not ask only in chat.
9. When moving to `Ready`: use `change_status` with `newStatus: 'Ready'` and a `comment` summarizing what was implemented, validated, and any caveats. Scale the completion summary and the two judgment calls below to effort — see `read_skill('orchestrator', 'Ceremony by effort')`.
   - **Branch / worktree tickets (PR flow):** **commit your work BEFORE moving to `Ready`** — see "Commit-Before-Ready" above. For a worktree branch the engine **refuses** the `Ready` move with 0 commits ahead (status unchanged); commit, then retry. Moving to `Ready` then opens the PR for review.
   - **Branchless tickets (direct flow):** keep code files uncommitted at this stage; the commit happens at `finish`.
   - **Visual Recap (judgment call, FLUX-976):** for a UI/UX or structurally interesting change, consider publishing a visual recap of the diff *before* the `Ready` move so the reviewer scans "what changed" instead of only the raw PR diff — see "Visual Recap Artifact" below. Skip it for bug fixes, XS/S effort, and trivial diffs.
   - **Structured completion payload (judgment call, FLUX-1147):** for a non-trivial change, also pass a `completion` object to `change_status` alongside the `comment` — `changedFiles` (repo-relative paths), `validation` (the commands you ran + whether each passed), `decisions` (non-obvious calls worth flagging), `residualRisk`, `docsUpdated` — so the reviewer/next agent/Furnace read fields instead of re-parsing your prose. It's persisted on the comment entry, not frontmatter, and is purely additive (never required). Skip it for the same bar as Visual Recap: bug fixes, XS/S effort, trivial diffs.
10. **Before `Ready` or `Done`, update any project docs the behavior change touches.** This is part of the ticket, not a follow-up. Check first:
    - Reference docs for schema/tool/API/contract changes — e.g. this repo keeps its own at `.docs/event-horizon/reference/*` (a MUST-update in *this* repo when a ticket changes ticket schema, MCP tools, REST endpoints, realtime channels, or the agent-adapter contract); apply the equivalent convention if this project has one, skip if it doesn't.
    - An architecture/code-map doc, if this project keeps one — add an entry when a new module becomes the right "land here first" file for future agents (this repo's copy: `.docs/event-horizon/architecture/code-map.md`).
    - Root `README.md`, integration guides, and any installed skill templates, when user-facing or agent-facing behavior changes.
    - `.docs/design/style-guide.md` (if present) — if the ticket changes the visual system (palette, type scale, spacing/radius, component vocabulary, interaction conventions), update the guide in the same ticket. See `read_skill('grooming', 'Design Style Guide')` for the convention and the bootstrap flow if the guide doesn't exist yet.
    - If nothing needs updating, say so explicitly in the completion comment ("no docs needed because …") instead of skipping the check silently.
11. On `finish <ticket>`:
    - **Branchless tickets:** stage all relevant files (code + docs), create the commit, then use `finish_ticket` with `implementationLink` (commit hash) and `completionComment`. Status moves to Done atomically. If you skipped the `completion` payload at `Ready` (or the ticket has no `Ready` step at all), `finish_ticket` accepts the same `completion` param — same judgment call as step 9.
    - **Branch / worktree tickets:** the implementation commit already exists (made before `Ready`) and a PR is open. `finish_ticket` merges the PR and advances to Done — the PR URL is the `implementationLink`. If you made further changes (e.g. docs) after `Ready`, commit + `git push origin <branch>` first so the PR updates, then `finish_ticket`.
    The completion comment should name the docs you updated, or state why none were needed.
12. **Never end a session with a blocking decision only in your final chat message — on any status.** If you cannot safely finish (e.g. the branch bundles other tickets' work / an integration PR, or the merge is an irreversible one-way door you're unsure about), move the ticket to **Require Input** with the decision + options and stop — a question left only in your final message is invisible on the board and will be missed (FLUX-570). This applies just as much when the ticket is already **Done/Ready/closed**: route the decision through `ask_user_question` (the status-independent picker), not chat prose, so it's caught even if the user isn't watching the live chat (FLUX-826).
13. **Shared-PR finish guard (FLUX-569).** `finish_ticket` **refuses** to finish a member ticket whose branch is shared by **non-Done sibling tickets** — merging would advance them all to Done as a one-way door (the FLUX-556/PR#6 incident). When you hit this: either finish/close the siblings first, merge the whole branch via the **PR ticket's** Merge action, or — only if you genuinely intend to land the entire shared PR — re-run with `force: true`. Don't reflexively force; if it's a real decision, route it through Require Input (per #12). PR tickets (`kind:'pr'`) are exempt — merging one to advance its members is the sanctioned shared-merge surface.

**Body convention (FLUX-953):** whenever you rewrite or extend a ticket `body`, keep a plain-language **TL;DR** blockquote as its first line — add one if it's missing, refresh it if the gist changed. The user reads that TL;DR instead of the whole body.

## Reground Before Coding — tickets with a "⚠️ Reground before starting" section (FLUX-1048)

Tickets filed from a **point-in-time codebase analysis** (tech-debt sweeps, refactor epics, audit/churn findings) carry a `## ⚠️ Reground before starting` body section — the grooming skill's convention. That section is a **work instruction to you, the implementer**, not background prose: the plan was written against a snapshot of the code, and by pickup time the evidence has often drifted. Before the first substantive code change:

1. **Treat every cited file:line as historical.** Re-derive the evidence via Serena/grep against current code — the section's snapshot date tells you how stale it may be. Never edit at a recorded line number without re-verifying it.
2. **Check for partial fixes already landed.** Check `<releaseNotesPath>/INDEX.md` (default `.docs/release-notes/INDEX.md`) first — the agent-consumable index of every released ticket with a one-line completion gist (FLUX-1151), cheaper than scanning release-note files line by line. It only covers already-*released* work, so also scan sibling tickets and recently Done/Released tickets — another ticket may have absorbed part (or all) of the work.
3. **Update the plan first, then code.** Rewrite the body against current reality via `update_ticket` (keep the TL;DR honest) and note what you re-verified in your plan comment (workflow step 4).
4. **If the finding no longer exists, do not implement the stale plan.** Re-scope the ticket, or propose archiving — route that decision through `Require Input` (or `ask_user_question` on a resting ticket), per the End-of-Turn Action Contract.

Skipping the reground because "the plan still looks plausible" is exactly the failure mode this section exists to prevent — plausible-but-stale plans implement cleanly and land wrong.

## Visual Recap Artifact (`publish_artifact`) — the exception, not the norm

The grooming skill publishes a plan-time **"before"** artifact (a mockup/diagram the user reasons against before code is written). This is the **"after"** half: at `Ready`, publish a **visual recap** of the diff so the reviewer reviews a scannable rendering of *what changed* instead of only the raw PR diff — inspired by Builder.io's agent-native `/visual-recap`. Shared mechanics (sandbox rules, CDN policy, revisions, annotation round-trip, layout-audit gate, richer artifact kinds) live in `read_skill('orchestrator', 'Rich Artifacts')` — pull it before your first emit.

Not required for every `Ready` move — keep it the exception, not ceremony. Default OFF when unsure.

- **Emit when** the change is UI/UX, touches a data model / API shape, or is otherwise structurally interesting — anything where a rendered "what changed" surface helps the reviewer more than the raw diff.
- **Skip when** it's a bug fix, an XS/S-effort ticket, or a trivial diff with no shape worth visualizing. A plain completion comment is the right output for these.
- **Skip when the ticket's branch touches a docsRoot `.md` file (FLUX-1662).** The engine auto-publishes a `kind:'doc-recap'` artifact on the same Ready move — a changed-files header plus the rendered after-state of each changed doc, with its own self-serve inline editor. Don't also hand-build a manual Visual Recap for the same docs change; it would just add a visually-indistinguishable duplicate artifact.

**How to emit** (do this *before* `change_status → Ready`, so the recap is present when the PR opens):
1. Build the diff against base — `git diff <baselineCommit>...HEAD` (branch/worktree tickets) or `git diff` on the uncommitted working tree (branchless). Pull out the touched-file list and the key hunks (the ones a reviewer actually needs — not the full raw patch).
2. Author a **complete, self-contained HTML document** as `html`: a touched-file tree, styled key diff hunks (not the entire patch), and a short plain-language summary of what changed and why. Lean on the **`frontend-design`** skill for the rendering.
3. Call `publish_artifact` with a `title` and a `note` that both include the word **"recap"** — this is what tags the revision as an implementation recap (distinct from grooming revisions in history) and is what the portal reads to label the panel **"Visual Recap"** instead of "Artifact".
4. Then proceed with the `Ready` move as normal.

## Branch Rules

- **Stay on your branch.** Once on a ticket branch, never run `git checkout` to another branch without explicit user confirmation in chat. If a switch is genuinely needed, stop and ask first.
- **Branch creation is not your decision.** The user chose whether to create a branch when starting the ticket from the portal. Do not create one unless `branch` (`action: 'status'`) returns no branch and the user explicitly asks.
- **Returning from Ready.** If the ticket is moved back to `In Progress` after review, re-read the most recent comment first. Check out the existing branch (still in the `branch` field), apply changes, commit, then run `git push origin <branch>` explicitly before calling `finish_ticket`. The open PR updates automatically from the push.
- **XS tickets.** Branch creation is optional and often skipped for XS effort tickets.

## Reviewer Agent Handoff

When a reviewer sends a ticket back to `In Progress`, read that structured comment before making any changes — it explains what needs fixing. For the reviewer's side of this handoff (the reviewState contract, severity taxonomy, diff scoping, and the acceptance-criteria checklist convention from FLUX-1148), pull `read_skill('review')`.

All persistence uses MCP tools (see "File Boundaries" below).

## File Boundaries

You may freely read and write files in:
- `engine/src/` — Express API and engine logic
- `portal/src/` — React UI components
- `.docs/` — project documentation
- Any other source code directories

You MUST NOT read or write files in:
- `.flux/` — ticket storage (in-repo mode)
- `.flux-store/` — ticket storage (orphan mode)
- `.flux/config.json` — use `get_board_config` MCP tool instead

Use MCP tools for all ticket interactions. Use Read for source code only.

## Common Project Patterns

- Ticket persistence: engine, not portal. Docs: `.docs/`. Cards: `TaskCard.tsx`. Modal: `TaskModal.tsx`. State: `AppContext.tsx`.
- Installer: `engine/src/workflow-installer.ts` and `engine/src/skill-installer.ts`.
- MCP server: `engine/src/mcp-server.ts` — defines all agent-facing tools.
- Ticket store: `engine/src/task-store.ts` — cache, file watchers, persistence.

## Commit Guidance

- One focused commit per ticket. Describe shipped behavior, not files touched.
- **Branchless tickets:** wait for `finish <ticket>` before committing (commit + implementationLink + Done = atomic).
- **Branch / worktree tickets:** commit BEFORE moving to `Ready` — the PR opens at `Ready` and needs commits to exist. `finish` then merges that PR.
- Good: `Add ticket effort field editing`. Bad: `fix stuff`, `updates`.

## Comment Conventions

- Keep comments factual and short. Completion comments: behavior, key files, validation, commit hash.
- Prefer comments that help the next agent continue without re-discovery.
- **Write for the reader (FLUX-1502):** completion summaries and `Require Input` questions lead with the outcome/question in plain language, bolding the load-bearing phrases; handoffs to the next agent are self-contained facts — exact paths/symbols/commands/ids, action first, never "see above". No filler, no hedging. Full rules: `read_skill('orchestrator', 'Communication Style')`.
- **Substantial comments: add a faithful `summary`.** When an `add_note` comment/activity carries a long or verbose note, pass a `summary` — capture the decision, the why, and anything a future agent must act on. As concise as it can be WITHOUT losing substance; length scales with importance — do **not** force one line. Skip it for short, already-dense notes. Once the note ages past the recent window the agent digest shows the summary in place of the full text (the full text stays fetchable via `get_ticket` with `expand: ["<id>"]`). A too-short summary makes the next agent expand everything — err toward robust.
- **Pin critical entries:** set `pin: true` on review handoffs and key decisions so they are NEVER collapsed in the digest.
- **Supersede dead decisions:** when a note reverses or replaces an earlier decision in *this* ticket (e.g. "abandoned approach A, going with B"), pass `supersedes: ["<id>"]` on `add_note` pointing at the now-dead entry — don't just append and leave the stale one to confuse the next session. The superseded entry then collapses to a one-line marker in the agent digest (still recoverable via `expand`), so a later agent reads the *live* decision, not the abandoned plan. Do **NOT** supersede a still-valid entry; superseding a `pin: true` or user-authored entry is **advisory-only** — the engine keeps it full (it will not bury human intent on an agent's say-so).
</skill_module>

<skill_module name="event-horizon-review">
---
title: Event Horizon Review
order: 4
delivery: [injected:review, concatenated, modular]
deliveryNote: "🚚 INJECTED into every Claude review-phase session at spawn (including the plan-review gate's own review pass) — content added here is paid by every review session · concatenated into gemini/cursor/antigravity/windsurf/generic installs · installed per-file for copilot/cline (modular, on-demand)."
---
> ⚠️ DO NOT DELETE — This file is required for the Event Horizon agent workflow. Deleting it will break review behaviour.

## Phase: Ready (review-phase / reviewer-of-record sessions)
Scope: Judge a diff against the ticket's intent and record a machine-readable verdict during the review phase.

---

# Event Horizon Agent — Review Skill

Version: 1.7.0

## When This Skill Applies

Load this skill when you are reviewing a ticket's diff — a review-phase session launched against a `Ready` ticket, or any session where your focus instructions cast you as a reviewer. This is distinct from the implementation skill (`Todo`/`In Progress`, writing the code) even though review often follows it on the same ticket.

Reviewer sessions are triggered manually by the user — never automatically when a ticket reaches `Ready`. When a reviewer sends a ticket back to `In Progress`, read that structured comment before making any changes; it explains what needs fixing. The review conversation lives on the ticket; the GitHub PR is the diff artifact, not the source of truth for what to change.

## Diff Scoping — review the merge-base diff, never `HEAD~1`

Review the scoped diff provided in your launch context. If none is present, or you need more context, run `git diff <baselineCommit>...HEAD` using the ticket's `baselineCommit` field (from `get_ticket`) as the base. **Never use `git diff HEAD~1`** — on a multi-commit branch it only shows the last commit, silently hiding everything before it.

## Severity Taxonomy

Tag every finding with one of these three levels — normalize on this scale even if you generate findings from several angles:

- **Blocker** — must fix before `Ready`. Correctness bugs, broken acceptance criteria, security holes, data loss.
- **Major** — should fix. Real problems that don't block merging today but will bite soon (missing error handling on a reachable path, a real perf regression, a gap in test coverage for new logic).
- **Minor** — nice to have. Style, naming, small simplifications, non-blocking polish.

**Synthesizing multiple reviewers' findings** (orchestrator/supervisor leads): merge overlapping findings and remove duplicates — if multiple reviewers raised the same issue, state it once and note the consensus instead of repeating it per-reviewer. Lead the consolidated list with Blockers first, and resolve any disagreements on the merits of the argument, not a raw vote count.

## Verdict Readability (FLUX-1502)

Lead the review comment with the verdict (**APPROVED** / **CHANGES NEEDED**) and a one-sentence reason — the user and the implementer both get the point from line one. Each finding is self-contained: file:line, what's wrong, the concrete fix — never "as noted above" or a reference to another reviewer's comment. Anything the user reads (the verdict line, the synthesis summary) stays plain-language; the structured skeleton (severity tags, `reviewState`) stays intact. Full rules: `read_skill('orchestrator', 'Communication Style')`.

## Test Coverage — the "tested leaf, untested glue" flag

When a change extracts a pure helper "for testability," the **caller that sequences it** needs coverage too — a tested leaf with an untested orchestrator is a red flag, not a green check. Prefer a test that drives the assembled behavior through the caller's seam, not only the leaf in isolation. Grade a gap here at **Major** (per the taxonomy above), not Minor.

## Finite Testability

Beyond "does it work," ask whether the diff's new behavior *can* be exhaustively tested:

- **External-state coupling** — does a function the diff adds or changes read/write module-level or ambient state instead of receiving it as an argument, making it untestable in isolation? Name the symbol and the state.
- **Decision-space coverage** — is each *new* enumerable decision the diff introduces (enum arm, switch/match case, dispatch-map key, config flag) exercised by a test? Line coverage can look high while a new dispatch key is never driven. Grade a gap **Major**, per the taxonomy above.
- **Test determinism** — do new tests depend on wall-clock time, unseeded randomness, execution order, or shared module-level state bleeding between tests? Judge statically by reading the test code; do not re-run the suite to check.

Scope is this diff, never a repo-wide audit — no numeric score, no execution of the target's test suite.

## Acceptance Criteria Checklist (FLUX-1148)

If the ticket body has a `## Acceptance criteria` section (the grooming skill's GFM-checkbox convention), check off the items the diff satisfies via `update_ticket` **before** recording your verdict below. This is advisory bookkeeping, not a gate — an unchecked item never blocks `Ready` and doesn't override the Severity Taxonomy above. Its only job is to keep the portal's advisory "X/Y checked" indicator honest for the next reader instead of silently going stale. No section, or a section you can't map to the diff → skip silently.

## The reviewState Contract — CRITICAL (FLUX-816/1078)

**A verdict isn't recorded until `change_status` carries `reviewState`.** A review comment — even one starting with `APPROVED` or `CHANGES NEEDED` — is a human-readable record, not a machine-readable one: the Furnace and the board only ever read the structured `reviewState` field. Posting a clear comment is not the end of the job.

- **Orchestrated review (multiple reviewers, one synthesizer):** individual reviewer personas post findings via `add_note` and do **not** call `change_status` — an orchestrator synthesizes all reviews and makes the call. Only call `change_status` yourself if your focus instructions don't say someone else will.
- **Sole reviewer of record:** when your focus instructions say you are the SOLE reviewer — no orchestrator will synthesize other reviews and decide for you — you MUST call `change_status` yourself before ending your turn, passing `reviewState` to match your verdict:
  - No Blocker or Major items → `change_status` to `Ready` with `reviewState: 'approved'`.
  - Any Blocker or Major item → `change_status` to `In Progress` with `reviewState: 'changes-requested'` and a comment summarizing the required changes, Blockers first.

Skipping the `change_status` call strands the ticket — from the outside it looks like the review never happened even though it did, and costs a human a round-trip to unblock it.

## Follow-ups into the current Furnace batch (FLUX-1218)

When you are reviewing a ticket inside a **Furnace batch**, your launch focus names the batch id (`This review is running inside Furnace batch <id> …`). If you spot a genuine, small, clearly-related follow-up worth doing in this same burn — most naturally in a sequential batch, where it's one shared branch/PR anyway — you may queue it straight into that batch without a human gate (same trust level as your own `reviewState` verdict):

1. `create_ticket` for the follow-up (normal TL;DR + plan conventions apply).
2. `furnace_ticket` (`action:'add'`, `batchId:` the id from your focus) to append it to the same batch immediately — it burns in order like any other batch ticket.
3. Note in your review comment that you added the follow-up and why, so the completion trail is legible.

Keep it scoped: a genuine next step directly related to this diff, not a dumping ground. Tangential ideas still go through the normal board/backlog path. If your focus doesn't name a batch id, you aren't in a burn — use the ordinary ticket tools instead.

All persistence uses MCP tools — never write ticket files directly.
</skill_module>

<skill_module name="event-horizon-release">
---
title: Event Horizon Release
order: 5
delivery: [pull-only, concatenated, modular]
deliveryNote: "🚚 pull-only for Claude — reached only via read_skill('release'), never auto-injected · concatenated into gemini/cursor/antigravity/windsurf/generic installs · installed per-file for copilot/cline (modular, on-demand)."
---
> ⚠️ DO NOT DELETE — Required for release orchestration.

## Phase: Release Orchestration

---

# Event Horizon Agent — Release Skill

Version: 2.5.0

## When This Skill Applies

Load when the user asks to create a release or run a release.

## Release Workflow

1. Determine version (e.g. `v1.2.0`). If not provided, propose one based on semantic versioning. Either `1.2.0` or `v1.2.0` is accepted — the script normalizes internally (FLUX-1317), so you don't have to match a prefix convention by hand.
2. Summarize what's in `Done` status and confirm ready for release.
3. Run `npm run flux:release <version>` in `engine/`. This gathers Done tickets, generates release notes in `.docs/`, appends a one-line-per-ticket block (with completion gist) to the canonical `<releaseNotesPath>/INDEX.md`, moves tickets to `Released`, and — since **FLUX-1317** — bumps the `version` field in the root/engine/portal `package.json` to the bare semver (all release artifacts stay `v`-prefixed; only `package.json` is bare). A non-semver arg skips the `package.json` bump with a warning but still writes notes and releases tickets. No separate manual version bump is needed.
4. Review generated release notes; adjust if needed.
5. Create a git commit immediately (e.g. `Release <version>`) — it captures the released-ticket files, generated notes, and the `package.json` bumps from step 3.
6. Notify the user: tickets released, committed, point to release notes.
</skill_module>

<skill_module name="event-horizon-mapping">
---
title: Event Horizon Mapping
order: 6
delivery: [pull-only, concatenated, modular]
deliveryNote: "🚚 pull-only for Claude — reached only via read_skill('mapping'), never auto-injected · concatenated into gemini/cursor/antigravity/windsurf/generic installs · installed per-file for copilot/cline (modular, on-demand)."
---
> ⚠️ DO NOT DELETE — Required for cross-project mapping in multi-repo groups.

## Phase: Cross-Project Mapping

Scope: Scour the member repos of a multi-repo group and write the cross-project knowledge base (feature maps, system topology, shared contracts) into the canonical group docs store.

---

# Event Horizon Agent — Mapping Skill

Version: 1.1.0

## When This Skill Applies

Load when the user asks to **map**, **document**, **inventory**, or **scour** a feature, system topology, or shared contract **across the repos of a multi-repo group** — i.e. work that spans more than one member repo and produces cross-project documentation.

This skill only applies when a group is configured (a committed `group.json` in the workspace root). If `get_project_group` reports `configured: false`, there is no group — fall back to normal single-repo documentation under the repo's own `.docs/`.

## Read-Only Members — Critical Rule

Member repos are mounted in your file scope for reading (native grep/glob/read reach them directly). **Treat every sibling member repo as READ-ONLY.**

- **Never** edit, create, commit, or push files inside a member checkout.
- The parent repo (the one holding `group.json`) is the **single writer**. All cross-project docs are authored there, in `.flux-group/`.
- To change a member's own docs, route the edit through the parent — do not write into the sibling directly. (The push-through-parent round-trip is owned by the engine.)

Writing into a sibling breaks the single-writer fan-out invariant and will be overwritten. Author once, in the parent.

## Workflow

1. **Discover the group.** Call `get_project_group` to learn the members: each member's `name`, `role`, git `remote`, resolved local `path`, and whether it's checked out (`pathExists`). Skip members whose `pathExists` is false — they aren't available to scour; note them as gaps rather than guessing.
2. **Scour with your own native tools.** Use grep / glob / read against the member `path`s. There is no special MCP file tool for this — the member repos are already in your scope. Read source, configs, READMEs, and existing `.docs/` to understand how a feature or contract crosses repo boundaries.
3. **Write into the canonical group store** (`.flux-group/` in the parent repo):
   - `features/<slug>.md` — one file per feature map (see structure below).
   - `topology.md` — how the member repos fit together (services, dependencies, data flow).
   - `contracts/<name>.md` — a shared contract (API schema, event shape, shared type) and which repos produce/consume it.
   - `index.md` — the feature index; add or update an entry whenever you author a `features/<slug>.md`.
4. **Cite repos, don't copy them.** Reference member files by repo-relative path and member `name` (e.g. `engine: src/routes/tasks.ts`). Prefer durable structural facts over volatile line numbers.
5. **Persistence is the engine's job.** Author the files in `.flux-group/`; committing them to the canonical orphan branch and fanning them out to members is handled by the engine, not by you. Do not `git commit` or `git push` the group store yourself.

## Mapping Modes

| Mode | Trigger | Output |
|---|---|---|
| **Map one feature** | "map the <X> feature across the repos" | `features/<slug>.md` for that feature + an `index.md` entry |
| **Inventory all features** | "what features exist / inventory the product" | one `features/<slug>.md` per discovered feature + a complete `index.md` |
| **Topology / contracts** | "document how the system fits together / the shared contracts" | `topology.md` and/or `contracts/<name>.md` |

## Feature Map Structure (`features/<slug>.md`)

Keep each feature map skimmable and cross-referenced:

- **Summary** — one or two sentences: what the feature does, for whom.
- **Repos involved** — table of `member name` → role in this feature → key entry-point files.
- **Flow** — how a request / event / action moves across the member repos (a short ordered list or a Mermaid diagram).
- **Contracts touched** — links to `contracts/*.md` this feature depends on.
- **Gaps / unknowns** — anything that couldn't be confirmed (e.g. a member not checked out), so the next mapping pass knows where to look.

## Conventions

- One feature per `features/<slug>.md`; use a stable, lowercase, hyphenated slug.
- Update `index.md` in the same pass that adds or changes a feature map — a stale index is worse than no index.
- Mapping is a snapshot. When the product changes, re-run the relevant mapping mode rather than trying to keep docs live; there are no watchers re-scanning member repos.
</skill_module>