---
title: "Scaling the tmux swarm: hardening the Fixtures API"
topic: tmux-multiple-ai-coding-agents
type: web API (.NET minimal API)
status: draft
---

# Scaling the tmux swarm: hardening the Fixtures API

## Scenario

The first demo built the North London API and grew it with three parallel agents, each
adding an endpoint. This follow-up picks up that finished API — now on `main` with
`GET /fixtures`, `/fixtures/form`, `/table/position`, `/fixtures/export`, and
`/teams/{name}` — and points a **bigger swarm** at a different kind of work: making the
API production-ready.

Four GitHub Copilot agents run at once, one per git worktree, one per tmux pane, arranged
in a 2x2 tiled grid. Each agent hardens a different aspect of the project — tests,
container, CI, dev container — and, crucially, **each owns a distinct set of files**. That
one design choice is the star of the show: because no two agents touch the same file, the
four branches merge back into `main` with zero conflicts. It is the direct fix for the
pain point the first demo surfaced, where three agents editing a shared `Program.cs`
collided at merge time.

The point of the demo is the same orchestration pattern, scaled up and de-risked:
worktrees for isolation, tmux for a pane-per-agent view that survives detach, Copilot CLI
as the agent in each pane — plus the discipline that turns "parallel agents" into "clean
merges."

## What it demonstrates

* **Scaling the swarm past three** maps to `scripts/setup-worktrees.sh` and
  `scripts/launch-agents.sh`, now parameterised for four tasks with a `tiled` tmux layout
  so all four agents sit in a 2x2 grid on one screen.
* **File ownership as the merge-safety rule** maps to the task design in "The parallel
  tasks": each branch adds new files in its own directory, so `scripts/review-merge.sh`
  merges all four without a single conflict. This is the explicit contrast with the first
  demo's shared-`Program.cs` conflicts.
* **tmux beyond the basics** maps to the launch script's `select-layout tiled`, a
  dedicated monitoring pane running `watch`, and the detach/reattach beat
  (`Ctrl+b d` → `tmux attach -t hardening`).
* **Heterogeneous agent tasks** maps to the four prompts — a test project, a Dockerfile, a
  CI workflow, and a dev container — showing the swarm handling unrelated concerns at once
  rather than four variations of the same change.
* **Copilot CLI as the agent in each pane** maps to the `copilot -i "<task>"` invocation
  the launch script drops into each pane, plus the headless `copilot -p "<task>"
  --allow-all-tools` variant for unattended runs.

## Stack and prerequisites

* The Fixtures API repository from the first demo, with all five endpoints merged to
  `main`.
