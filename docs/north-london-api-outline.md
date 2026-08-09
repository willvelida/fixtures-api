---
title: "Tmux for multiple AI coding agents demo: Fixtures API"
topic: tmux-multiple-ai-coding-agents
type: web API (.NET minimal API)
status: draft
---

# Tmux for multiple AI coding agents demo: Fixtures API

## Scenario

A small .NET minimal API that serves Arsenal fixtures and results from an in-memory
dataset. It ships with a single `GET /fixtures` endpoint. From there you hand three
*independent* features to three GitHub Copilot agents at the same time — each agent in
its own git worktree, each worktree in its own tmux pane — and watch all three build in
parallel, then review and merge them one at a time.

The API is deliberately modest. The point of the demo is the orchestration: worktrees
for isolation, tmux for a pane-per-agent view that survives detach, and Copilot CLI as
the agent in each pane. The three tasks touch different files, so the agents never step
on each other — which is exactly what the worktree-per-task pattern is there to prove.

## What it demonstrates

* **tmux sessions, panes, and live visibility** maps to `scripts/launch-agents.sh`, which
  creates one named session and splits it into a pane per agent so all three are on screen.
* **Per-agent isolation with git worktrees** maps to `scripts/setup-worktrees.sh`, which
  creates one worktree on its own branch per task.
* **Copilot CLI as the agent in each pane** maps to the `copilot` invocation the launch
  script drops into each pane (interactive), plus the headless `copilot -p "<task>"
  --allow-all-tools` variant noted for the brave.
* **Persistence — "detach, don't close"** maps to the detach/reattach step: `Ctrl+b d`,
  then `tmux attach -t northlondon`, with the agents still running.
* **Review everything, merge sequentially** maps to `scripts/review-merge.sh`, the loop
  that shows each worktree's diff and merges the branches back one at a time.

## Stack and prerequisites

* .NET SDK (minimal API, C#).
* git 2.5+ (worktrees).
* GitHub Copilot CLI, with an active Copilot subscription and the CLI trusted for each
  worktree directory.
* tmux. On Windows, run this inside WSL with tmux, or use psmux (native, reads
  `.tmux.conf`, ships a `tmux` binary) — same commands either way. This mirrors the
  blog's "Windows catch" section.
* Optional: a worktree post-create step to `dotnet restore` each new worktree so every
  agent starts from a buildable tree.

## Architecture

* `main` — the base North London API (in-memory fixtures, `GET /fixtures`).
* Three sibling worktrees off the same repo, each on its own branch:
  `../northlondon-form`, `../northlondon-table`, `../northlondon-ical`.
* One tmux session (`northlondon`) with three panes, each `cd`'d into one worktree with a
  `copilot` agent running.
* Merge each feature branch back into `main` after review.
* A diagram for this ("N worktrees → N tmux panes → N Copilot agents") belongs in
  `assets/`; the blog and video already reserve a diagram slot for it.

## Build steps

1. **Scaffold the base API**: create the North London API — an in-memory list of Arsenal
   fixtures/results and a single `GET /fixtures` endpoint. This is the shared repo the
   agents branch from. Code goes here.
2. **Commit and set up for worktrees**: commit `main`, confirm `git worktree list` works
   from the repo. Code goes here.
3. **Write `scripts/setup-worktrees.sh`**: create three worktrees on three feature
   branches (`feature/form-guide`, `feature/table-position`, `feature/ical-export`) in
   sibling directories. Exercises worktree isolation. Code goes here.
4. **Write `scripts/launch-agents.sh`**: create the `northlondon` tmux session, split into
   three panes, `cd` each pane into its worktree, and launch `copilot` in each with the
   task as the opening prompt. Exercises pane-per-agent and live visibility. Code goes here.
5. **Run it and watch**: run the launch script, watch three agents build three features at
   once. Detach with `Ctrl+b d`, reattach with `tmux attach -t northlondon` — agents still
   running. Exercises persistence. Code/commands go here.
6. **Write `scripts/review-merge.sh`**: loop the three worktrees, show each one's
   `git diff --stat`, then merge each feature branch into `main` one at a time. Exercises
   review-everything and sequential merge. Code goes here.
7. **Clean up**: `git worktree remove` each worktree and `git worktree prune`. Code goes here.

## The three parallel tasks (agent prompts)

* `feature/form-guide` — add a `GET /fixtures/form` endpoint returning the last five
  results as a W-D-L string.
* `feature/table-position` — add a `GET /table/position` endpoint returning Arsenal's
  current league position from the in-memory dataset.
* `feature/ical-export` — add a `GET /fixtures/export` endpoint that returns fixtures as
  an iCal (or CSV) feed.

## Snippet map

Each snippet name matches a `<!-- demo: <snippet name> -->` marker in the blog post and a
`[DEMO]` cue in the video script:

* **worktree setup** (from `scripts/setup-worktrees.sh`) fills the blog marker in
  "Giving each agent its own workspace with git worktrees" and the video's worktrees
  chapter.
* **parallel-agents launch** (from `scripts/launch-agents.sh`) fills the blog marker in
  "Running multiple GitHub Copilot agents in parallel" and the video's launch chapter.
* **review-merge** (bonus, from `scripts/review-merge.sh`) — no blog marker; earmarked for
  the video's "watching them work / merge" chapter if a clip needs it.

## Run it

```text
# 1. From the base repo on main
bash scripts/setup-worktrees.sh      # creates the three worktrees + branches
bash scripts/launch-agents.sh        # opens the tmux session, one agent per pane

# 2. Watch the three agents build. Step away safely:
#    Ctrl+b d       (detach — agents keep running)
#    tmux attach -t northlondon   (come back)

# 3. When they're done, review and merge one at a time
bash scripts/review-merge.sh
```

"Working" looks like: three panes each showing a Copilot agent that finished its feature,
three branches with a clean `git diff --stat`, and `main` building green after each merge.