* .NET SDK (the API targets a minimal API, C#).
* git 2.5+ (worktrees).
* GitHub Copilot CLI, signed in, and trusted for each worktree directory.
* tmux. On Windows, run this inside WSL with tmux (a standalone terminal such as Windows
  Terminal, not the VS Code integrated terminal, so the `Ctrl+b` prefix reaches tmux).
* Docker (optional) to actually run the container the swarm produces.

## Architecture

* `main` — the finished Fixtures API from the first demo.
* Four sibling worktrees off the same repo, each on its own branch:
  `../fixtures-tests`, `../fixtures-docker`, `../fixtures-ci`, `../fixtures-devcontainer`.
* One tmux session (`hardening`) split into a 2x2 grid of four panes, each `cd`'d into one
  worktree with a `copilot` agent running, plus an optional fifth strip running a live
  `git worktree list` / status watch.
* Merge each branch back into `main` after review — in any order, because the file sets do
  not overlap.
* A diagram for this ("N worktrees → N tmux panes → N Copilot agents, N disjoint file
  sets") belongs in `assets/`; the blog and video reserve a diagram slot for it.

## The file-ownership rule (why this swarm merges clean)

Each task is scoped so that its branch only ever **adds files in its own area**:

| Branch | Owns | Touches `Program.cs`? |
| --- | --- | --- |
| `feature/tests` | `FixturesApi.Tests/**` | No (see pre-step below) |
| `feature/docker` | `Dockerfile`, `.dockerignore` | No |
| `feature/ci` | `.github/workflows/ci.yml` | No |
| `feature/devcontainer` | `.devcontainer/devcontainer.json` | No |

The one thing that would break the rule: an integration test usually needs
`WebApplicationFactory<Program>`, which requires the app's implicit `Program` class to be
visible. Handle it **once, on `main`, before the swarm starts**, by adding a single line —
`public partial class Program;` — to the end of `Program.cs`. With that pre-seeded, even
the tests agent stays out of `Program.cs`, and the four-way merge is guaranteed
conflict-free.

## Build steps

1. **Start from a clean `main`**: `git checkout main && git pull`, confirm `dotnet build`
   is green and `git worktree list` shows only `main`. Commands go here.
2. **Pre-seed the test hook**: add `public partial class Program;` to the end of
   `Program.cs`, commit it to `main`. This keeps the tests agent out of `Program.cs`. Code
   goes here.
3. **Write `scripts/setup-worktrees.sh`**: create four worktrees on four feature branches
   (`feature/tests`, `feature/docker`, `feature/ci`, `feature/devcontainer`) in sibling
   directories, restoring each so it starts buildable. Code goes here.
4. **Write `scripts/launch-agents.sh`**: create the `hardening` tmux session, split into
   four panes, apply the `tiled` layout, `cd` each pane into its worktree, launch
   `copilot` in each with the task as the opening prompt, and add a monitoring pane. Code
   goes here.
5. **Run it and watch**: run the launch script, watch four agents harden four aspects at
   once. Detach with `Ctrl+b d`, reattach with `tmux attach -t hardening` — agents still
   running. Commands go here.
6. **Write `scripts/review-merge.sh`**: loop the four worktrees, show each one's
   `git diff --stat main`, then merge each branch into `main`. Because the file sets are
   disjoint, order does not matter and no conflict appears. Code goes here.
7. **Prove it landed**: `dotnet test` runs the new suite green, `docker build` produces an
   image, the CI workflow shows up under Actions, and the dev container opens. Commands go
   here.
8. **Clean up**: `git worktree remove` each worktree and `git worktree prune`. Code goes
   here.

## The four parallel tasks (agent prompts)

* `feature/tests` — add an xUnit test project `FixturesApi.Tests/` that references the API
  and covers the five existing endpoints with `WebApplicationFactory<Program>`. New files
  only.
* `feature/docker` — add a multi-stage `Dockerfile` and `.dockerignore` that build and run
  the published API. New files only.
* `feature/ci` — add `.github/workflows/ci.yml` that restores, builds, and tests the
  solution on push and pull request. New file only.
* `feature/devcontainer` — add `.devcontainer/devcontainer.json` with the .NET image and
  the tools this repo uses, so the project opens in a ready-to-run container. New file
  only.

## Snippet map

Each snippet name matches a `<!-- demo: <snippet name> -->` marker in the blog post and a
`[DEMO]` cue in the video script:

* **four-worktree setup** (from `scripts/setup-worktrees.sh`) fills the blog marker in
  "Scaling the swarm past three" and the video's setup chapter.
* **tiled-swarm launch** (from `scripts/launch-agents.sh`) fills the blog marker in
  "Four agents, one screen" and the video's launch chapter.
* **conflict-free merge** (from `scripts/review-merge.sh`) fills the blog marker in "Why
  file ownership keeps the merge clean" and the video's merge chapter — the payoff shot
  where four branches land with no conflicts.

## Run it

```text
# From the finished API on main (all five endpoints merged)
git checkout main && git pull

# 1. Set up the swarm
bash scripts/setup-worktrees.sh      # four worktrees + branches
bash scripts/launch-agents.sh        # tiled tmux session, one agent per pane

# 2. Watch the four agents harden the API. Step away safely:
#    Ctrl+b d                       (detach — agents keep running)
#    tmux attach -t hardening       (come back)

# 3. When they're done, review and merge (order does not matter)
bash scripts/review-merge.sh
```

"Working" looks like: four panes each showing a Copilot agent that finished its task, four
branches with disjoint `git diff --stat` output, and `main` merging all four with zero
conflicts — then `dotnet test` green, `docker build` succeeding, the CI workflow visible,
and the dev container opening.

## What's new versus the first demo

* **Bigger swarm, tiled layout**: four agents in a 2x2 grid instead of three in a row.
* **Heterogeneous work**: hardening concerns (tests, container, CI, dev container) instead
  of four takes on the same endpoint change.
* **The merge lesson, applied**: the first demo's three agents shared `Program.cs` and
  collided at merge; here, one-file-owner-per-agent makes the four-way merge conflict-free.
  That contrast is the through-line for both the blog and the video.
